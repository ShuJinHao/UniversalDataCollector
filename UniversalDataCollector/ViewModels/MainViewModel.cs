using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MonitorService _monitorService = new MonitorService();
        private readonly MesService _mesService = new MesService();
        private readonly ConfigService _configService = new ConfigService();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly object _logLock = new object();
        private const string _cacheFile = "RowIndex.cache";

        private MonitorConfig _monitorConfig;
        private AppConfig _mesConfig;
        private int _lastRowIndex = 0;
        private string _lastReadFileName = "";

        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        private DataTable _uploadedTable;
        public DataView GridData => _uploadedTable?.DefaultView;

        private string _statusText;
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged("StatusText"); } }

        private bool _isRunning;

        public bool IsRunning
        {
            get => _isRunning;
            set { if (_isRunning != value) { _isRunning = value; OnPropertyChanged("IsRunning"); OnPropertyChanged("StartButtonText"); if (_isRunning) StartScan(); else StopScan(); } }
        }

        public string StartButtonText => IsRunning ? "停止监控" : "开始监控";
        public RelayCommand OpenMesSettingsCommand { get; set; }
        public RelayCommand OpenMonitorSettingsCommand { get; set; }

        public MainViewModel()
        {
            OpenMesSettingsCommand = new RelayCommand(o => new Views.MesSettingWindow().ShowDialog());
            OpenMonitorSettingsCommand = new RelayCommand(o => new Views.MonitorSettingWindow().ShowDialog());

            _monitorConfig = _configService.Load<MonitorConfig>("MonitorConfig.json");
            _mesConfig = _configService.Load<AppConfig>("AppConfig.json");

            if (File.Exists(_cacheFile)) int.TryParse(File.ReadAllText(_cacheFile), out _lastRowIndex);

            InitDataTable();
            _timer.Tick += OnTimerTick;

            // ★ 修复：重启后自动检测配置并运行 ★
            bool hasPath = _monitorConfig.Mode == MonitorMode.File ? !string.IsNullOrEmpty(_monitorConfig.TargetFilePath) : !string.IsNullOrEmpty(_monitorConfig.TargetFolderPath);
            if (hasPath) IsRunning = true;
            else StatusText = "等待配置路径...";
        }

        private void StartScan()
        {
            _timer.Interval = TimeSpan.FromSeconds(_monitorConfig.IntervalSeconds > 0 ? _monitorConfig.IntervalSeconds : 3);
            _timer.Start();
            StatusText = "正在扫描...";
            AddLog("▶ 监控服务已启动。");
        }

        private void StopScan()
        {
            _timer.Stop(); StatusText = "已停止"; AddLog("⏸ 监控服务已人为停止。");
        }

        // ... 之前的引用保持不变 ...

        private async void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            try
            {
                // 1. 读取数据 (传入真实的最后行号)
                var result = _monitorService.ReadData(_monitorConfig, _lastRowIndex);

                // 文件夹模式：如果切换了文件，重置行号
                if (!string.IsNullOrEmpty(result.CurrentFileName) && result.CurrentFileName != _lastReadFileName)
                {
                    if (!string.IsNullOrEmpty(_lastReadFileName))
                    {
                        AddLog($"文件切换 -> {Path.GetFileName(result.CurrentFileName)}，进度重置。");
                        _lastRowIndex = 0; // 重置物理行号
                    }
                    _lastReadFileName = result.CurrentFileName;
                }

                // 2. 处理数据
                if (result.NewRows.Count > 0)
                {
                    foreach (var rowData in result.NewRows)
                    {
                        var uploadData = new Dictionary<string, object>();
                        bool mapSuccess = true;

                        foreach (var map in _mesConfig.Mappings)
                        {
                            try
                            {
                                string rawVal = map.ColumnIndex < rowData.Columns.Length ? rowData.Columns[map.ColumnIndex] : "";
                                object finalVal = rawVal;

                                switch (map.DataType)
                                {
                                    case "Int":
                                        // 兼容 1.0 这种带小数点的整数
                                        finalVal = double.TryParse(rawVal, out double dv) ? (int)dv : 0;
                                        break;

                                    case "Double":
                                        // ★ 解决科学计数法：转为 decimal 能有效防止 1E+07 这种格式 ★
                                        if (double.TryParse(rawVal, out double d))
                                            finalVal = decimal.Parse(d.ToString("0.################"));
                                        else
                                            finalVal = 0.0;
                                        break;

                                    case "DateTime":
                                        finalVal = DateTime.TryParse(rawVal, out DateTime dt) ? dt.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        break;

                                    default:
                                        finalVal = rawVal.Trim();
                                        break;
                                }
                                uploadData[map.MesFieldName] = finalVal;
                            }
                            catch { mapSuccess = false; }
                        }

                        if (mapSuccess)
                        {
                            bool success = await _mesService.UploadDynamicAsync(_mesConfig.MesApiUrl, uploadData, AddLog);
                            if (success)
                            {
                                // ★ 核心修复：更新为真实的物理行号，而不是简单的 ++ ★
                                _lastRowIndex = rowData.LineIndex;
                                File.WriteAllText(_cacheFile, _lastRowIndex.ToString());
                                AddRowToGrid(uploadData);
                            }
                            else
                            {
                                AddLog("上传失败，停止本轮，等待重试...");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { AddLog($"系统异常: {ex.Message}"); }
            finally { if (IsRunning) _timer.Start(); }
        }

        private Dictionary<string, object> MapRow(string[] cols)
        {
            var dic = new Dictionary<string, object>();
            foreach (var m in _mesConfig.Mappings)
            {
                string raw = m.ColumnIndex < cols.Length ? cols[m.ColumnIndex] : "";
                object val = raw;
                switch (m.DataType)
                {
                    case "Int": val = double.TryParse(raw, out double d) ? (int)d : 0; break;
                    case "Double": val = double.TryParse(raw, out double d2) ? d2.ToString("0.################") : "0"; break;
                    case "DateTime": val = DateTime.TryParse(raw, out DateTime dt) ? dt.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); break;
                    default: val = string.IsNullOrWhiteSpace(raw) ? m.DefaultValue : raw.Trim(); break;
                }
                dic[m.MesFieldName] = val;
            }
            return dic;
        }

        private void AddLog(string m)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {m}";
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Insert(0, line);
                if (Logs.Count > 100) Logs.RemoveAt(100);
            }));
            Task.Run(() =>
            {
                lock (_logLock)
                {
                    try
                    {
                        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.AppendAllText(Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.txt"), line + Environment.NewLine);
                    }
                    catch { }
                }
            });
        }

        private void InitDataTable()
        {
            _uploadedTable = new DataTable();
            foreach (var m in _mesConfig.Mappings) _uploadedTable.Columns.Add(m.MesFieldName);
            OnPropertyChanged("GridData");
        }

        private void AddRowToGrid(Dictionary<string, object> d)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DataRow r = _uploadedTable.NewRow();
                foreach (var k in d.Keys) if (_uploadedTable.Columns.Contains(k)) r[k] = d[k];
                _uploadedTable.Rows.InsertAt(r, 0);
                if (_uploadedTable.Rows.Count > 50) _uploadedTable.Rows.RemoveAt(50);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
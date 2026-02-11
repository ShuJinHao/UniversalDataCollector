using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows.Threading;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- 核心服务 ---
        private MonitorService _monitorService = new MonitorService();

        private MesService _mesService = new MesService();

        // ★ 修复：使用支持泛型的 ConfigService
        private ConfigService _configService = new ConfigService();

        // --- 配置对象 ---
        private AppConfig _mesConfig;

        private MonitorConfig _monitorConfig;

        // --- 运行状态 ---
        private DispatcherTimer _timer;

        private int _lastRowIndex = 0;
        private string _lastReadFileName = "";
        private string _cacheFile = "RowIndex.cache";

        // --- 界面绑定属性 ---
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        private DataTable _uploadedTable;
        public DataView GridData => _uploadedTable?.DefaultView;

        private string _statusText;

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        // --- 命令 ---
        public RelayCommand OpenMesSettingsCommand { get; set; }

        public RelayCommand OpenMonitorSettingsCommand { get; set; }

        // --- 构造函数 ---
        public MainViewModel()
        {
            try
            {
                // 1. 初始化命令
                OpenMesSettingsCommand = new RelayCommand(ExecuteOpenMesSettings);
                OpenMonitorSettingsCommand = new RelayCommand(ExecuteOpenMonitorSettings);

                // 2. ★ 修复：泛型加载配置 (解决您之前的报错) ★
                _mesConfig = _configService.Load<AppConfig>("AppConfig.json");
                _monitorConfig = _configService.Load<MonitorConfig>("MonitorConfig.json");

                // 3. 初始化表格结构
                InitDataTable();

                // 4. 加载断点记忆
                if (File.Exists(_cacheFile))
                    int.TryParse(File.ReadAllText(_cacheFile), out _lastRowIndex);

                // 5. 输出初始日志
                AddLog("系统启动...");
                if (_monitorConfig.Mode == MonitorMode.File)
                    AddLog($"当前模式: 单文件监控 -> {_monitorConfig.TargetFilePath}");
                else
                    AddLog($"当前模式: 文件夹监控 -> {_monitorConfig.TargetFolderPath} (过滤: {_monitorConfig.FileNamePattern})");

                AddLog($"MES地址: {_mesConfig.MesApiUrl}");
                AddLog($"上次进度: 第 {_lastRowIndex} 行");

                // 6. 启动定时器
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(_monitorConfig.IntervalSeconds > 0 ? _monitorConfig.IntervalSeconds : 3);
                _timer.Tick += OnTimerTick;
                _timer.Start();

                StatusText = "正在运行...";
            }
            catch (Exception ex)
            {
                StatusText = "初始化失败: " + ex.Message;
                AddLog("错误: " + ex.Message);
            }
        }

        private void InitDataTable()
        {
            _uploadedTable = new DataTable();
            foreach (var map in _mesConfig.Mappings)
            {
                if (!_uploadedTable.Columns.Contains(map.MesFieldName))
                {
                    _uploadedTable.Columns.Add(map.MesFieldName);
                }
            }
            OnPropertyChanged("GridData");
        }

        private async void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop(); // 暂停防止重入
            try
            {
                // 1. 读取数据
                var result = _monitorService.ReadData(_monitorConfig, ref _lastRowIndex);

                // 检测文件切换 (文件夹模式)
                if (!string.IsNullOrEmpty(result.CurrentFileName) && result.CurrentFileName != _lastReadFileName)
                {
                    if (!string.IsNullOrEmpty(_lastReadFileName))
                    {
                        AddLog($"检测到新文件: {Path.GetFileName(result.CurrentFileName)}，重置进度。");
                        _lastRowIndex = 0;
                        _lastReadFileName = result.CurrentFileName;
                        return; // 本轮跳过，下轮从 0 开始
                    }
                    _lastReadFileName = result.CurrentFileName;
                }

                // 2. 处理新数据
                if (result.NewRows.Count > 0)
                {
                    AddLog($"读取到 {result.NewRows.Count} 条新记录...");

                    foreach (var rowCols in result.NewRows)
                    {
                        var uploadData = new Dictionary<string, object>();
                        uploadData["id"] = 0;

                        bool mapSuccess = true;

                        foreach (var map in _mesConfig.Mappings)
                        {
                            try
                            {
                                string rawVal = "";
                                if (map.ColumnIndex < rowCols.Length)
                                    rawVal = rowCols[map.ColumnIndex];

                                object finalVal = rawVal;

                                // ★★★ 核心：完整的类型处理逻辑 (Int/Double/DateTime) ★★★
                                switch (map.DataType)
                                {
                                    case "Int": // 整数处理
                                        if (double.TryParse(rawVal, out double dVal))
                                            finalVal = (int)dVal;
                                        else
                                            finalVal = int.TryParse(map.DefaultValue, out int def) ? def : 0;
                                        break;

                                    case "Double": // 小数处理 (去科学计数法)
                                        if (double.TryParse(rawVal, out double d))
                                            finalVal = d.ToString("0.################");
                                        else
                                            finalVal = map.DefaultValue ?? "0";
                                        break;

                                    case "DateTime": // 时间处理
                                        if (DateTime.TryParse(rawVal, out DateTime dt) && dt.Year > 2000)
                                            finalVal = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                        else
                                            finalVal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                        break;

                                    case "String":
                                    default:
                                        if (string.IsNullOrWhiteSpace(rawVal))
                                            finalVal = map.DefaultValue;
                                        else
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
                                _lastRowIndex++;
                                File.WriteAllText(_cacheFile, _lastRowIndex.ToString());
                                AddRowToGrid(uploadData);
                            }
                            else
                            {
                                AddLog("上传中断，等待重试...");
                                break; // 死磕当前行
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"扫描异常: {ex.Message}");
            }
            finally
            {
                _timer.Start(); // 恢复定时器
            }
        }

        private void AddRowToGrid(Dictionary<string, object> data)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    DataRow row = _uploadedTable.NewRow();
                    foreach (var kvp in data)
                    {
                        if (_uploadedTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value;
                        }
                    }
                    _uploadedTable.Rows.InsertAt(row, 0);
                    if (_uploadedTable.Rows.Count > 100) _uploadedTable.Rows.RemoveAt(100);
                }
                catch { }
            });
        }

        private void AddLog(string msg)
        {
            string timeMsg = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, timeMsg);
                if (Logs.Count > 200) Logs.RemoveAt(Logs.Count - 1);
            });
            try
            {
                File.AppendAllText("Run.log", timeMsg + Environment.NewLine);
            }
            catch { }
        }

        private void ExecuteOpenMesSettings(object obj)
        {
            var win = new Views.MesSettingWindow();
            win.ShowDialog();
            // MES 设置保存时自己会重启，这里不做额外处理
        }

        private void ExecuteOpenMonitorSettings(object obj)
        {
            var win = new Views.MonitorSettingWindow();
            win.ShowDialog();

            // ★ 修复：无 Forms 引用的原生重启
            if (System.Windows.MessageBox.Show("配置已变更，是否立即重启软件？", "重启", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                string appPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                System.Diagnostics.Process.Start(appPath);
                System.Windows.Application.Current.Shutdown();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
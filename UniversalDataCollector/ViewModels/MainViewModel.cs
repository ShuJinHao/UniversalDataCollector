using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly object _ioLock = new object();

        private const string _singleFileCache = "SingleMode_Row.cache";
        private const string _folderHistory = "FolderMode_History.db";

        private MonitorConfig _monitorConfig;
        private AppConfig _mesConfig;
        private HashSet<string> _processedFolderFiles = new HashSet<string>();

        private int _currentRowIndex = 0;
        private string _activeSourceFile = "";
        private string _currentExportPath = "";

        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();
        public DataView GridData { get { return _uploadedTable?.DefaultView; } }
        private DataTable _uploadedTable;
        private string _statusText;
        public string StatusText { get { return _statusText; } set { _statusText = value; OnPropertyChanged("StatusText"); } }

        public RelayCommand OpenMesSettingsCommand { get; set; }
        public RelayCommand OpenMonitorSettingsCommand { get; set; }

        public MainViewModel()
        {
            OpenMesSettingsCommand = new RelayCommand(o => new Views.MesSettingWindow().ShowDialog());
            OpenMonitorSettingsCommand = new RelayCommand(o => new Views.MonitorSettingWindow().ShowDialog());

            try
            {
                _mesConfig = _configService.Load<AppConfig>("AppConfig.json");
                _monitorConfig = _configService.Load<MonitorConfig>("MonitorConfig.json");

                if (File.Exists(_folderHistory))
                {
                    foreach (var l in File.ReadAllLines(_folderHistory)) if (!string.IsNullOrEmpty(l)) _processedFolderFiles.Add(l);
                }

                InitDataTable();
                _timer.Interval = TimeSpan.FromSeconds(_monitorConfig.IntervalSeconds > 0 ? _monitorConfig.IntervalSeconds : 3);
                _timer.Tick += OnTimerTick;
                _timer.Start();
                StatusText = "系统启动...";
            }
            catch { }
        }

        private async void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            try
            {
                if (_monitorConfig.Mode == MonitorMode.File) await ProcessSingleFileMode();
                else await ProcessFolderMode();
            }
            catch (Exception ex) { AddLog("异常: " + ex.Message); }
            finally { _timer.Start(); }
        }

        private async Task ProcessSingleFileMode()
        {
            string path = _monitorConfig.TargetFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            if (_activeSourceFile != path)
            {
                _activeSourceFile = path;
                _currentRowIndex = File.Exists(_singleFileCache) ? int.Parse(File.ReadAllText(_singleFileCache)) : 0;
                _currentExportPath = PrepareExportPath(path);
                AddLog("监控单文件: " + Path.GetFileName(path));
            }
            await ReadAndUpload(path);
        }

        private async Task ProcessFolderMode()
        {
            var files = _monitorService.GetMatchedFiles(_monitorConfig);
            string targetFile = files.FirstOrDefault(f => !_processedFolderFiles.Contains(f));

            if (string.IsNullOrEmpty(targetFile)) { StatusText = "等待新文件..."; return; }

            if (_activeSourceFile != targetFile)
            {
                _activeSourceFile = targetFile;
                _currentRowIndex = 0;
                _currentExportPath = PrepareExportPath(targetFile);
                AddLog("处理文件: " + Path.GetFileName(targetFile));
            }

            bool hasData = await ReadAndUpload(targetFile);
            if (!hasData) // 无新数据，标记完成
            {
                if (!_processedFolderFiles.Contains(targetFile))
                {
                    _processedFolderFiles.Add(targetFile);
                    lock (_ioLock) { File.AppendAllLines(_folderHistory, new[] { targetFile }); }
                    AddLog("完成: " + Path.GetFileName(targetFile));
                }
            }
        }

        private async Task<bool> ReadAndUpload(string path)
        {
            // ★ 修复点：传入 3 个参数 (配置, 路径, 进度) ★
            var res = _monitorService.ReadFileContent(_monitorConfig, path, _currentRowIndex);

            if (res.NewRows.Count > 0)
            {
                StatusText = "上传中: " + Path.GetFileName(path);
                foreach (var row in res.NewRows)
                {
                    var data = MapRow(row.Columns);
                    if (await _mesService.UploadDynamicAsync(_mesConfig.MesApiUrl, data, AddLog))
                    {
                        _currentRowIndex = row.LineIndex;
                        if (_monitorConfig.Mode == MonitorMode.File) File.WriteAllText(_singleFileCache, _currentRowIndex.ToString());
                        WriteToLocalCsv(data);
                        AddRowToGrid(data);
                    }
                    else return true;
                }
                return true;
            }
            return false;
        }

        private string PrepareExportPath(string sourcePath)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExportData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string ext = Path.GetExtension(sourcePath); // 保持原后缀或改为.csv
            string fullPath = Path.Combine(dir, name + ".csv");
            int count = 1;
            while (File.Exists(fullPath)) fullPath = Path.Combine(dir, string.Format("{0}_{1}.csv", name, count++));
            return fullPath;
        }

        private void WriteToLocalCsv(Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(_currentExportPath)) return;
            lock (_ioLock)
            {
                try
                {
                    bool isNew = !File.Exists(_currentExportPath);
                    using (var sw = new StreamWriter(_currentExportPath, true, new UTF8Encoding(true)))
                    {
                        if (isNew) sw.WriteLine("Time," + string.Join(",", data.Keys));
                        sw.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "," + string.Join(",", data.Values.Select(v => "\"" + v + "\"")));
                    }
                }
                catch { }
            }
        }

        private Dictionary<string, object> MapRow(string[] cols)
        {
            var dic = new Dictionary<string, object>();
            foreach (var m in _mesConfig.Mappings)
            {
                string raw = m.ColumnIndex < cols.Length ? cols[m.ColumnIndex] : "";
                dic[m.MesFieldName] = raw;
            }
            return dic;
        }

        private void AddLog(string m)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + m);
                if (Logs.Count > 100) Logs.RemoveAt(100);
            }));
        }

        private void InitDataTable()
        {
            _uploadedTable = new DataTable();
            if (_mesConfig != null) foreach (var m in _mesConfig.Mappings) _uploadedTable.Columns.Add(m.MesFieldName);
            OnPropertyChanged("GridData");
        }

        private void AddRowToGrid(Dictionary<string, object> data)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    DataRow r = _uploadedTable.NewRow();
                    foreach (string k in data.Keys) if (_uploadedTable.Columns.Contains(k)) r[k] = data[k];
                    _uploadedTable.Rows.InsertAt(r, 0);
                }
                catch { }
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string n)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }
}
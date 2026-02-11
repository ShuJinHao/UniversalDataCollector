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
        // 服务类
        private MonitorService _monitorService = new MonitorService();

        private MesService _mesService = new MesService();

        // 配置服务 (两份配置分开管理)
        private ConfigService _mesCfgService = new ConfigService(); // 读 AppConfig.json (MES配置)

        private MonitorConfigService _monitorCfgService = new MonitorConfigService(); // 读 MonitorConfig.json (监控配置)

        // 数据状态
        private AppConfig _mesConfig;

        private MonitorConfig _monitorConfig;

        private DispatcherTimer _timer;
        private int _lastRowIndex = 0;
        private string _lastReadFileName = ""; // 记录上一次读取的文件名，用于检测文件切换
        private string _cacheFile = "RowIndex.cache";

        // --- 界面绑定属性 ---
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        // 动态表格
        private DataTable _uploadedTable;

        public DataView GridData => _uploadedTable?.DefaultView;

        private string _statusText;

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        // 命令
        public RelayCommand OpenMesSettingsCommand { get; set; }

        public RelayCommand OpenMonitorSettingsCommand { get; set; }

        public MainViewModel()
        {
            try
            {
                // 1. 初始化命令
                OpenMesSettingsCommand = new RelayCommand(ExecuteOpenMesSettings);
                OpenMonitorSettingsCommand = new RelayCommand(ExecuteOpenMonitorSettings);

                // 2. 加载配置
                _mesConfig = _mesCfgService.Load();       // 加载 MES 接口和字段映射
                _monitorConfig = _monitorCfgService.Load(); // 加载 路径和模式

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
            // 根据 MES 配置里的字段映射生成列
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
            _timer.Stop(); // 暂停定时器，防止重入
            try
            {
                // 1. 调用监控服务读取数据
                var result = _monitorService.ReadData(_monitorConfig, ref _lastRowIndex);

                // ★ 关键逻辑：如果是文件夹模式，检测到换文件了，进度归零 ★
                if (!string.IsNullOrEmpty(result.CurrentFileName) && result.CurrentFileName != _lastReadFileName)
                {
                    // 如果不是第一次运行（_lastReadFileName不为空），说明是运行中换了文件
                    if (!string.IsNullOrEmpty(_lastReadFileName))
                    {
                        AddLog($"检测到新文件: {Path.GetFileName(result.CurrentFileName)}，重置进度。");
                        _lastRowIndex = 0; // 归零

                        // 既然归零了，重新读一遍新文件的前面部分（或者直接从头开始处理）
                        // 这里简单处理：本次 tick 先跳过，等下一次 tick 从 0 开始读
                        _lastReadFileName = result.CurrentFileName;
                        return;
                    }
                    _lastReadFileName = result.CurrentFileName;
                }

                // 2. 如果有新数据
                if (result.NewRows.Count > 0)
                {
                    AddLog($"读取到 {result.NewRows.Count} 条新记录...");

                    foreach (var rowCols in result.NewRows)
                    {
                        // 3. 动态映射 (CSV列 -> MES字段)
                        var uploadData = new Dictionary<string, object>();
                        uploadData["id"] = 0; // 默认ID

                        bool mapSuccess = true;

                        foreach (var map in _mesConfig.Mappings)
                        {
                            try
                            {
                                string rawVal = "";
                                // 防止 CSV 列数不够报错
                                if (map.ColumnIndex < rowCols.Length)
                                    rawVal = rowCols[map.ColumnIndex];

                                object finalVal = rawVal;

                                // 类型转换处理
                                if (map.DataType == "Number")
                                {
                                    if (double.TryParse(rawVal, out double d))
                                        finalVal = d.ToString("0.################"); // 去科学计数法
                                    else
                                        finalVal = map.DefaultValue ?? "0";
                                }
                                else if (map.DataType == "DateTime")
                                {
                                    if (DateTime.TryParse(rawVal, out DateTime dt) && dt.Year > 2000)
                                        finalVal = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                    else
                                        finalVal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 时间兜底
                                }
                                else // String
                                {
                                    if (string.IsNullOrWhiteSpace(rawVal)) finalVal = map.DefaultValue;
                                }

                                uploadData[map.MesFieldName] = finalVal;
                            }
                            catch { mapSuccess = false; }
                        }

                        // 4. 执行上传
                        if (mapSuccess)
                        {
                            bool success = await _mesService.UploadDynamicAsync(_mesConfig.MesApiUrl, uploadData, AddLog);

                            if (success)
                            {
                                // 成功：进度+1，保存断点，界面表格加一行
                                _lastRowIndex++;
                                File.WriteAllText(_cacheFile, _lastRowIndex.ToString());
                                AddRowToGrid(uploadData);
                            }
                            else
                            {
                                // 失败：死磕当前行，退出循环，等待下次定时器重试
                                AddLog("上传中断，等待重试...");
                                break;
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

        // 更新界面表格
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
                    _uploadedTable.Rows.InsertAt(row, 0); // 插在最前面
                    if (_uploadedTable.Rows.Count > 100) _uploadedTable.Rows.RemoveAt(100);
                }
                catch { }
            });
        }

        private void AddLog(string msg)
        {
            string timeMsg = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            // 写界面
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, timeMsg);
                if (Logs.Count > 200) Logs.RemoveAt(Logs.Count - 1);
            });
            // 写本地文件 (简单的追加写入)
            try
            {
                File.AppendAllText("Run.log", timeMsg + Environment.NewLine);
            }
            catch { }
        }

        // 打开设置窗口的逻辑
        private void ExecuteOpenMesSettings(object obj)
        {
            // 这里假设您有原来的 SettingsWindow (现在只负责 MES 配置)
            // var win = new Views.SettingsWindow();
            // win.ShowDialog();
            // 提示：修改完配置最好重启软件，或者在这里重新调用 _mesCfgService.Load()
            System.Windows.MessageBox.Show("请打开原来的 MES 设置窗口 (SettingsWindow.xaml)", "提示");
        }

        private void ExecuteOpenMonitorSettings(object obj)
        {
            // 打开新的监控配置窗口
            var win = new Views.MonitorSettingWindow();
            win.ShowDialog();

            // 窗口关闭后，询问是否重启以应用
            if (System.Windows.MessageBox.Show("修改监控配置需要重启软件才能生效，是否立即重启？", "配置已变更", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
                System.Windows.Forms.Application.Restart();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // 核心引擎
        private CollectorCoreService _collectorCore;

        private readonly ConfigService _configService = new ConfigService();
        private AppConfig _tempConfig; // 仅用于界面初始化显示表头

        // UI 数据源
        public ObservableCollection<string> Logs { get; set; } = new ObservableCollection<string>();

        public DataView GridData { get { return _uploadedTable?.DefaultView; } }
        private DataTable _uploadedTable;

        private string _statusText;

        public string StatusText
        {
            get { return _statusText; }
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        // 命令
        public RelayCommand OpenMesSettingsCommand { get; set; }

        public RelayCommand OpenMonitorSettingsCommand { get; set; }
        public RelayCommand StartCommand { get; set; }

        public MainViewModel()
        {
            // 1. 初始化命令
            OpenMesSettingsCommand = new RelayCommand(o => new Views.MesSettingWindow().ShowDialog());
            OpenMonitorSettingsCommand = new RelayCommand(o => new Views.MonitorSettingWindow().ShowDialog());
            StartCommand = new RelayCommand(o => StartEngine());

            // 2. ★关键修复★：先初始化空表格和日志，确保界面有东西显示
            _uploadedTable = new DataTable();
            StatusText = "正在初始化...";

            // 3. ★关键修复★：尝试读取一次配置，先把表头画出来！
            // (之前界面没了就是因为缺了这一步，导致表格是空的，或者因为依赖缺失崩了)
            try
            {
                _tempConfig = _configService.Load<AppConfig>("AppConfig.json");
                if (_tempConfig != null && _tempConfig.Mappings != null)
                {
                    foreach (var map in _tempConfig.Mappings)
                    {
                        if (!_uploadedTable.Columns.Contains(map.MesFieldName))
                        {
                            _uploadedTable.Columns.Add(map.MesFieldName);
                        }
                    }
                }
                OnPropertyChanged("GridData");
            }
            catch (Exception ex)
            {
                AddLog("配置文件读取警告: " + ex.Message);
            }

            // 4. 启动后台引擎 (加了 try-catch 防止启动报错导致界面消失)
            StartEngine();
        }

        private void StartEngine()
        {
            try
            {
                if (_collectorCore == null)
                {
                    _collectorCore = new CollectorCoreService();
                    // 订阅事件
                    _collectorCore.OnLog += AddLog;
                    _collectorCore.OnStatusChange += s => StatusText = s;
                    _collectorCore.OnNewDataProcessed += AddRowToGrid;
                }

                _collectorCore.Start();
            }
            catch (Exception ex)
            {
                StatusText = "引擎启动失败";
                AddLog("致命错误: " + ex.Message);
                MessageBox.Show("核心服务启动失败，请检查配置或日志。\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- UI 更新逻辑 ---

        private void AddLog(string m)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + m);
                if (Logs.Count > 100) Logs.RemoveAt(100);
            }));
        }

        private void AddRowToGrid(Dictionary<string, object> flatData)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    // 双重保险：如果有新字段，动态加列
                    foreach (var key in flatData.Keys)
                    {
                        if (!_uploadedTable.Columns.Contains(key))
                            _uploadedTable.Columns.Add(key);
                    }

                    DataRow r = _uploadedTable.NewRow();
                    foreach (var kvp in flatData)
                    {
                        if (_uploadedTable.Columns.Contains(kvp.Key))
                        {
                            r[kvp.Key] = FormatValueForDisplay(kvp.Value);
                        }
                    }
                    _uploadedTable.Rows.InsertAt(r, 0);

                    if (_uploadedTable.Rows.Count > 50) _uploadedTable.Rows.RemoveAt(50);

                    // 强制刷新一次视图通知
                    OnPropertyChanged("GridData");
                }
                catch (Exception ex)
                {
                    // 吞掉 UI 错误，防止崩溃
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }));
        }

        private object FormatValueForDisplay(object value)
        {
            if (value == null) return "";
            // 将数组转为字符串显示 "1.1, 2.2, 3.3"
            if (value is System.Collections.IEnumerable list && !(value is string))
            {
                var strList = new List<string>();
                foreach (var item in list) strList.Add(item?.ToString() ?? "");
                return string.Join(", ", strList);
            }
            return value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
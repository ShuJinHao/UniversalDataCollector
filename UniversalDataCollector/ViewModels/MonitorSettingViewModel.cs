using System;
using System.ComponentModel;
using System.Windows;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    // ★★★ 核心修复：这里类名必须是 MonitorSettingViewModel，不能是 Mes... ★★★
    public class MonitorSettingViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _configService = new ConfigService();
        public MonitorConfig Config { get; set; }

        public string TargetFilePath { get { return Config.TargetFilePath; } set { Config.TargetFilePath = value; OnPropertyChanged("TargetFilePath"); } }
        public string TargetFolderPath { get { return Config.TargetFolderPath; } set { Config.TargetFolderPath = value; OnPropertyChanged("TargetFolderPath"); } }
        public string FileNamePattern { get { return Config.FileNamePattern; } set { Config.FileNamePattern = value; OnPropertyChanged("FileNamePattern"); } }
        public int IntervalSeconds { get { return Config.IntervalSeconds; } set { Config.IntervalSeconds = value; OnPropertyChanged("IntervalSeconds"); } }

        // 你要的跳过行数
        public int StartRowIndex { get { return Config.StartRowIndex; } set { Config.StartRowIndex = value; OnPropertyChanged("StartRowIndex"); } }

        public bool IsFileMode { get { return Config.Mode == MonitorMode.File; } set { if (value) { Config.Mode = MonitorMode.File; Notify(); } } }
        public bool IsFolderMode { get { return Config.Mode == MonitorMode.Folder; } set { if (value) { Config.Mode = MonitorMode.Folder; Notify(); } } }

        private void Notify()
        {
            OnPropertyChanged("IsFileMode"); OnPropertyChanged("IsFolderMode");
        }

        public RelayCommand SaveCommand { get; set; }

        public MonitorSettingViewModel()
        {
            Config = _configService.Load<MonitorConfig>("MonitorConfig.json") ?? MonitorConfig.GetDefault();
            SaveCommand = new RelayCommand(o =>
            {
                _configService.Save("MonitorConfig.json", Config);
                MessageBox.Show("监控配置已保存");
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
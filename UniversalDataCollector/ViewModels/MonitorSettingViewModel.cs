using System;
using System.ComponentModel;
using System.Windows;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MonitorSettingViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _configService = new ConfigService();
        public MonitorConfig Config { get; set; }

        // 代理属性，确保界面 TextBox 即时刷新
        public string TargetFilePath { get => Config.TargetFilePath; set { Config.TargetFilePath = value; OnPropertyChanged("TargetFilePath"); } }

        public string TargetFolderPath { get => Config.TargetFolderPath; set { Config.TargetFolderPath = value; OnPropertyChanged("TargetFolderPath"); } }
        public string FileNamePattern { get => Config.FileNamePattern; set { Config.FileNamePattern = value; OnPropertyChanged("FileNamePattern"); } }
        public int IntervalSeconds { get => Config.IntervalSeconds; set { Config.IntervalSeconds = value; OnPropertyChanged("IntervalSeconds"); } }

        public bool IsFileMode { get => Config.Mode == MonitorMode.File; set { if (value) { Config.Mode = MonitorMode.File; Notify(); } } }
        public bool IsFolderMode { get => Config.Mode == MonitorMode.Folder; set { if (value) { Config.Mode = MonitorMode.Folder; Notify(); } } }

        private void Notify()
        {
            OnPropertyChanged("IsFileMode"); OnPropertyChanged("IsFolderMode");
        }

        public RelayCommand SaveCommand { get; set; }

        public MonitorSettingViewModel()
        {
            Config = _configService.Load<MonitorConfig>("MonitorConfig.json") ?? new MonitorConfig();

            SaveCommand = new RelayCommand(o =>
            {
                try
                {
                    _configService.Save("MonitorConfig.json", Config);

                    if (MessageBox.Show("配置已保存。必须重启程序以使监控生效，是否立即重启？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // ★ 调用 App 里的安全重启逻辑 ★
                        App.RequestRestart();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存配置失败: " + ex.Message);
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
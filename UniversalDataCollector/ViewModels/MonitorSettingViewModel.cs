using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MonitorSettingViewModel : INotifyPropertyChanged
    {
        private MonitorConfig _config;
        private ConfigService _service = new ConfigService();
        private const string CfgPath = "MonitorConfig.json"; // ★ 指定文件名

        public bool IsFileMode { get => _config.Mode == MonitorMode.File; set { if (value) _config.Mode = MonitorMode.File; RefreshUI(); } }
        public bool IsFolderMode { get => _config.Mode == MonitorMode.Folder; set { if (value) _config.Mode = MonitorMode.Folder; RefreshUI(); } }

        public string TargetFilePath { get => _config.TargetFilePath; set { _config.TargetFilePath = value; OnPropertyChanged("TargetFilePath"); } }
        public string TargetFolderPath { get => _config.TargetFolderPath; set { _config.TargetFolderPath = value; OnPropertyChanged("TargetFolderPath"); } }
        public string FileNamePattern { get => _config.FileNamePattern; set { _config.FileNamePattern = value; OnPropertyChanged("FileNamePattern"); } }
        public int IntervalSeconds { get => _config.IntervalSeconds; set { _config.IntervalSeconds = value; OnPropertyChanged("IntervalSeconds"); } }

        public Visibility FilePanelVisibility => IsFileMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FolderPanelVisibility => IsFolderMode ? Visibility.Visible : Visibility.Collapsed;

        public ICommand SaveCommand { get; }
        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        public MonitorSettingViewModel()
        {
            // ★ 修复：使用泛型加载
            _config = _service.Load<MonitorConfig>(CfgPath);

            SaveCommand = new RelayCommand(Save);
            BrowseFileCommand = new RelayCommand(o =>
            {
                var dlg = new OpenFileDialog { Filter = "CSV|*.csv|All|*.*" };
                if (dlg.ShowDialog() == true) TargetFilePath = dlg.FileName;
            });
            BrowseFolderCommand = new RelayCommand(o => MessageBox.Show("请复制文件夹路径粘贴到输入框。"));
        }

        private void Save(object obj)
        {
            // ★ 修复：调用 2 个参数的 Save 方法
            _service.Save(CfgPath, _config);
            MessageBox.Show("保存成功！");
        }

        private void RefreshUI()
        {
            OnPropertyChanged("IsFileMode"); OnPropertyChanged("IsFolderMode");
            OnPropertyChanged("FilePanelVisibility"); OnPropertyChanged("FolderPanelVisibility");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
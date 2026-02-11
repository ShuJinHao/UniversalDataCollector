using Microsoft.Win32; // 使用 WPF 原生对话框
using System;
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
        private MonitorConfigService _service = new MonitorConfigService();

        // --- 属性绑定 ---
        public bool IsFileMode
        {
            get => _config.Mode == MonitorMode.File;
            set { if (value) _config.Mode = MonitorMode.File; RefreshUI(); }
        }

        public bool IsFolderMode
        {
            get => _config.Mode == MonitorMode.Folder;
            set { if (value) _config.Mode = MonitorMode.Folder; RefreshUI(); }
        }

        public string TargetFilePath
        {
            get => _config.TargetFilePath;
            set { _config.TargetFilePath = value; OnPropertyChanged("TargetFilePath"); }
        }

        public string TargetFolderPath
        {
            get => _config.TargetFolderPath;
            set { _config.TargetFolderPath = value; OnPropertyChanged("TargetFolderPath"); }
        }

        public string FileNamePattern
        {
            get => _config.FileNamePattern;
            set { _config.FileNamePattern = value; OnPropertyChanged("FileNamePattern"); }
        }

        public int IntervalSeconds
        {
            get => _config.IntervalSeconds;
            set { _config.IntervalSeconds = value; OnPropertyChanged("IntervalSeconds"); }
        }

        public Visibility FilePanelVisibility => IsFileMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FolderPanelVisibility => IsFolderMode ? Visibility.Visible : Visibility.Collapsed;

        // --- 命令 ---
        public ICommand SaveCommand { get; }

        public ICommand BrowseFileCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        public MonitorSettingViewModel()
        {
            _config = _service.Load();
            SaveCommand = new RelayCommand(Save);
            BrowseFileCommand = new RelayCommand(BrowseFile);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
        }

        private void Save(object obj)
        {
            _service.Save(_config);
            MessageBox.Show("配置已保存！请重启软件生效。", "提示");
        }

        private void BrowseFile(object obj)
        {
            // 使用 WPF 原生对话框，无需 WinForms
            var dlg = new OpenFileDialog();
            dlg.Filter = "CSV Files|*.csv|All Files|*.*";
            if (dlg.ShowDialog() == true)
            {
                TargetFilePath = dlg.FileName;
            }
        }

        private void BrowseFolder(object obj)
        {
            // 既然您不想用 WinForms，这里直接提示手动输入
            MessageBox.Show("请直接在文本框中粘贴文件夹路径即可。", "提示");
        }

        private void RefreshUI()
        {
            OnPropertyChanged("IsFileMode");
            OnPropertyChanged("IsFolderMode");
            OnPropertyChanged("FilePanelVisibility");
            OnPropertyChanged("FolderPanelVisibility");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
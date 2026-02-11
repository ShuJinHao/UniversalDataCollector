using Microsoft.Win32; // 用于 OpenFileDialog
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

// 如果不想用 WinForms 的 FolderBrowserDialog，您可以只让用户手动填路径
// 或者引用 System.Windows.Forms 后开启下面的代码

namespace UniversalDataCollector.ViewModels
{
    public class MonitorSettingViewModel : INotifyPropertyChanged
    {
        private MonitorConfig _config;
        private MonitorConfigService _service = new MonitorConfigService();

        // 界面绑定属性
        public bool IsFileMode
        {
            get => _config.Mode == MonitorMode.File;
            set
            {
                if (value) _config.Mode = MonitorMode.File;
                RefreshUI();
            }
        }

        public bool IsFolderMode
        {
            get => _config.Mode == MonitorMode.Folder;
            set
            {
                if (value) _config.Mode = MonitorMode.Folder;
                RefreshUI();
            }
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

        // 控制显示隐藏
        public Visibility FilePanelVisibility => IsFileMode ? Visibility.Visible : Visibility.Collapsed;

        public Visibility FolderPanelVisibility => IsFolderMode ? Visibility.Visible : Visibility.Collapsed;

        // 命令
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
            MessageBox.Show("监控配置已保存！请重启软件以生效。", "提示");
            // 这里可以关闭窗口
        }

        private void BrowseFile(object obj)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                TargetFilePath = dlg.FileName;
            }
        }

        private void BrowseFolder(object obj)
        {
            // 如果您不想引用 System.Windows.Forms，可以删除这个方法，让用户手动输入
            // 或者使用 WinForms 的对话框:
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TargetFolderPath = dlg.SelectedPath;
                }
            }
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
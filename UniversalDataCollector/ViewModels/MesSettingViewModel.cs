using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;
using System.Collections.Generic;
using System.Linq;

namespace UniversalDataCollector.ViewModels
{
    public class MesSettingViewModel : INotifyPropertyChanged
    {
        private AppConfig _config;
        private ConfigService _service = new ConfigService();
        private const string CfgPath = "AppConfig.json"; // ★ 指定文件名

        public string MesApiUrl
        {
            get => _config.MesApiUrl;
            set { _config.MesApiUrl = value; OnPropertyChanged("MesApiUrl"); }
        }

        public ObservableCollection<FieldMapping> Mappings { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public MesSettingViewModel()
        {
            // ★ 修复：使用泛型加载
            _config = _service.Load<AppConfig>(CfgPath);

            Mappings = new ObservableCollection<FieldMapping>(_config.Mappings);
            SaveCommand = new RelayCommand(Save);
            DeleteCommand = new RelayCommand(Delete);
        }

        private void Delete(object parameter)
        {
            if (parameter is FieldMapping item)
            {
                if (MessageBox.Show($"确定要删除 '{item.MesFieldName}' 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Mappings.Remove(item);
                }
            }
        }

        private void Save(object obj)
        {
            var validMappings = Mappings.Where(m => !string.IsNullOrWhiteSpace(m.MesFieldName)).ToList();
            _config.Mappings = validMappings;

            // ★ 修复：调用 2 个参数的 Save 方法
            _service.Save(CfgPath, _config);

            if (MessageBox.Show("配置已保存！是否重启生效？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string appPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                System.Diagnostics.Process.Start(appPath);
                Application.Current.Shutdown();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
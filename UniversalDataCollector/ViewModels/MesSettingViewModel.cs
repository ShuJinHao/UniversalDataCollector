using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    public class MesSettingViewModel : INotifyPropertyChanged
    {
        private ConfigService _configService = new ConfigService();
        private AppConfig _config;

        public AppConfig Config
        {
            get => _config;
            set { _config = value; OnPropertyChanged("Config"); }
        }

        public RelayCommand AddMappingCommand { get; set; }
        public RelayCommand DeleteMappingCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }

        public MesSettingViewModel()
        {
            // 加载配置
            Config = _configService.Load<AppConfig>("AppConfig.json");

            // 确保 Mappings 已初始化
            if (Config.Mappings == null)
                Config.Mappings = new ObservableCollection<FieldMapping>();

            // 添加按钮逻辑
            AddMappingCommand = new RelayCommand(o =>
            {
                Config.Mappings.Add(new FieldMapping { MesFieldName = "NEW_FIELD", DataType = "String" });
            });

            // 删除按钮逻辑
            DeleteMappingCommand = new RelayCommand(o =>
            {
                if (o is FieldMapping mapping)
                    Config.Mappings.Remove(mapping);
            });

            // 保存逻辑
            SaveCommand = new RelayCommand(o =>
            {
                _configService.Save("AppConfig.json", Config);
                MessageBox.Show("配置已保存！");
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
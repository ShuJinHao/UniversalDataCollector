using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using UniversalDataCollector.Models;
using UniversalDataCollector.Services;

namespace UniversalDataCollector.ViewModels
{
    // ★★★ 检查：这里类名必须是 MesSettingViewModel ★★★
    public class MesSettingViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _configService = new ConfigService();
        public AppConfig Config { get; set; }

        public string MesApiUrl { get { return Config.MesApiUrl; } set { Config.MesApiUrl = value; OnPropertyChanged("MesApiUrl"); } }
        public ObservableCollection<FieldMapping> Mappings { get { return Config.Mappings; } }

        // 选中项，用于删除
        private FieldMapping _selectedMapping;

        public FieldMapping SelectedMapping { get { return _selectedMapping; } set { _selectedMapping = value; OnPropertyChanged("SelectedMapping"); } }

        public RelayCommand AddCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }

        public MesSettingViewModel()
        {
            Config = _configService.Load<AppConfig>("AppConfig.json") ?? new AppConfig();

            AddCommand = new RelayCommand(o => Mappings.Add(new FieldMapping { MesFieldName = "NewField", ColumnIndex = 0, DataType = "String" }));

            DeleteCommand = new RelayCommand(o =>
            {
                if (SelectedMapping != null) Mappings.Remove(SelectedMapping);
            });

            SaveCommand = new RelayCommand(o =>
            {
                _configService.Save("AppConfig.json", Config);
                MessageBox.Show("MES配置已保存");
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
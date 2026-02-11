using System.Diagnostics;
using System.Windows;
using UniversalDataCollector.ViewModels;

namespace UniversalDataCollector
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void BtnOpenConfig_Click(object sender, RoutedEventArgs e)
        {
            // 用记事本打开配置文件
            try { Process.Start("notepad.exe", "AppConfig.json"); } catch { }
        }
    }
}
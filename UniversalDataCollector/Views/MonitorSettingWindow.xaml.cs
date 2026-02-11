using System.Windows;
using Microsoft.Win32;

namespace UniversalDataCollector.Views
{
    public partial class MonitorSettingWindow : Window
    {
        public MonitorSettingWindow()
        {
            InitializeComponent();
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "CSV文件 (*.csv)|*.csv" };
            if (dialog.ShowDialog() == true)
            {
                // ★ 直接通过 DataContext 修改，触发通知 ★
                if (this.DataContext is ViewModels.MonitorSettingViewModel vm)
                {
                    vm.TargetFilePath = dialog.FileName;
                }
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // 变通方法：选个文件取目录
            var dialog = new OpenFileDialog { Title = "请选择文件夹下的任意一个文件" };
            if (dialog.ShowDialog() == true)
            {
                if (this.DataContext is ViewModels.MonitorSettingViewModel vm)
                {
                    vm.TargetFolderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
using System;
using System.Windows;
using Microsoft.Win32; // 用于 OpenFileDialog
using UniversalDataCollector.ViewModels;

namespace UniversalDataCollector.Views
{
    public partial class MonitorSettingWindow : Window
    {
        public MonitorSettingWindow()
        {
            InitializeComponent();
        }

        // ★ 恢复：点击选择文件
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel/CSV Files|*.csv;*.xlsx;*.xls|All files|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                var vm = this.DataContext as MonitorSettingViewModel;
                if (vm != null)
                {
                    vm.TargetFilePath = openFileDialog.FileName;
                }
            }
        }

        // ★ 恢复：点击选择文件夹
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // 在 WPF 中选择文件夹最简单的办法是使用 Windows Forms 的 Dialog
            // 如果不想引入 Forms 引用，可以用 OpenFileDialog 变通，但体验不好
            // 这里使用最稳妥的 Forms 方式 (需要项目引用 System.Windows.Forms)

            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var vm = this.DataContext as MonitorSettingViewModel;
                    if (vm != null)
                    {
                        vm.TargetFolderPath = dialog.SelectedPath;
                    }
                }
            }
        }
    }
}
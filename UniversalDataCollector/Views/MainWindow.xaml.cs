using System.Diagnostics;
using System.Windows;
using UniversalDataCollector.ViewModels;

// ★★★ 关键修改：加上 .Views，这就跟 XAML 对上了 ★★★
namespace UniversalDataCollector.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        // 如果您在 XAML 里用了 Click="BtnOpenConfig_Click"，保留这个；
        // 如果您用了 Command="{Binding ...}"，这个方法其实可以删掉。
        private void BtnOpenConfig_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("notepad.exe", "AppConfig.json"); } catch { }
        }
    }
}
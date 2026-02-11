using System.Windows;

namespace UniversalDataCollector.Views
{
    /// <summary>
    /// MesSettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MesSettingWindow : Window
    {
        public MesSettingWindow()
        {
            InitializeComponent();
        }

        // 处理关闭按钮的点击事件
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
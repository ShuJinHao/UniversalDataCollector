using System.Windows;
using UniversalDataCollector.ViewModels;

namespace UniversalDataCollector.Views
{
    public partial class MesSettingWindow : Window
    {
        public MesSettingWindow()
        {
            InitializeComponent();
            this.DataContext = new MesSettingViewModel();
        }
    }
}
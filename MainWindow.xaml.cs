using System.Windows;
using WMS.Client.ViewModels;

namespace WMS.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 綁定 ViewModel
            this.DataContext = new MainViewModel();
        }
    }
}
using System.Windows;
using WMS.Client.ViewModels; // 引用 ViewModels

namespace WMS.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 这里应该绑定 HomeViewModel 或者 MainViewModel，绝对不是 LoginViewModel
            // 如果还没做 MainViewModel，暂时留空或者不动
            this.DataContext = new MainViewModel();
        }
    }
}
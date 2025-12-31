using System.Windows.Controls;
using WMS.Client.ViewModels; // 引用

namespace WMS.Client.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            this.DataContext = new HomeViewModel(); // 绑定！
        }
    }
}
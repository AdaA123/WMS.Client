using System.Windows; // 必须引用这个
using WMS.Client.ViewModels;

namespace WMS.Client.Views
{
    // 🔴 重点：这里必须是 : Window，不能是 : UserControl
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel();
        }
    }
}
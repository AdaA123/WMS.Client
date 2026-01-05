using System.Windows;
using WMS.Client.ViewModels;

namespace WMS.Client.Views
{
    public partial class ChangePasswordView : Window
    {
        public ChangePasswordView()
        {
            InitializeComponent();
            // 🔴 关键修复：绑定 ViewModel
            this.DataContext = new ChangePasswordViewModel();
        }
    }
}
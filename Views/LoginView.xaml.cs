using System.Windows;
using System.Windows.Input; // 引用 Input 命名空间支持拖动
using WMS.Client.ViewModels;

namespace WMS.Client.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            // 🔴 关键修复：绑定 ViewModel，否则按钮没反应
            this.DataContext = new LoginViewModel();
        }

        // 让无边框窗口可以拖动
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
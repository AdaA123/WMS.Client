using System.Windows;
using WMS.Client.ViewModels;
using WMS.Client.Views;

namespace WMS.Client
{
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _mainViewModel = new MainViewModel();
            this.DataContext = _mainViewModel;

            // 程序启动，先显示登录页
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginVm = new LoginViewModel();
            var loginView = new LoginView();

            // 简单处理：把 View 的 DataContext 设为 VM
            loginView.DataContext = loginVm;

            // 订阅登录成功事件
            loginVm.LoginSuccessAction = () =>
            {
                // 登录成功，切换到 Dashboard
                ShowDashboard();
            };

            _mainViewModel.CurrentView = loginView;
        }

        private void ShowDashboard()
        {
            // 切换到主界面
            _mainViewModel.CurrentView = new DashboardView();
        }
    }
}
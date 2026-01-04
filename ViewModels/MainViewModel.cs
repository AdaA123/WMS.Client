using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 1. 定义各子页面
        private readonly HomeViewModel _homeVM;
        private readonly InboundViewModel _inboundVM;
        private readonly OutboundViewModel _outboundVM;
        private readonly ReturnViewModel _returnVM; // 🔴 新增

        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            // 2. 初始化
            _homeVM = new HomeViewModel();
            _inboundVM = new InboundViewModel();
            _outboundVM = new OutboundViewModel();
            _returnVM = new ReturnViewModel(); // 🔴 新增

            // 默认显示首页
            CurrentView = _homeVM;
        }

        // 3. 导航跳转逻辑
        [RelayCommand]
        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Home":
                    CurrentView = _homeVM;
                    _homeVM.LoadDashboardData(); // 刷新首页数据
                    break;

                case "Inbound":
                    CurrentView = _inboundVM;
                    break;

                case "Outbound":
                    CurrentView = _outboundVM;
                    break;

                case "Return": // 🔴 新增：跳转到退货页
                    CurrentView = _returnVM;
                    break;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            var loginView = new Views.LoginView();
            loginView.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        [RelayCommand]
        private void OpenChangePassword()
        {
            string currentUser = "admin";
            var view = new Views.ChangePasswordView();
            view.DataContext = new ChangePasswordViewModel(currentUser);

            if (view.ShowDialog() == true)
            {
                Logout();
            }
        }
    }
}
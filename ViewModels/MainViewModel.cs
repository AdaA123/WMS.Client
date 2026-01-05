using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 当前登录用户
        [ObservableProperty]
        private UserModel _currentUser;

        // 当前显示的视图模型
        [ObservableProperty]
        private object _currentView;

        // 缓存各个 ViewModel
        private readonly HomeViewModel _homeVM;
        private readonly InboundViewModel _inboundVM;
        private readonly OutboundViewModel _outboundVM;
        private readonly ReturnViewModel _returnVM;

        public MainViewModel(UserModel user)
        {
            CurrentUser = user;

            // 初始化子页面
            _homeVM = new HomeViewModel();
            _inboundVM = new InboundViewModel();
            _outboundVM = new OutboundViewModel();
            _returnVM = new ReturnViewModel();

            // 默认显示首页
            CurrentView = _homeVM;
        }

        // 无参构造函数供设计器使用（可选）
        public MainViewModel() : this(new UserModel { Username = "Admin" }) { }

        [RelayCommand]
        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Home":
                    CurrentView = _homeVM;
                    // 🔴 修复 CS4014：使用 _ = 忽略等待警告
                    _ = _homeVM.LoadDashboardDataCommand.ExecuteAsync(null);
                    break;
                case "Inbound":
                    CurrentView = _inboundVM;
                    break;
                case "Outbound":
                    CurrentView = _outboundVM;
                    break;
                case "Return":
                    CurrentView = _returnVM;
                    break;
            }
        }

        [RelayCommand]
        private void OpenChangePassword()
        {
            // 🔴 这里调用 ChangePasswordViewModel 的带参构造函数
            // 将 CurrentUser 传进去，解决了 CS1729 错误
            var vm = new ChangePasswordViewModel(CurrentUser);
            var view = new ChangePasswordView
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };
            view.ShowDialog();
        }

        [RelayCommand]
        private void Logout()
        {
            var loginView = new LoginView();
            loginView.Show();

            Application.Current.MainWindow?.Close();
            Application.Current.MainWindow = loginView;
        }
    }
}
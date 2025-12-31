using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WMS.Client.Views; // 引用 Views 命名空间，用于退出登录时打开 LoginView

namespace WMS.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 1. 定义三个子页面的 ViewModel (单例模式，保持页面状态)
        private readonly HomeViewModel _homeVM;
        private readonly InboundViewModel _inboundVM;
        private readonly OutboundViewModel _outboundVM;

        // 2. 当前显示的视图 (绑定到 MainWindow 的 ContentControl)
        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            // 初始化三个子页面
            _homeVM = new HomeViewModel();
            _inboundVM = new InboundViewModel();
            _outboundVM = new OutboundViewModel();

            // 🔴 关键点：设置默认页面！
            // 如果少了这一行，程序启动后 ContentControl 是空的，所以是一片白
            CurrentView = _homeVM;
        }

        // 3. 导航命令 (绑定到左侧菜单按钮)
        // 参数 viewName 来自 CommandParameter (Home, Inbound, Outbound)
        [RelayCommand]
        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Home":
                    CurrentView = _homeVM;
                    // 每次切回首页时，刷新一下统计数据
                    _homeVM.LoadDashboardData();
                    break;

                case "Inbound":
                    CurrentView = _inboundVM;
                    break;

                case "Outbound":
                    CurrentView = _outboundVM;
                    break;
            }
        }

        // 4. 退出登录命令
        [RelayCommand]
        private void Logout()
        {
            // 1. 先把新的登录窗口弹出来
            var loginView = new Views.LoginView();
            loginView.Show();

            // 2. 🔴 关键：遍历查找并关闭当前的 MainWindow
            // 我们不能简单调用 Application.Current.MainWindow.Close()，因为那个指向可能会变
            foreach (Window window in Application.Current.Windows)
            {
                // 如果这个窗口是 MainWindow，就把它关掉
                if (window is MainWindow)
                {
                    window.Close();
                    break; // 找到了就不用再找了
                }
            }
        }

        [RelayCommand]
        private void OpenChangePassword()
        {
            // 假设当前登录的是 admin (实际项目中你应该用一个全局变量存当前登录的用户名)
            // 这里我们先写死 "admin"，或者如果你有存 UserSession，就用那个
            string currentUser = "admin";

            var view = new Views.ChangePasswordView();
            // 绑定 ViewModel，并传入用户名
            view.DataContext = new ChangePasswordViewModel(currentUser);

            // 以模态窗口（对话框）形式打开，这会阻塞主窗口直到它关闭
            bool? result = view.ShowDialog();

            if (result == true)
            {
                // 如果密码修改成功，直接退出登录，强制用户重新登录
                Logout();
            }
        }

    }
}
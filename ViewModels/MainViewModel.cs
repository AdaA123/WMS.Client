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
            // 打开新的登录窗口
            var loginView = new LoginView();
            loginView.Show();

            // 关闭当前的主窗口
            Application.Current.MainWindow.Close();
        }
    }
}
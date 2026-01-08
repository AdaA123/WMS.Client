using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        private object? _currentViewModel;

        // 預設隱藏選單
        [ObservableProperty]
        private Visibility _menuVisibility = Visibility.Collapsed;

        public UserModel? CurrentUser { get; set; }

        public MainViewModel()
        {
            _dbService = new DatabaseService();
            // 🟢 啟動時：設置當前視圖為登入頁
            CurrentViewModel = new LoginViewModel(_dbService, this);
        }

        // 登入成功後調用
        public void GoToHome(UserModel user)
        {
            CurrentUser = user;
            CurrentViewModel = new HomeViewModel();
            MenuVisibility = Visibility.Visible; // 顯示選單
        }

        [RelayCommand] private void NavigateToHome() => CurrentViewModel = new HomeViewModel();
        [RelayCommand] private void NavigateToInbound() => CurrentViewModel = new InboundViewModel();
        [RelayCommand] private void NavigateToOutbound() => CurrentViewModel = new OutboundViewModel();
        [RelayCommand] private void NavigateToReturn() => CurrentViewModel = new ReturnViewModel();
        [RelayCommand] private void NavigateToFinancial() => CurrentViewModel = new FinancialViewModel();

        [RelayCommand]
        private void NavigateToChangePassword()
        {
            if (CurrentUser != null)
                CurrentViewModel = new ChangePasswordViewModel(_dbService, CurrentUser);
        }

        [RelayCommand]
        private void Logout()
        {
            // 🟢 新增：退出确认提示
            // MessageBox.Show 会阻断线程等待用户点击，如果点击“是(Yes)”才执行退出逻辑
            if (MessageBox.Show("确定要退出登录吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MenuVisibility = Visibility.Collapsed;
                CurrentUser = null;
                // 切换回登录视图
                CurrentViewModel = new LoginViewModel(_dbService, this);
            }
        }
    }
}
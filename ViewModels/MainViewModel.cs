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

        [ObservableProperty] private object? _currentViewModel;
        [ObservableProperty] private Visibility _menuVisibility = Visibility.Collapsed;
        public UserModel? CurrentUser { get; set; }

        public MainViewModel()
        {
            _dbService = new DatabaseService();
            CurrentViewModel = new LoginViewModel(_dbService, this);
        }

        public void GoToHome(UserModel user)
        {
            CurrentUser = user;
            CurrentViewModel = new HomeViewModel();
            MenuVisibility = Visibility.Visible;
        }

        [RelayCommand] private void NavigateToHome() => CurrentViewModel = new HomeViewModel();
        [RelayCommand] private void NavigateToInbound() => CurrentViewModel = new InboundViewModel();
        [RelayCommand] private void NavigateToOutbound() => CurrentViewModel = new OutboundViewModel();
        [RelayCommand] private void NavigateToReturn() => CurrentViewModel = new ReturnViewModel();

        // 🟢 新增：批发导航
        [RelayCommand] private void NavigateToWholesale() => CurrentViewModel = new WholesaleViewModel();

        [RelayCommand] private void NavigateToFinancial() => CurrentViewModel = new FinancialViewModel();

        [RelayCommand] private void NavigateToProductArchive() => CurrentViewModel = new ProductArchiveViewModel();
        [RelayCommand] private void NavigateToCustomerArchive() => CurrentViewModel = new CustomerArchiveViewModel();
        [RelayCommand] private void NavigateToSupplierArchive() => CurrentViewModel = new SupplierArchiveViewModel();

        [RelayCommand]
        private void Logout()
        {
            if (MessageBox.Show("确定要退出登录吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MenuVisibility = Visibility.Collapsed;
                CurrentUser = null;
                CurrentViewModel = new LoginViewModel(_dbService, this);
            }
        }

        [RelayCommand]
        private void NavigateToChangePassword()
        {
            if (CurrentUser != null) CurrentViewModel = new ChangePasswordViewModel(_dbService, CurrentUser);
        }
    }
}
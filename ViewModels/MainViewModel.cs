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
        [ObservableProperty]
        private UserModel _currentUser;

        [ObservableProperty]
        private object _currentView;

        private readonly HomeViewModel _homeVM;
        private readonly InboundViewModel _inboundVM;
        private readonly OutboundViewModel _outboundVM;
        private readonly ReturnViewModel _returnVM;
        private readonly FinancialViewModel _financialVM; // 🟢 新增

        public MainViewModel(UserModel user)
        {
            CurrentUser = user;

            _homeVM = new HomeViewModel();
            _inboundVM = new InboundViewModel();
            _outboundVM = new OutboundViewModel();
            _returnVM = new ReturnViewModel();
            _financialVM = new FinancialViewModel(); // 🟢 初始化

            CurrentView = _homeVM;
        }

        public MainViewModel() : this(new UserModel { Username = "Admin" }) { }

        [RelayCommand]
        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Home":
                    CurrentView = _homeVM;
                    _ = _homeVM.LoadDashboardDataCommand.ExecuteAsync(null);
                    break;
                case "Inbound":
                    CurrentView = _inboundVM;
                    _ = _inboundVM.RefreshDataAsync();
                    break;
                case "Outbound":
                    CurrentView = _outboundVM;
                    _ = _outboundVM.RefreshDataAsync();
                    break;
                case "Return":
                    CurrentView = _returnVM;
                    _ = _returnVM.RefreshDataAsync();
                    break;
                case "Financial": // 🟢 新增导航 case
                    CurrentView = _financialVM;
                    _ = _financialVM.RefreshDataAsync();
                    break;
            }
        }

        [RelayCommand]
        private void OpenChangePassword()
        {
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
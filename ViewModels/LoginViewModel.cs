using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks; // 引入 Task
using System.Windows;
using System.Windows.Controls;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty]
        private string _username = "admin";

        public LoginViewModel()
        {
            _dbService = new DatabaseService();
        }

        // 🔴 修复 MVVMTK0039：将 async void 改为 async Task
        [RelayCommand]
        private async Task Login(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("用户名或密码不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isValid = await _dbService.LoginAsync(Username, password);
            if (isValid)
            {
                // 登录成功，创建当前用户对象
                var user = new UserModel { Username = Username, Password = password };

                // 传递用户给 MainViewModel (通过构造函数或属性)
                var mainWindow = new MainWindow();
                var mainViewModel = new MainViewModel(user); // 假设 MainViewModel 接收用户
                mainWindow.DataContext = mainViewModel;

                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("用户名或密码错误！", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}
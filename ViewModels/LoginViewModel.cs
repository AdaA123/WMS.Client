using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // 必须引用，用于识别 PasswordBox
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        // 1. ✅ 手动定义属性 (稳定可靠，不会报 CS0103 错误)
        // 这里默认值设为 "admin"，方便调试
        private string? _username = "admin";
        public string? Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public LoginViewModel()
        {
            _dbService = new DatabaseService();
        }

        // 2. 登录命令
        [RelayCommand]
        private async Task Login(object parameter)
        {
            // 从 View 层传入的参数中获取 PasswordBox
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            // 校验输入
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入完整的账号和密码！", "提示");
                return;
            }

            // 执行数据库验证
            bool isValid = await _dbService.LoginAsync(Username, password);

            if (isValid)
            {
                // ✅ 验证成功：打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 关闭当前的登录窗口
                // 使用 Application.Current.Windows 遍历关闭非主窗口
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is not MainWindow)
                    {
                        window.Close();
                    }
                }
            }
            else
            {
                MessageBox.Show("账号或密码错误！\n(默认账号: admin, 密码: 888888)", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. 退出程序命令
        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }
    }
}
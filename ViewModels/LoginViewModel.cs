using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;
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
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty] private string _username = "";

        [ObservableProperty] private string _resetUsername = "";
        [ObservableProperty] private string _resetSecurityQuestion = "请输入账号获取问题";
        [ObservableProperty] private string _resetAnswer = "";

        async partial void OnResetUsernameChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                ResetSecurityQuestion = await _dbService.GetSecurityQuestionAsync(value);
            else
                ResetSecurityQuestion = "请输入账号获取问题";
        }

        public LoginViewModel(DatabaseService dbService, MainViewModel mainViewModel)
        {
            _dbService = dbService;
            _mainViewModel = mainViewModel;
        }

        [RelayCommand]
        private async Task Login(object parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                var password = passwordBox.Password;
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("请输入用户名和密码");
                    return;
                }

                // 🟢 接收 UserModel 对象
                var user = await _dbService.LoginAsync(Username, password);
                if (user != null)
                {
                    // 登录成功，传递用户对象
                    _mainViewModel.GoToHome(user);
                }
                else
                {
                    MessageBox.Show("用户名或密码错误");
                }
            }
        }

        [RelayCommand]
        private async Task OpenForgotPassword()
        {
            ResetUsername = "";
            ResetAnswer = "";
            ResetSecurityQuestion = "请输入账号获取问题";
            var view = new ForgotPasswordDialog { DataContext = this };
            await DialogHost.Show(view, "LoginDialogHost");
        }

        [RelayCommand]
        private async Task ExecuteResetPassword(object parameter)
        {
            if (parameter is PasswordBox pb)
            {
                var newPass = pb.Password;
                if (string.IsNullOrWhiteSpace(ResetUsername) || string.IsNullOrWhiteSpace(ResetAnswer) || string.IsNullOrWhiteSpace(newPass))
                {
                    MessageBox.Show("请填写完整信息");
                    return;
                }

                var success = await _dbService.VerifyAndResetPasswordAsync(ResetUsername, ResetAnswer, newPass);
                if (success)
                {
                    MessageBox.Show("密码重置成功！请使用新密码登录。");
                    DialogHost.Close("LoginDialogHost");
                }
                else
                {
                    MessageBox.Show("重置失败：账号不存在或密保答案错误。");
                }
            }
        }
    }
}
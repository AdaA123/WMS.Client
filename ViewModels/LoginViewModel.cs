using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls; // 必须加这行，为了识别 PasswordBox
using System.Windows;          // 必须加这行，为了识别 MessageBox

namespace WMS.Client.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _password;

        public Action LoginSuccessAction { get; set; } // 登录成功的回调

        [RelayCommand]
        private void Login(object parameter) // 1. 增加参数
        {
            // 2. 从参数中获取密码框控件
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password; // 获取实际输入的密码

            // 3. 验证逻辑
            if (UserName == "admin" && password == "123")
            {
                LoginSuccessAction?.Invoke(); // 登录成功
            }
            else
            {
                // 建议加上具体的错误提示，方便调试
                System.Windows.MessageBox.Show($"登录失败！\n你输入的账号: {UserName}\n你输入的密码: {password}\n正确应该是: admin / 123");
            }
        }
    }
}
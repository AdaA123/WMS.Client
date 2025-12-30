using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;          // 用于 MessageBox
using System.Windows.Controls; // 用于 PasswordBox <--- 你之前缺的就是这个

namespace WMS.Client.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _userName = "admin"; // 给个默认值方便测试

        // 注意：这里不再需要 _password 属性，因为我们直接从 UI 控件获取密码

        public Action? LoginSuccessAction { get; set; }

        [RelayCommand]
        private void Login(object parameter)
        {
            // 1. 将参数转换为 PasswordBox 控件
            var passwordBox = parameter as PasswordBox;

            // 2. 获取实际密码
            var password = passwordBox?.Password;

            // 3. 验证 (为了测试方便，先打印出来看看)
            // System.Diagnostics.Debug.WriteLine($"账号:{UserName}, 密码:{password}");

            if (UserName == "admin" && password == "123")
            {
                LoginSuccessAction?.Invoke();
            }
            else
            {
                MessageBox.Show($"登录失败！\n账号: {UserName}\n密码: {password}\n(正确是 admin / 123)");
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly UserModel _currentUser; // 保存当前用户

        // 🔴 修复 CS1729：添加接收 UserModel 的构造函数
        public ChangePasswordViewModel(UserModel currentUser)
        {
            _dbService = new DatabaseService();
            _currentUser = currentUser;
        }

        // 为了兼容性，保留无参构造函数（可选，但在 MainViewModel 中调用的是带参的）
        public ChangePasswordViewModel() : this(new UserModel { Username = "admin" }) { }

        // 🔴 修复 MVVMTK0039：将 async void 改为 async Task
        [RelayCommand]
        private async Task Change(object parameter)
        {
            var window = parameter as Window;
            if (window == null) return;

            var oldPassBox = window.FindName("OldPass") as PasswordBox;
            var newPassBox = window.FindName("NewPass") as PasswordBox;
            var confirmPassBox = window.FindName("ConfirmPass") as PasswordBox;

            // 🔴 修复 CS8600：处理可能的 null 值
            string oldPass = oldPassBox?.Password ?? string.Empty;
            string newPass = newPassBox?.Password ?? string.Empty;
            string confirmPass = confirmPassBox?.Password ?? string.Empty;

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("密码不能为空！", "提示");
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("两次输入的新密码不一致！", "错误");
                return;
            }

            // 使用当前用户的用户名
            string username = _currentUser?.Username ?? "admin";

            bool success = await _dbService.ChangePasswordAsync(username, oldPass, newPass);

            if (success)
            {
                MessageBox.Show("密码修改成功！请重新登录。", "成功");
                window.Close();
            }
            else
            {
                MessageBox.Show("旧密码错误，修改失败！", "错误");
            }
        }

        [RelayCommand]
        private void Cancel(object parameter)
        {
            var window = parameter as Window;
            window?.Close();
        }
    }
}
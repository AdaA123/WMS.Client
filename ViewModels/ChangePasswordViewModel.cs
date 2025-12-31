using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks; // ✅ 必须加上这一行！
using System.Windows;
using System.Windows.Controls;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly string _currentUsername;

        public ChangePasswordViewModel(string username)
        {
            _dbService = new DatabaseService();
            _currentUsername = username;
        }

        [RelayCommand]
        private async Task Confirm(Window window)
        {
            var oldBox = window.FindName("PbOld") as PasswordBox;
            var newBox = window.FindName("PbNew") as PasswordBox;
            var confirmBox = window.FindName("PbConfirm") as PasswordBox;

            var oldPwd = oldBox?.Password;
            var newPwd = newBox?.Password;
            var confirmPwd = confirmBox?.Password;

            if (string.IsNullOrEmpty(oldPwd) || string.IsNullOrEmpty(newPwd))
            {
                MessageBox.Show("密码不能为空！", "提示");
                return;
            }

            if (newPwd != confirmPwd)
            {
                MessageBox.Show("两次输入的新密码不一致！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isSuccess = await _dbService.ChangePasswordAsync(_currentUsername, oldPwd, newPwd);

            if (isSuccess)
            {
                MessageBox.Show("密码修改成功！请重新登录。", "成功");
                window.DialogResult = true;
                window.Close();
            }
            else
            {
                MessageBox.Show("旧密码错误，修改失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            window.Close();
        }
    }
}
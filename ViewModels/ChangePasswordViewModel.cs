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
        private readonly UserModel _currentUser;

        // 🟢 修复：构造函数接收 Service 和 User
        public ChangePasswordViewModel(DatabaseService dbService, UserModel currentUser)
        {
            _dbService = dbService;
            _currentUser = currentUser;
        }

        [RelayCommand]
        private async Task ChangePassword(object parameter)
        {
            // parameter 应该是传入的 PasswordBox 数组或类似结构，这里简化处理
            // 为了简单起见，通常 View 层会传一个 Converter 或者我们在 VM 里绑定
            // 这里假设 View 层传入了一个包含 PasswordBox 的数组 (object[]) 
            // 或者我们简单一点，不在 VM 操作 PasswordBox 的 UI 元素，而是推荐使用 behavior

            // 为了快速修复错误，这里仅演示逻辑，具体 View 绑定需对应
            if (parameter is object[] boxes && boxes.Length == 3 &&
                boxes[0] is PasswordBox pbOld &&
                boxes[1] is PasswordBox pbNew &&
                boxes[2] is PasswordBox pbConfirm)
            {
                string oldPass = pbOld.Password;
                string newPass = pbNew.Password;
                string confirmPass = pbConfirm.Password;

                if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
                {
                    MessageBox.Show("密码不能为空");
                    return;
                }

                if (newPass != confirmPass)
                {
                    MessageBox.Show("两次新密码输入不一致");
                    return;
                }

                bool success = await _dbService.ChangePasswordAsync(_currentUser.Username ?? "", oldPass, newPass);
                if (success)
                {
                    MessageBox.Show("密码修改成功！");
                    pbOld.Clear(); pbNew.Clear(); pbConfirm.Clear();
                }
                else
                {
                    MessageBox.Show("旧密码错误");
                }
            }
        }
    }
}
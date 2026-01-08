using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WMS.Client.Models
{
    public partial class UserModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // 🟢 修复：显式定义属性以支持 [Unique] 特性
        private string? _username;

        [Unique]
        public string? Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        [ObservableProperty]
        private string? _password;

        // 🟢 新增：密保问题
        [ObservableProperty]
        private string? _securityQuestion;

        // 🟢 新增：密保答案
        [ObservableProperty]
        private string? _securityAnswer;
    }
}
using SQLite;

namespace WMS.Client.Models
{
    public class UserModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // 实际项目中建议加密存储，这里为了演示先存明文
    }
}
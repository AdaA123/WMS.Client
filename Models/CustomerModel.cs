using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WMS.Client.Models
{
    public partial class CustomerModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _name;          // 客户名称
        [ObservableProperty] private string? _contactPerson; // 联系人
        [ObservableProperty] private string? _phone;         // 电话
        [ObservableProperty] private string? _address;       // 地址
        [ObservableProperty] private string? _remark;        // 备注
    }
}
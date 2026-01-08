using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WMS.Client.Models
{
    public partial class ProductModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _name;        // 品名
        [ObservableProperty] private string? _spec;        // 规格/型号
        [ObservableProperty] private string? _unit;        // 单位 (个/箱/kg)
        [ObservableProperty] private decimal _price;       // 默认参考价
        [ObservableProperty] private string? _remark;      // 备注
    }
}
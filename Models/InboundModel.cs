using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;

namespace WMS.Client.Models
{
    public partial class InboundModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _orderNo;
        [ObservableProperty] private string? _productName;
        [ObservableProperty] private string? _supplier;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _price; // 单价
        [ObservableProperty] private DateTime _inboundDate;

        // 🟢 新增：状态 (待验收, 已验收, 已退货)
        // 默认为 "待验收"
        [ObservableProperty] private string _status = "待验收";

        // 总金额 (不存入数据库，实时计算)
        [Ignore]
        public decimal TotalAmount => Quantity * Price;

        // 当数量或单价变化时，通知 TotalAmount 更新
        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalAmount));
        partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalAmount));
    }
}
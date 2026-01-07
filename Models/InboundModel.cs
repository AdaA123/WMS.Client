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

        // 进货总数
        [ObservableProperty] private int _quantity;

        [ObservableProperty] private decimal _price; // 单价

        // 创建时间 (入库时间)
        [ObservableProperty] private DateTime _inboundDate;

        // 状态 (待验收, 已验收, 已退货)
        [ObservableProperty] private string _status = "待验收";

        // 🟢 新增：验收合格数量 (计入库存)
        [ObservableProperty] private int _acceptedQuantity;

        // 🟢 新增：拒收/退货数量
        [ObservableProperty] private int _rejectedQuantity;

        // 🟢 新增：验收/处理时间
        [ObservableProperty] private DateTime? _checkDate;

        // 总金额 (显示用，基于进货总数，因为你已经付了款或生成了单据)
        [Ignore]
        public decimal TotalAmount => Quantity * Price;

        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalAmount));
        partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalAmount));
    }
}
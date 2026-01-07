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

        [ObservableProperty] private DateTime _inboundDate;

        // 状态 (待验收, 已验收, 已退货)
        [ObservableProperty] private string _status = "待验收";

        // 验收合格数量 (计入库存)
        [ObservableProperty] private int _acceptedQuantity;

        // 拒收/退货数量
        [ObservableProperty] private int _rejectedQuantity;

        // 验收/处理时间
        [ObservableProperty] private DateTime? _checkDate;

        // 1. 入库总额 (基于进货总数，对应采购单金额)
        [Ignore]
        public decimal TotalAmount => Quantity * Price;

        // 🟢 2. 新增：验收总额 (基于合格数量，对应实际入库资产)
        [Ignore]
        public decimal AcceptedTotalAmount => AcceptedQuantity * Price;

        // --- 变更通知逻辑 ---

        // 当总数量变化 -> 更新入库总额
        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalAmount));

        // 当单价变化 -> 更新两个总额
        partial void OnPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(AcceptedTotalAmount));
        }

        // 🟢 当合格数量变化 -> 更新验收总额
        partial void OnAcceptedQuantityChanged(int value) => OnPropertyChanged(nameof(AcceptedTotalAmount));
    }
}
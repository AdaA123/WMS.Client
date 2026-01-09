using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;

namespace WMS.Client.Models
{
    public partial class WholesaleModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _orderNo;      // 批发单号 (如 WS20231027001)
        [ObservableProperty] private string? _productName;  // 产品名称
        [ObservableProperty] private string? _customer;     // 客户/批发商
        [ObservableProperty] private int _quantity;         // 批发数量
        [ObservableProperty] private decimal _price;        // 批发单价
        [ObservableProperty] private DateTime _wholesaleDate = DateTime.Now; // 销售日期
        [ObservableProperty] private string? _remark;       // 备注

        [Ignore]
        public decimal TotalAmount => Price * Quantity;     // 总金额 (不存数据库，实时计算)
    }
}
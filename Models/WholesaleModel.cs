using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace WMS.Client.Models
{
    // 批发主单：记录整单信息
    public partial class WholesaleOrder : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _orderNo;      // 单号
        [ObservableProperty] private string? _customer;     // 客户
        [ObservableProperty] private DateTime _orderDate = DateTime.Now;
        [ObservableProperty] private decimal _totalAmount;  // 整单总金额
        [ObservableProperty] private string? _remark;       // 备注

        [Ignore]
        public List<WholesaleItem> Items { get; set; } = new(); // 关联的明细
    }

    // 批发细项：记录具体商品
    public partial class WholesaleItem : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int OrderId { get; set; } // 外键：关联主单

        [ObservableProperty] private string? _productName;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _price;

        [Ignore]
        public decimal SubTotal => Price * Quantity; // 小计
    }
}
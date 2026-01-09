using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;

namespace WMS.Client.Models
{
    // 批发主单
    public partial class WholesaleOrder : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _orderNo;
        [ObservableProperty] private string? _customer;
        [ObservableProperty] private string? _address; // 🟢 新增：送货地址
        [ObservableProperty] private DateTime _orderDate = DateTime.Now;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private string? _remark;

        [Ignore]
        public List<WholesaleItem> Items { get; set; } = new();
    }

    // 批发细项 (保持不变)
    public partial class WholesaleItem : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int OrderId { get; set; }

        [ObservableProperty] private string? _productName;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _price;

        [Ignore]
        public decimal SubTotal => Price * Quantity;
    }
}
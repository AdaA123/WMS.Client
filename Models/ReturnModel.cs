using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Diagnostics;

namespace WMS.Client.Models
{
    public partial class ReturnModel : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [ObservableProperty] private string? _returnNo;
        [ObservableProperty] private string? _productName;
        [ObservableProperty] private string? _customer;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _price; // 退款金额
        [ObservableProperty] private string? _reason;
        [ObservableProperty] private DateTime _returnDate;

        [Ignore]
        public decimal TotalAmount => Quantity * Price;

        partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalAmount));
        partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalAmount));
    }
}
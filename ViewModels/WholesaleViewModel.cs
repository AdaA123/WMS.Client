using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class WholesaleViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;

        public ObservableCollection<WholesaleOrder> WholesaleList { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();
        public ObservableCollection<string> CustomerList { get; } = new();
        public ObservableCollection<WholesaleItem> OrderItems { get; } = new();

        [ObservableProperty] private WholesaleOrder _currentOrder = new();
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private string _dialogTitle = "新建批发单";
        [ObservableProperty] private decimal _totalOrderAmount;

        [ObservableProperty] private string _tempProductName = "";
        [ObservableProperty] private int _tempQuantity = 1;
        [ObservableProperty] private decimal _tempPrice = 0;

        public WholesaleViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _ = LoadData();
        }

        [RelayCommand]
        private async Task LoadData()
        {
            var data = await _dbService.GetWholesaleOrdersAsync();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                data = data.Where(x => (x.OrderNo?.Contains(SearchText) ?? false) ||
                                       (x.Customer?.Contains(SearchText) ?? false)).ToList();
            }

            WholesaleList.Clear();
            foreach (var item in data) WholesaleList.Add(item);

            var products = await _dbService.GetProductListAsync();
            ProductList.Clear(); foreach (var p in products) ProductList.Add(p);
            var customers = await _dbService.GetCustomerListAsync();
            CustomerList.Clear(); foreach (var c in customers) CustomerList.Add(c);
        }

        partial void OnSearchTextChanged(string value) => _ = LoadData();
        partial void OnTempProductNameChanged(string value) => _ = FillPrice(value);

        // 🟢 修复 CS8826：去掉了 value 类型的 ?，与属性定义保持一致
        partial void OnCurrentOrderChanged(WholesaleOrder value)
        {
            if (value != null)
            {
                value.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(WholesaleOrder.Customer) && !string.IsNullOrEmpty(CurrentOrder.Customer))
                    {
                        var customer = (await _dbService.GetCustomersAsync()).FirstOrDefault(c => c.Name == CurrentOrder.Customer);
                        if (customer != null && string.IsNullOrEmpty(CurrentOrder.Address))
                        {
                            CurrentOrder.Address = customer.Address;
                        }
                    }
                };
            }
        }

        private async Task FillPrice(string name)
        {
            var lastOut = await _dbService.GetLastOutboundByProductAsync(name);
            if (lastOut != null) TempPrice = lastOut.Price;
        }

        [RelayCommand]
        private async Task OpenCreateDialog()
        {
            DialogTitle = "新建批发单";
            CurrentOrder = new WholesaleOrder
            {
                OrderNo = $"WS{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks % 10000:0000}",
                OrderDate = DateTime.Now
            };
            OnCurrentOrderChanged(CurrentOrder);

            OrderItems.Clear();
            UpdateTotal();
            TempProductName = ""; TempQuantity = 1; TempPrice = 0;

            var view = new WholesaleDialog { DataContext = this };
            await DialogHost.Show(view, "RootDialog");
        }

        [RelayCommand]
        private async Task Edit(WholesaleOrder item)
        {
            DialogTitle = "编辑批发单";
            CurrentOrder = new WholesaleOrder
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                Customer = item.Customer,
                Address = item.Address,
                OrderDate = item.OrderDate,
                Remark = item.Remark
            };
            OnCurrentOrderChanged(CurrentOrder);

            OrderItems.Clear();
            foreach (var i in item.Items)
                OrderItems.Add(new WholesaleItem { Id = i.Id, OrderId = i.OrderId, ProductName = i.ProductName, Quantity = i.Quantity, Price = i.Price });

            UpdateTotal();

            var view = new WholesaleDialog { DataContext = this };
            await DialogHost.Show(view, "RootDialog");
        }

        [RelayCommand]
        private void AddItem()
        {
            if (string.IsNullOrEmpty(TempProductName)) return;
            if (TempQuantity <= 0) return;

            OrderItems.Add(new WholesaleItem
            {
                ProductName = TempProductName,
                Quantity = TempQuantity,
                Price = TempPrice
            });

            UpdateTotal();
            TempProductName = ""; TempQuantity = 1;
        }

        [RelayCommand]
        private void RemoveItem(WholesaleItem item)
        {
            OrderItems.Remove(item);
            UpdateTotal();
        }

        private void UpdateTotal() => TotalOrderAmount = OrderItems.Sum(x => x.SubTotal);

        [RelayCommand]
        private async Task Save()
        {
            if (OrderItems.Count == 0) { MessageBox.Show("请至少添加一种商品"); return; }
            if (string.IsNullOrEmpty(CurrentOrder.Customer)) { MessageBox.Show("请选择客户"); return; }

            CurrentOrder.Items = OrderItems.ToList();
            CurrentOrder.TotalAmount = TotalOrderAmount;

            await _dbService.SaveWholesaleOrderAsync(CurrentOrder);

            if (!CustomerList.Contains(CurrentOrder.Customer))
                await _dbService.SaveCustomerAsync(new CustomerModel { Name = CurrentOrder.Customer, Address = CurrentOrder.Address });

            await LoadData();
            DialogHost.Close("RootDialog");
        }

        [RelayCommand]
        private async Task Delete(WholesaleOrder item)
        {
            if (MessageBox.Show("确定删除该单据吗？库存将回滚。", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteWholesaleOrderAsync(item);
                await LoadData();
            }
        }

        [RelayCommand]
        private void PrintOrder()
        {
            if (OrderItems.Count == 0) { MessageBox.Show("没有商品明细，无法打印。", "提示"); return; }
            _printService.PrintWholesaleOrder(CurrentOrder, OrderItems);
        }
    }
}
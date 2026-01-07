using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        [ObservableProperty] private int _totalInbound;
        [ObservableProperty] private int _totalOutbound;
        [ObservableProperty] private int _totalReturn;

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => FilterInventoryList();

        private List<InventorySummaryModel> _allInventory = new();
        public ObservableCollection<InventorySummaryModel> InventoryList { get; } = new();

        public HomeViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = LoadDashboardData();
        }

        [RelayCommand]
        public async Task LoadDashboardData()
        {
            var inbounds = await _dbService.GetInboundOrdersAsync();
            TotalInbound = inbounds.Sum(x => x.Quantity);

            var outbounds = await _dbService.GetOutboundOrdersAsync();
            TotalOutbound = outbounds.Sum(x => x.Quantity);

            var returns = await _dbService.GetReturnOrdersAsync();
            TotalReturn = returns.Sum(x => x.Quantity);

            _allInventory = await _dbService.GetInventorySummaryAsync();
            FilterInventoryList();
        }

        private void FilterInventoryList()
        {
            InventoryList.Clear();
            var query = _allInventory.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim().ToLower();
                query = query.Where(x => x.ProductName != null && x.ProductName.ToLower().Contains(key));
            }
            foreach (var item in query) InventoryList.Add(item);
        }

        // 🟢 快速入库 (带自动填充)
        [RelayCommand]
        private async Task QuickInbound()
        {
            var newOrder = new InboundModel
            {
                OrderNo = $"RK{DateTime.Now:yyyyMMddHHmmss}",
                InboundDate = DateTime.Now,
                Quantity = 1
            };

            // 监听变化实现自动填充
            newOrder.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(InboundModel.ProductName) && !string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    var last = await _dbService.GetLastInboundByProductAsync(newOrder.ProductName);
                    if (last != null)
                    {
                        newOrder.Price = last.Price;
                        newOrder.Supplier = last.Supplier;
                    }
                }
            };

            var suppliers = await _dbService.GetSupplierListAsync();
            var products = await _dbService.GetProductListAsync();

            var view = new InboundDialog(suppliers, products) { DataContext = newOrder };
            var result = await DialogHost.Show(view, "HomeDialogHost");

            if (result is bool confirm && confirm)
            {
                if (string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    MessageBox.Show("产品名称不能为空！");
                    return;
                }
                await _dbService.SaveInboundOrderAsync(newOrder);
                MessageBox.Show("快速入库成功！");
                await LoadDashboardData();
            }
        }

        // 🟢 快速出库 (带自动填充)
        [RelayCommand]
        private async Task QuickOutbound()
        {
            var newOrder = new OutboundModel
            {
                OrderNo = $"CK{DateTime.Now:yyyyMMddHHmmss}",
                OutboundDate = DateTime.Now,
                Quantity = 1
            };

            newOrder.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(OutboundModel.ProductName) && !string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    var last = await _dbService.GetLastOutboundByProductAsync(newOrder.ProductName);
                    if (last != null)
                    {
                        newOrder.Price = last.Price;
                        newOrder.Customer = last.Customer;
                    }
                }
            };

            var products = await _dbService.GetProductListAsync();
            var customers = await _dbService.GetCustomerListAsync();
            var view = new OutboundDialog(products, customers) { DataContext = newOrder };
            var result = await DialogHost.Show(view, "HomeDialogHost");

            if (result is bool confirm && confirm)
            {
                if (string.IsNullOrWhiteSpace(newOrder.ProductName))
                {
                    MessageBox.Show("请选择产品！");
                    return;
                }
                await _dbService.SaveOutboundOrderAsync(newOrder);
                MessageBox.Show("快速出库成功！");
                await LoadDashboardData();
            }
        }

        [RelayCommand]
        private void Print()
        {
            if (InventoryList.Count == 0) { MessageBox.Show("当前没有数据可打印"); return; }
            _printService.PrintInventoryReport(InventoryList);
        }

        [RelayCommand]
        private void Export()
        {
            if (InventoryList.Count == 0) { MessageBox.Show("当前没有数据可导出"); return; }
            _exportService.ExportInventory(InventoryList);
        }
    }
}
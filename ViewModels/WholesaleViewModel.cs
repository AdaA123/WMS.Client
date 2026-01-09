using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class WholesaleViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<WholesaleModel> WholesaleList { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();
        public ObservableCollection<string> Customers { get; } = new();
        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (新->旧)", "时间 (旧->新)", "数量 (多->少)", "金额 (高->低)" };

        [ObservableProperty] private WholesaleModel _newWholesale = new();
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private string _selectedSortOption = "时间 (新->旧)";
        [ObservableProperty] private string _entryProductName = "";

        public WholesaleViewModel()
        {
            _dbService = new DatabaseService();
            NewWholesale = new WholesaleModel { OrderNo = GenerateOrderNo(), WholesaleDate = DateTime.Now };
            _ = LoadData();
        }

        private string GenerateOrderNo() => $"WS{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks % 10000:0000}";

        [RelayCommand]
        private async Task LoadData()
        {
            var data = await _dbService.GetWholesaleOrdersAsync();
            var products = await _dbService.GetProductListAsync();
            var customers = await _dbService.GetCustomerListAsync();

            ProductList.Clear(); foreach (var p in products) ProductList.Add(p);
            Customers.Clear(); foreach (var c in customers) Customers.Add(c);

            // 搜索过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                data = data.Where(x => (x.OrderNo?.Contains(SearchText) ?? false) ||
                                       (x.ProductName?.Contains(SearchText) ?? false) ||
                                       (x.Customer?.Contains(SearchText) ?? false)).ToList();
            }

            // 排序
            data = SelectedSortOption switch
            {
                "时间 (旧->新)" => data.OrderBy(x => x.WholesaleDate).ToList(),
                "数量 (多->少)" => data.OrderByDescending(x => x.Quantity).ToList(),
                "金额 (高->低)" => data.OrderByDescending(x => x.TotalAmount).ToList(),
                _ => data.OrderByDescending(x => x.WholesaleDate).ToList(),
            };

            WholesaleList.Clear();
            foreach (var item in data) WholesaleList.Add(item);
        }

        partial void OnSearchTextChanged(string value) => _ = LoadData();
        partial void OnSelectedSortOptionChanged(string value) => _ = LoadData();

        partial void OnEntryProductNameChanged(string value)
        {
            NewWholesale.ProductName = value;
            if (!string.IsNullOrEmpty(value)) _ = FillProductInfo(value);
        }

        private async Task FillProductInfo(string productName)
        {
            // 批发参考上次出库价，您也可以改为参考其他价格
            var lastOut = await _dbService.GetLastOutboundByProductAsync(productName);
            if (lastOut != null && NewWholesale.Price == 0)
            {
                NewWholesale.Price = lastOut.Price; // 自动填入参考价
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrEmpty(NewWholesale.ProductName)) { MessageBox.Show("请填写产品名称"); return; }
            if (NewWholesale.Quantity <= 0) { MessageBox.Show("数量必须大于0"); return; }

            // 检查库存 (可选)
            var inventory = await _dbService.GetInventorySummaryAsync();
            var item = inventory.FirstOrDefault(x => x.ProductName == NewWholesale.ProductName);
            if (item != null && item.CurrentStock < NewWholesale.Quantity)
            {
                if (MessageBox.Show($"当前库存仅剩 {item.CurrentStock}，确定要批发 {NewWholesale.Quantity} 吗？\n库存将变为负数。", "库存预警", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
            }

            if (string.IsNullOrEmpty(NewWholesale.OrderNo)) NewWholesale.OrderNo = GenerateOrderNo();

            await _dbService.SaveWholesaleOrderAsync(NewWholesale);

            // 如果是新客户，自动添加到客户档案
            if (!string.IsNullOrEmpty(NewWholesale.Customer))
            {
                var exists = (await _dbService.GetCustomersAsync()).Any(c => c.Name == NewWholesale.Customer);
                if (!exists) await _dbService.SaveCustomerAsync(new CustomerModel { Name = NewWholesale.Customer });
            }

            NewWholesale = new WholesaleModel { OrderNo = GenerateOrderNo(), WholesaleDate = DateTime.Now };
            EntryProductName = "";
            await LoadData();
        }

        [RelayCommand]
        private void Edit(WholesaleModel item)
        {
            NewWholesale = new WholesaleModel
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                ProductName = item.ProductName,
                Customer = item.Customer,
                Price = item.Price,
                Quantity = item.Quantity,
                WholesaleDate = item.WholesaleDate,
                Remark = item.Remark
            };
            EntryProductName = item.ProductName ?? "";
        }

        [RelayCommand]
        private void Cancel()
        {
            NewWholesale = new WholesaleModel { OrderNo = GenerateOrderNo(), WholesaleDate = DateTime.Now };
            EntryProductName = "";
        }

        [RelayCommand]
        private async Task Delete(WholesaleModel item)
        {
            if (MessageBox.Show("确定删除该批发单吗？\n库存将会回退。", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteWholesaleOrderAsync(item);
                await LoadData();
            }
        }
    }
}
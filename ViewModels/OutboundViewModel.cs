using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class OutboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        public ObservableCollection<OutboundModel> OutboundList { get; } = new();
        public ObservableCollection<string> Customers { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();

        private List<OutboundModel> _cachedList = new();

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => ProcessData();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "客户" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => ProcessData();

        [ObservableProperty] private OutboundModel _newOutbound = new();

        // 🟢 自动填充触发属性
        [ObservableProperty] private string _entryProductName = "";
        async partial void OnEntryProductNameChanged(string value)
        {
            NewOutbound.ProductName = value;
            if (NewOutbound.Id == 0 && !string.IsNullOrWhiteSpace(value))
            {
                var lastRecord = await _dbService.GetLastOutboundByProductAsync(value);
                if (lastRecord != null)
                {
                    NewOutbound.Price = lastRecord.Price;
                    NewOutbound.Customer = lastRecord.Customer;
                }
            }
        }

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            _cachedList = await _dbService.GetOutboundOrdersAsync();

            var customers = await _dbService.GetCustomerListAsync();
            Customers.Clear(); foreach (var c in customers) Customers.Add(c);

            var products = await _dbService.GetProductListAsync();
            ProductList.Clear(); foreach (var p in products) ProductList.Add(p);

            ProcessData();
        }

        private void ProcessData()
        {
            var query = _cachedList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim().ToLower();
                query = query.Where(x =>
                    (x.OrderNo?.ToLower().Contains(key) ?? false) ||
                    (x.ProductName?.ToLower().Contains(key) ?? false) ||
                    (x.Customer?.ToLower().Contains(key) ?? false));
            }

            query = SelectedSortOption switch
            {
                "时间 (最新)" => query.OrderByDescending(x => x.OutboundDate),
                "时间 (最早)" => query.OrderBy(x => x.OutboundDate),
                "产品名称" => query.OrderBy(x => x.ProductName),
                "客户" => query.OrderBy(x => x.Customer),
                _ => query.OrderByDescending(x => x.OutboundDate)
            };

            OutboundList.Clear();
            foreach (var item in query) OutboundList.Add(item);
        }

        [RelayCommand]
        private void Edit(OutboundModel item)
        {
            if (item == null) return;
            NewOutbound = new OutboundModel { Id = item.Id, OrderNo = item.OrderNo, ProductName = item.ProductName, Quantity = item.Quantity, Price = item.Price, Customer = item.Customer, OutboundDate = item.OutboundDate };
            _entryProductName = item.ProductName ?? "";
            OnPropertyChanged(nameof(EntryProductName));
        }

        [RelayCommand]
        private void Cancel()
        {
            NewOutbound = new OutboundModel();
            EntryProductName = "";
        }

        [RelayCommand] private void Print() { if (OutboundList.Count == 0) { MessageBox.Show("无数据可打印"); return; } _printService.PrintOutboundReport(OutboundList); }
        [RelayCommand] private void Export() { if (OutboundList.Count == 0) { MessageBox.Show("无数据可导出"); return; } _exportService.ExportOutbound(OutboundList); }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewOutbound.ProductName)) { MessageBox.Show("产品名称不能为空！"); return; }
            if (NewOutbound.Quantity <= 0) { MessageBox.Show("数量必须大于 0！"); return; }
            try
            {
                if (NewOutbound.Id == 0)
                {
                    NewOutbound.OrderNo = $"CK{DateTime.Now:yyyyMMddHHmmss}";
                    NewOutbound.OutboundDate = DateTime.Now;
                }
                if (string.IsNullOrEmpty(NewOutbound.Customer)) NewOutbound.Customer = "散客";
                await _dbService.SaveOutboundOrderAsync(NewOutbound);
                await RefreshDataAsync();
                Cancel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private async Task Delete(OutboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"确认删除单号 [{item.OrderNo}] 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteOutboundOrderAsync(item);
                await RefreshDataAsync();
                if (NewOutbound.Id == item.Id) Cancel();
            }
        }
    }
}
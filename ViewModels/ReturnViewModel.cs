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
    public partial class ReturnViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly ExportService _exportService;
        private readonly PrintService _printService; // 🟢 注入

        public ObservableCollection<ReturnModel> ReturnList { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();
        public ObservableCollection<string> Customers { get; } = new();

        private List<ReturnModel> _cachedList = new();

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => ProcessData();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "客户" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => ProcessData();

        [ObservableProperty] private ReturnModel _newReturn = new();

        [ObservableProperty] private string _entryProductName = "";
        async partial void OnEntryProductNameChanged(string value)
        {
            if (NewReturn.ProductName != value) NewReturn.ProductName = value;

            if (NewReturn.Id == 0 && !string.IsNullOrWhiteSpace(value))
            {
                var lastRecord = await _dbService.GetLastReturnByProductAsync(value);
                if (lastRecord != null)
                {
                    NewReturn.Price = lastRecord.Price;
                    NewReturn.Customer = lastRecord.Customer;
                }
                else
                {
                    var lastSale = await _dbService.GetLastOutboundByProductAsync(value);
                    if (lastSale != null)
                    {
                        NewReturn.Price = lastSale.Price;
                        NewReturn.Customer = lastSale.Customer;
                    }
                }
            }
        }

        public ReturnViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _printService = new PrintService(); // 🟢 初始化
            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            _cachedList = await _dbService.GetReturnOrdersAsync();
            var prods = await _dbService.GetShippedProductListAsync();
            ProductList.Clear(); foreach (var p in prods) ProductList.Add(p);
            var custs = await _dbService.GetCustomerListAsync();
            Customers.Clear(); foreach (var c in custs) Customers.Add(c);
            ProcessData();
        }

        private void ProcessData()
        {
            var query = _cachedList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim().ToLower();
                query = query.Where(x =>
                    (x.ReturnNo?.ToLower().Contains(key) ?? false) ||
                    (x.ProductName?.ToLower().Contains(key) ?? false) ||
                    (x.Customer?.ToLower().Contains(key) ?? false));
            }

            query = SelectedSortOption switch
            {
                "时间 (最新)" => query.OrderByDescending(x => x.ReturnDate),
                "时间 (最早)" => query.OrderBy(x => x.ReturnDate),
                "产品名称" => query.OrderBy(x => x.ProductName),
                "客户" => query.OrderBy(x => x.Customer),
                _ => query.OrderByDescending(x => x.ReturnDate)
            };

            ReturnList.Clear();
            foreach (var item in query) ReturnList.Add(item);
        }

        [RelayCommand]
        private void Edit(ReturnModel item)
        {
            if (item == null) return;
            NewReturn = new ReturnModel { Id = item.Id, ReturnNo = item.ReturnNo, ProductName = item.ProductName, Quantity = item.Quantity, Price = item.Price, Customer = item.Customer, Reason = item.Reason, ReturnDate = item.ReturnDate };
            EntryProductName = item.ProductName ?? "";
        }

        [RelayCommand]
        private void Cancel()
        {
            NewReturn = new ReturnModel();
            EntryProductName = "";
        }

        [RelayCommand] private void Export() { if (ReturnList.Count == 0) { MessageBox.Show("无数据可导出"); return; } _exportService.ExportReturn(ReturnList); }

        // 🟢 新增：打印命令
        [RelayCommand]
        private void Print()
        {
            if (ReturnList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
            _printService.PrintReturnReport(ReturnList);
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewReturn.ProductName)) { MessageBox.Show("请选择产品！"); return; }
            if (NewReturn.Quantity <= 0) { MessageBox.Show("数量必须大于 0！"); return; }
            try
            {
                if (NewReturn.Id == 0)
                {
                    NewReturn.ReturnNo = $"TH{DateTime.Now:yyyyMMddHHmmss}";
                    NewReturn.ReturnDate = DateTime.Now;
                }
                if (string.IsNullOrEmpty(NewReturn.Customer)) NewReturn.Customer = "散客";
                if (string.IsNullOrEmpty(NewReturn.Reason)) NewReturn.Reason = "无理由退货";
                await _dbService.SaveReturnOrderAsync(NewReturn);
                await RefreshDataAsync();
                Cancel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private async Task Delete(ReturnModel item)
        {
            if (MessageBox.Show($"确定删除单号 {item.ReturnNo} 吗？", "删除确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteReturnOrderAsync(item);
                await RefreshDataAsync();
                if (NewReturn.Id == item.Id) Cancel();
            }
        }
    }
}
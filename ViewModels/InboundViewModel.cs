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
    public partial class InboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        public ObservableCollection<InboundModel> InboundList { get; } = new();
        public ObservableCollection<string> Suppliers { get; } = new();
        // 产品下拉列表
        public ObservableCollection<string> ProductList { get; } = new();

        private List<InboundModel> _cachedList = new();

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => ProcessData();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "供应商" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => ProcessData();

        [ObservableProperty] private InboundModel _newInbound = new();

        // 🟢 录入专用产品名称属性，用于触发自动填充逻辑
        [ObservableProperty] private string _entryProductName = "";

        // 当输入或选择产品时触发
        async partial void OnEntryProductNameChanged(string value)
        {
            // 同步到实体
            NewInbound.ProductName = value;

            // 只有是新建模式(ID=0)且输入不为空时，才自动填充
            if (NewInbound.Id == 0 && !string.IsNullOrWhiteSpace(value))
            {
                var lastRecord = await _dbService.GetLastInboundByProductAsync(value);
                if (lastRecord != null)
                {
                    // 自动填充上次的价格和供应商
                    NewInbound.Price = lastRecord.Price;
                    NewInbound.Supplier = lastRecord.Supplier;
                }
            }
        }

        public InboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            _cachedList = await _dbService.GetInboundOrdersAsync();

            var suppliers = await _dbService.GetSupplierListAsync();
            Suppliers.Clear();
            foreach (var s in suppliers) Suppliers.Add(s);

            var products = await _dbService.GetProductListAsync();
            ProductList.Clear();
            foreach (var p in products) ProductList.Add(p);

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
                    (x.Supplier?.ToLower().Contains(key) ?? false));
            }

            query = SelectedSortOption switch
            {
                "时间 (最新)" => query.OrderByDescending(x => x.InboundDate),
                "时间 (最早)" => query.OrderBy(x => x.InboundDate),
                "产品名称" => query.OrderBy(x => x.ProductName),
                "供应商" => query.OrderBy(x => x.Supplier),
                _ => query.OrderByDescending(x => x.InboundDate)
            };

            InboundList.Clear();
            foreach (var item in query) InboundList.Add(item);
        }

        [RelayCommand]
        private void Edit(InboundModel item)
        {
            if (item == null) return;
            // 复制对象
            NewInbound = new InboundModel
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Supplier = item.Supplier,
                InboundDate = item.InboundDate
            };
            // 此时不需要触发自动填充，所以直接设置字段或不做操作，
            // 但为了界面显示，我们需要更新EntryProductName
            _entryProductName = item.ProductName ?? "";
            OnPropertyChanged(nameof(EntryProductName));
        }

        [RelayCommand]
        private void Cancel()
        {
            NewInbound = new InboundModel();
            EntryProductName = "";
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewInbound.ProductName)) { MessageBox.Show("产品名称不能为空！"); return; }
            if (NewInbound.Quantity <= 0) { MessageBox.Show("数量必须大于 0！"); return; }
            try
            {
                if (NewInbound.Id == 0)
                {
                    NewInbound.OrderNo = $"RK{DateTime.Now:yyyyMMddHHmmss}";
                    NewInbound.InboundDate = DateTime.Now;
                }
                await _dbService.SaveInboundOrderAsync(NewInbound);
                await RefreshDataAsync();
                Cancel(); // 重置
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand] private void Print() { if (InboundList.Count == 0) MessageBox.Show("无数据"); else _printService.PrintInboundReport(InboundList); }
        [RelayCommand] private void Export() { if (InboundList.Count == 0) MessageBox.Show("无数据"); else _exportService.ExportInbound(InboundList); }
        [RelayCommand] private async Task Delete(InboundModel item) { if (MessageBox.Show("确认删除？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { await _dbService.DeleteInboundOrderAsync(item); await RefreshDataAsync(); if (NewInbound.Id == item.Id) Cancel(); } }
    }
}
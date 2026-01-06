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

        // 🟢 缓存原始数据
        private List<InboundModel> _cachedList = new();

        // 🟢 搜索属性
        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => ProcessData();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "供应商" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => ProcessData();

        [ObservableProperty] private InboundModel _newInbound = new();

        public InboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            // 1. 加载数据到缓存
            _cachedList = await _dbService.GetInboundOrdersAsync();
            // 2. 加载下拉框
            var suppliers = await _dbService.GetSupplierListAsync();
            Suppliers.Clear();
            foreach (var s in suppliers) Suppliers.Add(s);
            // 3. 处理筛选和排序
            ProcessData();
        }

        private void ProcessData()
        {
            var query = _cachedList.AsEnumerable();

            // 筛选
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim().ToLower();
                query = query.Where(x =>
                    (x.OrderNo?.ToLower().Contains(key) ?? false) ||
                    (x.ProductName?.ToLower().Contains(key) ?? false) ||
                    (x.Supplier?.ToLower().Contains(key) ?? false));
            }

            // 排序
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
        }

        [RelayCommand]
        private void Cancel() => NewInbound = new InboundModel();

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
                NewInbound = new InboundModel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private void Print()
        {
            if (InboundList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
            _printService.PrintInboundReport(InboundList);
        }

        [RelayCommand]
        private void Export()
        {
            if (InboundList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
            _exportService.ExportInbound(InboundList);
        }

        [RelayCommand]
        private async Task Delete(InboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"确认删除单号 [{item.OrderNo}] 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteInboundOrderAsync(item);
                await RefreshDataAsync();
                if (NewInbound.Id == item.Id) NewInbound = new InboundModel();
            }
        }
    }
}
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
    public partial class InboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        public ObservableCollection<InboundModel> InboundList { get; } = new();
        public ObservableCollection<string> Suppliers { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();

        private List<InboundModel> _cachedList = new();

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => ProcessData();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "供应商", "状态" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => ProcessData();

        [ObservableProperty] private InboundModel _newInbound = new();

        [ObservableProperty] private string _entryProductName = "";

        async partial void OnEntryProductNameChanged(string value)
        {
            if (NewInbound.ProductName != value) NewInbound.ProductName = value;
            if (NewInbound.Id == 0 && !string.IsNullOrWhiteSpace(value))
            {
                var lastRecord = await _dbService.GetLastInboundByProductAsync(value);
                if (lastRecord != null)
                {
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
            Suppliers.Clear(); foreach (var s in suppliers) Suppliers.Add(s);

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
                    (x.Supplier?.ToLower().Contains(key) ?? false) ||
                    (x.Status?.ToLower().Contains(key) ?? false));
            }

            query = SelectedSortOption switch
            {
                "时间 (最新)" => query.OrderByDescending(x => x.InboundDate),
                "时间 (最早)" => query.OrderBy(x => x.InboundDate),
                "产品名称" => query.OrderBy(x => x.ProductName),
                "供应商" => query.OrderBy(x => x.Supplier),
                "状态" => query.OrderBy(x => x.Status),
                _ => query.OrderByDescending(x => x.InboundDate)
            };

            InboundList.Clear();
            foreach (var item in query) InboundList.Add(item);
        }

        [RelayCommand]
        private void Edit(InboundModel item)
        {
            if (item == null) return;
            if (item.Status != "待验收")
            {
                if (MessageBox.Show($"该单据状态为[{item.Status}]，修改可能会影响库存准确性，确认修改吗？", "警告", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            NewInbound = new InboundModel
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Supplier = item.Supplier,
                InboundDate = item.InboundDate,
                Status = item.Status,
                AcceptedQuantity = item.AcceptedQuantity,
                RejectedQuantity = item.RejectedQuantity,
                CheckDate = item.CheckDate
            };
            EntryProductName = item.ProductName ?? "";
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
                    NewInbound.Status = "待验收";
                    NewInbound.AcceptedQuantity = 0;
                    NewInbound.RejectedQuantity = 0;
                }

                await _dbService.SaveInboundOrderAsync(NewInbound);
                await RefreshDataAsync();
                Cancel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private async Task ConfirmAccept(InboundModel item)
        {
            if (item == null) return;
            if (item.Status != "待验收") { MessageBox.Show("只有[待验收]的单据才能进行此操作"); return; }

            var dialog = new AcceptanceDialog(item);
            var result = await DialogHost.Show(dialog, "InboundDialogHost");

            if (result != null && int.TryParse(result.ToString(), out int acceptedQty))
            {
                item.AcceptedQuantity = acceptedQty;
                item.RejectedQuantity = item.Quantity - acceptedQty;
                item.CheckDate = DateTime.Now;

                if (item.AcceptedQuantity == 0)
                    item.Status = "已退货";
                else if (item.RejectedQuantity > 0)
                    item.Status = "已验收";
                else
                    item.Status = "已验收";

                await _dbService.SaveInboundOrderAsync(item);
                await RefreshDataAsync();
                MessageBox.Show($"验收完成！\n合格: {item.AcceptedQuantity}\n退回: {item.RejectedQuantity}");
            }
        }

        [RelayCommand] private void Print() { if (InboundList.Count == 0) MessageBox.Show("无数据"); else _printService.PrintInboundReport(InboundList); }
        [RelayCommand] private void Export() { if (InboundList.Count == 0) MessageBox.Show("无数据"); else _exportService.ExportInbound(InboundList); }
        [RelayCommand] private async Task Delete(InboundModel item) { if (MessageBox.Show("确认删除？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { await _dbService.DeleteInboundOrderAsync(item); await RefreshDataAsync(); if (NewInbound.Id == item.Id) Cancel(); } }
    }
}
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
    public partial class OutboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        public ObservableCollection<OutboundModel> OutboundList { get; } = new();
        public ObservableCollection<string> Customers { get; } = new();
        public ObservableCollection<string> ProductList { get; } = new();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "客户" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => SortData();

        [ObservableProperty] private OutboundModel _newOutbound = new();

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = LoadData();
            _ = LoadCustomers();
            _ = LoadProductList();
        }

        [RelayCommand]
        private void Edit(OutboundModel item)
        {
            if (item == null) return;
            NewOutbound = new OutboundModel
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price,
                Customer = item.Customer,
                OutboundDate = item.OutboundDate
            };
        }

        [RelayCommand]
        private void Cancel()
        {
            NewOutbound = new OutboundModel();
        }

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
                await LoadData();
                await LoadCustomers();
                NewOutbound = new OutboundModel();
            }
            catch (Exception ex) { MessageBox.Show($"保存失败：{ex.Message}"); }
        }

        [RelayCommand]
        private void Print()
        {
            if (OutboundList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
            _printService.PrintOutboundReport(OutboundList);
        }

        // 🟢 确保此方法存在
        [RelayCommand]
        private void Export()
        {
            if (OutboundList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
            _exportService.ExportOutbound(OutboundList);
        }

        [RelayCommand]
        private async Task Delete(OutboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"确认删除单号 [{item.OrderNo}] 吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteOutboundOrderAsync(item);
                await LoadData();
                if (NewOutbound.Id == item.Id) NewOutbound = new OutboundModel();
            }
        }

        private async Task LoadData()
        {
            var list = await _dbService.GetOutboundOrdersAsync();
            OutboundList.Clear();
            foreach (var item in list) OutboundList.Add(item);
            SortData();
        }

        private async Task LoadCustomers()
        {
            var list = await _dbService.GetCustomerListAsync();
            Customers.Clear();
            foreach (var item in list) if (!string.IsNullOrEmpty(item)) Customers.Add(item);
        }

        private async Task LoadProductList()
        {
            var list = await _dbService.GetProductListAsync();
            ProductList.Clear();
            foreach (var item in list) if (!string.IsNullOrEmpty(item)) ProductList.Add(item);
        }

        private void SortData()
        {
            if (OutboundList.Count == 0) return;
            var sorted = SelectedSortOption switch
            {
                "时间 (最新)" => OutboundList.OrderByDescending(x => x.OutboundDate).ToList(),
                "时间 (最早)" => OutboundList.OrderBy(x => x.OutboundDate).ToList(),
                "产品名称" => OutboundList.OrderBy(x => x.ProductName).ToList(),
                "客户" => OutboundList.OrderBy(x => x.Customer).ToList(),
                _ => OutboundList.OrderByDescending(x => x.OutboundDate).ToList()
            };
            OutboundList.Clear();
            foreach (var item in sorted) OutboundList.Add(item);
        }
    }
}
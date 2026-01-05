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
    public partial class InboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;
        private readonly ExportService _exportService;

        public ObservableCollection<InboundModel> InboundList { get; } = new();
        public ObservableCollection<string> Suppliers { get; } = new();

        public ObservableCollection<string> SortOptions { get; } = new() { "时间 (最新)", "时间 (最早)", "产品名称", "供应商" };
        [ObservableProperty] private string _selectedSortOption = "时间 (最新)";
        partial void OnSelectedSortOptionChanged(string value) => SortData();

        [ObservableProperty]
        private InboundModel _newInbound = new();

        public InboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _exportService = new ExportService();
            _ = RefreshDataAsync();
        }

        // 🟢 供外部调用的刷新方法
        public async Task RefreshDataAsync()
        {
            await LoadData();
            await LoadSuppliers();
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
        private void Cancel()
        {
            NewInbound = new InboundModel();
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewInbound.ProductName))
            {
                MessageBox.Show("产品名称不能为空！"); return;
            }
            if (NewInbound.Quantity <= 0)
            {
                MessageBox.Show("数量必须大于 0！"); return;
            }

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
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            if (MessageBox.Show($"确认删除单号 [{item.OrderNo}] 吗？", "删除确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteInboundOrderAsync(item);
                await RefreshDataAsync();
                if (NewInbound.Id == item.Id) NewInbound = new InboundModel();
            }
        }

        private async Task LoadData()
        {
            var list = await _dbService.GetInboundOrdersAsync();
            InboundList.Clear();
            foreach (var item in list) InboundList.Add(item);
            SortData();
        }

        private async Task LoadSuppliers()
        {
            var list = await _dbService.GetSupplierListAsync();
            Suppliers.Clear();
            foreach (var item in list) if (!string.IsNullOrEmpty(item)) Suppliers.Add(item);
        }

        private void SortData()
        {
            if (InboundList.Count == 0) return;
            var sorted = SelectedSortOption switch
            {
                "时间 (最新)" => InboundList.OrderByDescending(x => x.InboundDate).ToList(),
                "时间 (最早)" => InboundList.OrderBy(x => x.InboundDate).ToList(),
                "产品名称" => InboundList.OrderBy(x => x.ProductName).ToList(),
                "供应商" => InboundList.OrderBy(x => x.Supplier).ToList(),
                _ => InboundList.OrderByDescending(x => x.InboundDate).ToList()
            };
            InboundList.Clear();
            foreach (var item in sorted) InboundList.Add(item);
        }
    }
}
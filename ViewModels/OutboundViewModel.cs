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

        // 列表数据源
        public ObservableCollection<OutboundModel> OutboundList { get; } = new();

        // 客户列表
        public ObservableCollection<string> Customers { get; } = new();

        // 🔴 新增：产品名称列表 (用于下拉选择)
        public ObservableCollection<string> ProductList { get; } = new();

        // 排序选项
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "时间 (最新)", "时间 (最早)", "产品名称", "客户"
        };

        [ObservableProperty]
        private string _selectedSortOption = "时间 (最新)";

        partial void OnSelectedSortOptionChanged(string value) => SortData();

        [ObservableProperty]
        private OutboundModel _newOutbound = new();

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _ = LoadData();
            _ = LoadCustomers();
            _ = LoadProductList(); // 🔴 启动时加载产品列表
        }

        // 排序逻辑
        private void SortData()
        {
            if (OutboundList.Count == 0) return;
            var sortedList = SelectedSortOption switch
            {
                "时间 (最新)" => OutboundList.OrderByDescending(x => x.OutboundDate).ToList(),
                "时间 (最早)" => OutboundList.OrderBy(x => x.OutboundDate).ToList(),
                "产品名称" => OutboundList.OrderBy(x => x.ProductName).ToList(),
                "客户" => OutboundList.OrderBy(x => x.Customer).ToList(),
                _ => OutboundList.OrderByDescending(x => x.OutboundDate).ToList()
            };
            OutboundList.Clear();
            foreach (var item in sortedList) OutboundList.Add(item);
        }

        [RelayCommand]
        private void Print()
        {
            if (OutboundList.Count == 0) { MessageBox.Show("当前没有数据可打印！"); return; }
            try { _printService.PrintOutboundReport(OutboundList); }
            catch (Exception ex) { MessageBox.Show($"打印失败：{ex.Message}"); }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewOutbound.ProductName))
            {
                MessageBox.Show("产品名称不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (NewOutbound.Quantity <= 0)
            {
                MessageBox.Show("出库数量必须大于 0！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                NewOutbound.OrderNo = $"CK{DateTime.Now:yyyyMMddHHmmss}";
                NewOutbound.OutboundDate = DateTime.Now;

                if (string.IsNullOrEmpty(NewOutbound.Customer)) NewOutbound.Customer = "散客";

                await _dbService.SaveOutboundOrderAsync(NewOutbound);

                await LoadData();
                await LoadCustomers();
                // 注意：出库不会产生新产品名，所以不需要刷新 ProductList

                NewOutbound = new OutboundModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"出库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Delete(OutboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"确认删除单号 [{item.OrderNo}] 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteOutboundOrderAsync(item);
                await LoadData();
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

        // 🔴 加载产品列表的方法
        private async Task LoadProductList()
        {
            var list = await _dbService.GetProductListAsync();
            ProductList.Clear();
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item)) ProductList.Add(item);
            }
        }
    }
}
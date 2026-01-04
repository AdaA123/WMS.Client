using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq; // ✅ 必须引用
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class InboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService; // 1. 引入打印服务

        public ObservableCollection<InboundModel> InboundList { get; } = new();
        public ObservableCollection<string> Suppliers { get; } = new();

        // 排序选项
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "时间 (最新)",
            "时间 (最早)",
            "产品名称",
            "供应商"
        };

        [ObservableProperty]
        private string _selectedSortOption = "时间 (最新)";

        // 监听排序选项变化
        partial void OnSelectedSortOptionChanged(string value)
        {
            SortData();
        }

        [ObservableProperty]
        private InboundModel _newInbound = new();

        public InboundViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService(); // 2. 初始化打印服务
            _ = LoadData();
            _ = LoadSuppliers();
        }

        // 排序逻辑
        private void SortData()
        {
            if (InboundList.Count == 0) return;

            var sortedList = SelectedSortOption switch
            {
                "时间 (最新)" => InboundList.OrderByDescending(x => x.InboundDate).ToList(),
                "时间 (最早)" => InboundList.OrderBy(x => x.InboundDate).ToList(),
                "产品名称" => InboundList.OrderBy(x => x.ProductName).ToList(),
                "供应商" => InboundList.OrderBy(x => x.Supplier).ToList(),
                _ => InboundList.OrderByDescending(x => x.InboundDate).ToList()
            };

            InboundList.Clear();
            foreach (var item in sortedList)
            {
                InboundList.Add(item);
            }
        }

        // 3. 打印命令
        [RelayCommand]
        private void Print()
        {
            if (InboundList.Count == 0)
            {
                MessageBox.Show("当前没有数据可打印！", "提示");
                return;
            }

            try
            {
                _printService.PrintInboundReport(InboundList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewInbound.ProductName))
            {
                MessageBox.Show("产品名称不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (NewInbound.Quantity <= 0)
            {
                MessageBox.Show("入库数量必须大于 0！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                NewInbound.OrderNo = $"RK{DateTime.Now:yyyyMMddHHmmss}";
                NewInbound.InboundDate = DateTime.Now;

                await _dbService.SaveInboundOrderAsync(NewInbound);

                await LoadData();
                await LoadSuppliers();

                NewInbound = new InboundModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Delete(InboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"确定要删除单号 [{item.OrderNo}] 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteInboundOrderAsync(item);
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            var list = await _dbService.GetInboundOrdersAsync();
            InboundList.Clear();
            foreach (var item in list) InboundList.Add(item);

            // 加载完数据后应用排序
            SortData();
        }

        private async Task LoadSuppliers()
        {
            var list = await _dbService.GetSupplierListAsync();
            Suppliers.Clear();
            foreach (var item in list) if (!string.IsNullOrEmpty(item)) Suppliers.Add(item);
        }
    }
}
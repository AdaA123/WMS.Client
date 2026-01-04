using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq; // ✅ 必须引用，用于排序
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class OutboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        // 列表数据源
        public ObservableCollection<OutboundModel> OutboundList { get; } = new();
        // 客户下拉框数据源
        public ObservableCollection<string> Customers { get; } = new();

        // 🔴 1. 补全：排序选项列表
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "时间 (最新)",
            "时间 (最早)",
            "产品名称",
            "客户"
        };

        // 🔴 2. 补全：当前选中的排序方式
        // 漏了这一段，就会报 CS0103 和 CS0759 错误
        [ObservableProperty]
        private string _selectedSortOption = "时间 (最新)";

        // 🔴 3. 补全：监听排序变化
        partial void OnSelectedSortOptionChanged(string value)
        {
            SortData();
        }

        // 绑定输入框的对象
        [ObservableProperty]
        private OutboundModel _newOutbound = new();

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            _ = LoadData();
            _ = LoadCustomers();
        }

        // 🔴 4. 补全：排序逻辑
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
            foreach (var item in sortedList)
            {
                OutboundList.Add(item);
            }
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

                if (string.IsNullOrEmpty(NewOutbound.Customer))
                    NewOutbound.Customer = "散客";

                await _dbService.SaveOutboundOrderAsync(NewOutbound);

                await LoadData();
                await LoadCustomers();

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

            // 加载完后自动排序
            SortData();
        }

        private async Task LoadCustomers()
        {
            var list = await _dbService.GetCustomerListAsync();
            Customers.Clear();
            foreach (var item in list)
                if (!string.IsNullOrEmpty(item)) Customers.Add(item);
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
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

        // ✅ 解决 CS0103：添加输入对象绑定
        [ObservableProperty]
        private OutboundModel _newOutbound = new();

        public OutboundViewModel()
        {
            _dbService = new DatabaseService();
            _ = LoadData();
            _ = LoadCustomers();
        }

        // ✅ 保存逻辑 (对应界面“确认出库”)
        [RelayCommand]
        private async Task Save()
        {
            // 1. 校验
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
                // 2. 自动生成单号 CK + 时间
                NewOutbound.OrderNo = $"CK{DateTime.Now:yyyyMMddHHmmss}";
                NewOutbound.OutboundDate = DateTime.Now;

                // 默认客户
                if (string.IsNullOrEmpty(NewOutbound.Customer))
                    NewOutbound.Customer = "散客";

                // 3. 保存
                await _dbService.SaveOutboundOrderAsync(NewOutbound);

                // 4. 刷新
                await LoadData();
                await LoadCustomers();

                // 5. 清空输入
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
            list.Reverse(); // 新的在上面
            foreach (var item in list) OutboundList.Add(item);
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
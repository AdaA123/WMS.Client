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
    public partial class InboundViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        // 列表數據源
        public ObservableCollection<InboundModel> InboundList { get; } = new();
        // 供應商下拉框數據源
        public ObservableCollection<string> Suppliers { get; } = new();

        // 輸入框綁定對象
        [ObservableProperty]
        private InboundModel _newInbound = new();

        public InboundViewModel()
        {
            _dbService = new DatabaseService();
            _ = LoadData();
            _ = LoadSuppliers();
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(NewInbound.ProductName))
            {
                MessageBox.Show("產品名稱不能為空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (NewInbound.Quantity <= 0)
            {
                MessageBox.Show("入庫數量必須大於 0！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 自動生成單號 RK + 時間戳
                NewInbound.OrderNo = $"RK{DateTime.Now:yyyyMMddHHmmss}";
                NewInbound.InboundDate = DateTime.Now;

                // 保存到數據庫 (這裡會自動使用 Quantity 和 Price 字段)
                await _dbService.SaveInboundOrderAsync(NewInbound);

                // 刷新界面
                await LoadData();
                await LoadSuppliers();

                // 清空輸入框
                NewInbound = new InboundModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Delete(InboundModel item)
        {
            if (item == null) return;
            if (MessageBox.Show($"確定要刪除單號 [{item.OrderNo}] 嗎？", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteInboundOrderAsync(item);
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            var list = await _dbService.GetInboundOrdersAsync();
            InboundList.Clear();
            list.Reverse(); // 最新數據在最上面
            foreach (var item in list) InboundList.Add(item);
        }

        private async Task LoadSuppliers()
        {
            var list = await _dbService.GetSupplierListAsync();
            Suppliers.Clear();
            foreach (var item in list) if (!string.IsNullOrEmpty(item)) Suppliers.Add(item);
        }
    }
}
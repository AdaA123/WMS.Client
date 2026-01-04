using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // ✅ 引用 RelayCommand
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService; // 1. 引入打印服务

        // 三个卡片的数字
        [ObservableProperty] private int _totalInbound;
        [ObservableProperty] private int _totalOutbound;
        [ObservableProperty] private int _currentStock;

        // 用于绑定表格的库存汇总列表
        public ObservableCollection<InventorySummaryModel> SummaryList { get; } = new();

        public HomeViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService(); // 2. 初始化
            LoadDashboardData();
        }

        // 🔴 3. 打印命令
        [RelayCommand]
        private void Print()
        {
            if (SummaryList.Count == 0)
            {
                MessageBox.Show("当前没有数据可打印！", "提示");
                return;
            }

            try
            {
                _printService.PrintInventoryReport(SummaryList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 加载数据
        public async void LoadDashboardData()
        {
            // 1. 加载顶部卡片统计
            TotalInbound = await _dbService.GetTotalInboundCountAsync();
            TotalOutbound = await _dbService.GetTotalOutboundCountAsync();
            CurrentStock = TotalInbound - TotalOutbound;

            // 2. 加载底部详细汇总
            var summary = await _dbService.GetInventorySummaryAsync();

            SummaryList.Clear();
            foreach (var item in summary)
            {
                SummaryList.Add(item);
            }
        }
    }
}
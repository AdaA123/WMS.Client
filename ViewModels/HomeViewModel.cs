using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel; // ✅ 引用集合
using System.Threading.Tasks;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        // 三个卡片的数字
        [ObservableProperty] private int _totalInbound;
        [ObservableProperty] private int _totalOutbound;
        [ObservableProperty] private int _currentStock;

        // 🔴 新增：用于绑定表格的库存汇总列表
        public ObservableCollection<InventorySummaryModel> SummaryList { get; } = new();

        public HomeViewModel()
        {
            _dbService = new DatabaseService();
            LoadDashboardData();
        }

        // 加载数据
        public async void LoadDashboardData()
        {
            // 1. 加载顶部卡片统计 (统计单据数量)
            TotalInbound = await _dbService.GetTotalInboundCountAsync();
            TotalOutbound = await _dbService.GetTotalOutboundCountAsync();
            CurrentStock = TotalInbound - TotalOutbound;

            // 2. 加载底部详细汇总 (按产品合并)
            var summary = await _dbService.GetInventorySummaryAsync();

            SummaryList.Clear();
            foreach (var item in summary)
            {
                SummaryList.Add(item);
            }
        }
    }
}
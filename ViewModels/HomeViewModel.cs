using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        // 三个要显示的数字
        [ObservableProperty] private int _totalInbound;
        [ObservableProperty] private int _totalOutbound;
        [ObservableProperty] private int _currentStock;

        public HomeViewModel()
        {
            _dbService = new DatabaseService();
            LoadDashboardData();
        }

        // 加载数据
        public async void LoadDashboardData()
        {
            TotalInbound = await _dbService.GetTotalInboundCountAsync();
            TotalOutbound = await _dbService.GetTotalOutboundCountAsync();

            // 计算库存
            CurrentStock = TotalInbound - TotalOutbound;
        }
    }
}
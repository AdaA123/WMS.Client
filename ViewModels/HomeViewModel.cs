using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;

namespace WMS.Client.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PrintService _printService;

        [ObservableProperty]
        private int _totalInbound;

        [ObservableProperty]
        private int _totalOutbound;

        [ObservableProperty]
        private int _totalReturn;

        public ObservableCollection<InventorySummaryModel> InventoryList { get; } = new();

        public HomeViewModel()
        {
            _dbService = new DatabaseService();
            _printService = new PrintService();
            _ = LoadDashboardData();
        }

        [RelayCommand]
        public async Task LoadDashboardData()
        {
            var inbounds = await _dbService.GetInboundOrdersAsync();
            TotalInbound = inbounds.Sum(x => x.Quantity);

            var outbounds = await _dbService.GetOutboundOrdersAsync();
            TotalOutbound = outbounds.Sum(x => x.Quantity);

            var returns = await _dbService.GetReturnOrdersAsync();
            TotalReturn = returns.Sum(x => x.Quantity);

            var summary = await _dbService.GetInventorySummaryAsync();
            InventoryList.Clear();
            foreach (var item in summary) InventoryList.Add(item);
        }

        [RelayCommand]
        private void Print()
        {
            if (InventoryList.Count == 0) { MessageBox.Show("当前没有数据可打印"); return; }
            _printService.PrintInventoryReport(InventoryList);
        }
    }
}
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
    public partial class FinancialViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly ExportService _exportService;
        private readonly PrintService _printService;

        // 顶部总览卡片数据
        [ObservableProperty] private decimal _totalRevenue; // 总收入
        [ObservableProperty] private decimal _totalCost;    // 总成本
        [ObservableProperty] private decimal _totalGrossProfit; // 总毛利

        // 表格数据列表
        public ObservableCollection<FinancialSummaryModel> FinancialList { get; } = new();

        public FinancialViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _printService = new PrintService();

            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            var data = await _dbService.GetFinancialSummaryAsync();

            FinancialList.Clear();
            foreach (var item in data)
            {
                FinancialList.Add(item);
            }

            // 计算顶部卡片总和
            TotalRevenue = FinancialList.Sum(x => x.TotalRevenue);
            TotalCost = FinancialList.Sum(x => x.TotalCost);
            // 毛利 = 收入 - 成本 - 退款
            TotalGrossProfit = TotalRevenue - TotalCost - FinancialList.Sum(x => x.TotalRefund);
        }

        [RelayCommand]
        private void Export()
        {
            if (FinancialList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
            _exportService.ExportFinancials(FinancialList);
        }

        [RelayCommand]
        private void Print()
        {
            if (FinancialList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
            _printService.PrintFinancialReport(FinancialList);
        }
    }
}
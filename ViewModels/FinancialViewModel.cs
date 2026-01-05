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
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private decimal _totalCost;
        [ObservableProperty] private decimal _totalGrossProfit;

        // 表格数据列表
        public ObservableCollection<FinancialSummaryModel> FinancialList { get; } = new(); // 单品分析
        public ObservableCollection<FinancialReportModel> MonthlyList { get; } = new();    // 月度报表
        public ObservableCollection<FinancialReportModel> YearlyList { get; } = new();     // 年度报表

        // 当前选中的标签页索引 (0=单品, 1=月度, 2=年度)
        [ObservableProperty] private int _selectedTabIndex;

        public FinancialViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _printService = new PrintService();

            _ = RefreshDataAsync();
        }

        public async Task RefreshDataAsync()
        {
            // 1. 加载单品分析
            var data = await _dbService.GetFinancialSummaryAsync();
            FinancialList.Clear();
            foreach (var item in data) FinancialList.Add(item);

            // 计算顶部卡片
            TotalRevenue = FinancialList.Sum(x => x.TotalRevenue);
            TotalCost = FinancialList.Sum(x => x.TotalCost);
            TotalGrossProfit = TotalRevenue - TotalCost - FinancialList.Sum(x => x.TotalRefund);

            // 2. 加载月度报表
            var monthData = await _dbService.GetPeriodReportAsync(isMonthly: true);
            MonthlyList.Clear();
            foreach (var item in monthData) MonthlyList.Add(item);

            // 3. 加载年度报表
            var yearData = await _dbService.GetPeriodReportAsync(isMonthly: false);
            YearlyList.Clear();
            foreach (var item in yearData) YearlyList.Add(item);
        }

        [RelayCommand]
        private void Export()
        {
            if (SelectedTabIndex == 0)
            {
                if (FinancialList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
                _exportService.ExportFinancials(FinancialList);
            }
            else if (SelectedTabIndex == 1)
            {
                if (MonthlyList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
                _exportService.ExportPeriodReport(MonthlyList, "月度财务"); // 🟢 修正为简体
            }
            else if (SelectedTabIndex == 2)
            {
                if (YearlyList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
                _exportService.ExportPeriodReport(YearlyList, "年度财务"); // 🟢 修正为简体
            }
        }

        [RelayCommand]
        private void Print()
        {
            if (SelectedTabIndex == 0)
            {
                if (FinancialList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
                _printService.PrintFinancialReport(FinancialList);
            }
            else if (SelectedTabIndex == 1)
            {
                if (MonthlyList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
                _printService.PrintPeriodReport(MonthlyList, "月度财务报表"); // 🟢 修正为简体
            }
            else if (SelectedTabIndex == 2)
            {
                if (YearlyList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
                _printService.PrintPeriodReport(YearlyList, "年度财务报表"); // 🟢 修正为简体
            }
        }
    }
}
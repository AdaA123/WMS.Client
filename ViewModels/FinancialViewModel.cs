using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private decimal _totalCost;
        [ObservableProperty] private decimal _totalGrossProfit;

        // 🟢 日期筛选
        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private DateTime _endDate;

        public ObservableCollection<FinancialSummaryModel> FinancialList { get; } = new();
        public ObservableCollection<FinancialReportModel> MonthlyList { get; } = new();
        public ObservableCollection<FinancialReportModel> YearlyList { get; } = new();

        [ObservableProperty] private int _selectedTabIndex;

        public FinancialViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _printService = new PrintService();

            // 🟢 默认显示今年的数据
            StartDate = new DateTime(DateTime.Now.Year, 1, 1);
            EndDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1); // 今天的最后一刻

            _ = RefreshDataAsync();
        }

        // 🟢 刷新数据命令 (点击按钮触发)
        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            if (StartDate > EndDate)
            {
                MessageBox.Show("开始日期不能晚于结束日期！");
                return;
            }

            // 1. 加载单品分析 (带日期筛选)
            var data = await _dbService.GetFinancialSummaryAsync(StartDate, EndDate);
            FinancialList.Clear();
            foreach (var item in data) FinancialList.Add(item);

            // 计算顶部卡片
            TotalRevenue = FinancialList.Sum(x => x.TotalRevenue);
            TotalCost = FinancialList.Sum(x => x.TotalCost);
            TotalGrossProfit = TotalRevenue - TotalCost - FinancialList.Sum(x => x.TotalRefund);

            // 2. 加载月度报表 (带日期筛选)
            var monthData = await _dbService.GetPeriodReportAsync(isMonthly: true, StartDate, EndDate);
            MonthlyList.Clear();
            foreach (var item in monthData) MonthlyList.Add(item);

            // 3. 加载年度报表 (带日期筛选)
            var yearData = await _dbService.GetPeriodReportAsync(isMonthly: false, StartDate, EndDate);
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
                _exportService.ExportPeriodReport(MonthlyList, "月度财务");
            }
            else if (SelectedTabIndex == 2)
            {
                if (YearlyList.Count == 0) { MessageBox.Show("无数据可导出"); return; }
                _exportService.ExportPeriodReport(YearlyList, "年度财务");
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
                _printService.PrintPeriodReport(MonthlyList, "月度财务报表");
            }
            else if (SelectedTabIndex == 2)
            {
                if (YearlyList.Count == 0) { MessageBox.Show("无数据可打印"); return; }
                _printService.PrintPeriodReport(YearlyList, "年度财务报表");
            }
        }
    }
}
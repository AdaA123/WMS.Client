using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic; // 引入 List
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

        // --- 顶部卡片数据 ---
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private decimal _totalCost;
        [ObservableProperty] private decimal _totalGrossProfit;

        // --- 筛选条件 ---
        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private DateTime _endDate;

        // 🟢 缓存单品分析数据
        private List<FinancialSummaryModel> _cachedFinancialList = new();

        // 🟢 搜索属性
        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => FilterFinancialList();

        // --- 表格数据 ---
        public ObservableCollection<FinancialSummaryModel> FinancialList { get; } = new();
        public ObservableCollection<FinancialReportModel> MonthlyList { get; } = new();
        public ObservableCollection<FinancialReportModel> YearlyList { get; } = new();

        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private bool _isChartExpanded = true;

        // --- 图表数据 ---
        [ObservableProperty]
        private SeriesCollection _chartSeries = new SeriesCollection();
        [ObservableProperty]
        private string[] _chartLabels = Array.Empty<string>();

        public Func<double, string> YFormatter { get; set; }

        public FinancialViewModel()
        {
            _dbService = new DatabaseService();
            _exportService = new ExportService();
            _printService = new PrintService();

            YFormatter = value => value.ToString("C0");

            StartDate = new DateTime(DateTime.Now.Year, 1, 1);
            EndDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

            _ = RefreshDataAsync();
        }

        [RelayCommand]
        public async Task RefreshDataAsync()
        {
            if (StartDate > EndDate)
            {
                MessageBox.Show("开始日期不能晚于结束日期！");
                return;
            }

            // 1. 获取并缓存单品数据
            _cachedFinancialList = await _dbService.GetFinancialSummaryAsync(StartDate, EndDate);

            // 2. 应用过滤
            FilterFinancialList();

            // 3. 计算总额 (基于缓存的全量数据)
            TotalRevenue = _cachedFinancialList.Sum(x => x.TotalRevenue);
            TotalCost = _cachedFinancialList.Sum(x => x.TotalCost);
            TotalGrossProfit = TotalRevenue - TotalCost - _cachedFinancialList.Sum(x => x.TotalRefund);

            var monthData = await _dbService.GetPeriodReportAsync(isMonthly: true, StartDate, EndDate);
            MonthlyList.Clear();
            foreach (var item in monthData) MonthlyList.Add(item);

            var yearData = await _dbService.GetPeriodReportAsync(isMonthly: false, StartDate, EndDate);
            YearlyList.Clear();
            foreach (var item in yearData) YearlyList.Add(item);

            UpdateChart(monthData);
        }

        private void FilterFinancialList()
        {
            FinancialList.Clear();
            var query = _cachedFinancialList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string key = SearchText.Trim().ToLower();
                query = query.Where(x => x.ProductName != null && x.ProductName.ToLower().Contains(key));
            }

            foreach (var item in query) FinancialList.Add(item);
        }

        private void UpdateChart(List<FinancialReportModel> data)
        {
            var sortedData = data.OrderBy(x => x.PeriodDate).ToList();
            ChartLabels = sortedData.Select(x => x.PeriodName).ToArray();

            ChartSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "总收入",
                    Values = new ChartValues<decimal>(sortedData.Select(x => x.Revenue)),
                    Fill = System.Windows.Media.Brushes.MediumSeaGreen
                },
                new ColumnSeries
                {
                    Title = "总成本",
                    Values = new ChartValues<decimal>(sortedData.Select(x => x.Cost)),
                    Fill = System.Windows.Media.Brushes.IndianRed
                },
                new LineSeries
                {
                    Title = "净利润趋势",
                    Values = new ChartValues<decimal>(sortedData.Select(x => x.Profit)),
                    Stroke = System.Windows.Media.Brushes.DodgerBlue,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    PointGeometrySize = 10,
                    StrokeThickness = 3
                }
            };
        }

        [RelayCommand]
        private void Export()
        {
            if (SelectedTabIndex == 0) _exportService.ExportFinancials(FinancialList);
            else if (SelectedTabIndex == 1) _exportService.ExportPeriodReport(MonthlyList, "月度财务");
            else if (SelectedTabIndex == 2) _exportService.ExportPeriodReport(YearlyList, "年度财务");
        }

        [RelayCommand]
        private void Print()
        {
            if (SelectedTabIndex == 0) _printService.PrintFinancialReport(FinancialList);
            else if (SelectedTabIndex == 1) _printService.PrintPeriodReport(MonthlyList, "月度财务报表");
            else if (SelectedTabIndex == 2) _printService.PrintPeriodReport(YearlyList, "年度财务报表");
        }
    }
}
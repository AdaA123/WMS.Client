using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;
using MaterialDesignThemes.Wpf; // 引入 DialogHost 支持

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

        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private DateTime _endDate;

        private List<FinancialSummaryModel> _cachedFinancialList = new();

        [ObservableProperty] private string _searchText = "";
        partial void OnSearchTextChanged(string value) => FilterFinancialList();

        public ObservableCollection<FinancialSummaryModel> FinancialList { get; } = new();
        public ObservableCollection<FinancialReportModel> MonthlyList { get; } = new();
        public ObservableCollection<FinancialReportModel> YearlyList { get; } = new();

        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private bool _isChartExpanded = true;

        // --- 柱状/折线图数据 ---
        [ObservableProperty]
        private SeriesCollection _chartSeries = new SeriesCollection();
        [ObservableProperty]
        private string[] _chartLabels = Array.Empty<string>();

        // 🟢 新增：产品收入占比饼图
        public SeriesCollection RevenuePieSeries { get; } = new();

        // 详情页数据源
        public ObservableCollection<InboundModel> DetailInbounds { get; } = new();
        public ObservableCollection<OutboundModel> DetailOutbounds { get; } = new();
        public ObservableCollection<ReturnModel> DetailReturns { get; } = new();
        [ObservableProperty] private string _detailTitle = "";

        private ProductModel? _currentDetailProduct;
        private string? _currentPeriodTitle;

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

            _cachedFinancialList = await _dbService.GetFinancialSummaryAsync(StartDate, EndDate);
            FilterFinancialList();
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

            // 🟢 更新饼图 (响应筛选结果)
            UpdatePieChart(query);
        }

        private void UpdatePieChart(IEnumerable<FinancialSummaryModel> data)
        {
            RevenuePieSeries.Clear();
            var topRevenue = data.OrderByDescending(x => x.TotalRevenue).Take(5);
            foreach (var item in topRevenue)
            {
                RevenuePieSeries.Add(new PieSeries
                {
                    Title = item.ProductName,
                    Values = new ChartValues<decimal> { item.TotalRevenue },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:C0}"
                });
            }
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
        private async Task ViewDetail(FinancialSummaryModel item)
        {
            if (item == null || string.IsNullOrEmpty(item.ProductName)) return;
            DetailTitle = $"商品详情：{item.ProductName}";
            _currentDetailProduct = (await _dbService.GetProductsAsync()).FirstOrDefault(p => p.Name == item.ProductName)
                                    ?? new ProductModel { Name = item.ProductName, Spec = "未知", Unit = "未知", Price = 0 };
            _currentPeriodTitle = null;

            var t1 = _dbService.GetInboundsByProductAsync(item.ProductName);
            var t2 = _dbService.GetOutboundsByProductAsync(item.ProductName);
            var t3 = _dbService.GetReturnsByProductAsync(item.ProductName);
            await Task.WhenAll(t1, t2, t3);

            FillDetailLists(t1.Result, t2.Result, t3.Result);
            await DialogHost.Show(new ProductDetailDialog { DataContext = this }, "RootDialog");
        }

        [RelayCommand]
        private async Task ViewPeriodDetail(FinancialReportModel item)
        {
            if (item == null) return;
            DateTime start = item.PeriodDate;
            DateTime end;
            if (item.PeriodName != null && item.PeriodName.Contains("月"))
            {
                end = start.AddMonths(1).AddSeconds(-1);
                DetailTitle = $"月度流水详情：{item.PeriodName}";
            }
            else
            {
                end = start.AddYears(1).AddSeconds(-1);
                DetailTitle = $"年度流水详情：{item.PeriodName}";
            }
            _currentPeriodTitle = DetailTitle;
            _currentDetailProduct = null;

            var t1 = _dbService.GetInboundsByDateRangeAsync(start, end);
            var t2 = _dbService.GetOutboundsByDateRangeAsync(start, end);
            var t3 = _dbService.GetReturnsByDateRangeAsync(start, end);
            await Task.WhenAll(t1, t2, t3);

            FillDetailLists(t1.Result, t2.Result, t3.Result);
            await DialogHost.Show(new PeriodDetailDialog { DataContext = this }, "RootDialog");
        }

        private void FillDetailLists(IEnumerable<InboundModel> inbounds, IEnumerable<OutboundModel> outbounds, IEnumerable<ReturnModel> returns)
        {
            DetailInbounds.Clear(); foreach (var i in inbounds) DetailInbounds.Add(i);
            DetailOutbounds.Clear(); foreach (var i in outbounds) DetailOutbounds.Add(i);
            DetailReturns.Clear(); foreach (var i in returns) DetailReturns.Add(i);
        }

        [RelayCommand]
        private void PrintDetail()
        {
            if (_currentDetailProduct != null)
                _printService.PrintProductDetails(_currentDetailProduct, DetailInbounds, DetailOutbounds, DetailReturns);
            else if (!string.IsNullOrEmpty(_currentPeriodTitle))
                _printService.PrintPeriodDetails(_currentPeriodTitle, DetailInbounds, DetailOutbounds, DetailReturns);
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
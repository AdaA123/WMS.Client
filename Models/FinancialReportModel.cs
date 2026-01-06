using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace WMS.Client.Models
{
    public partial class FinancialReportModel : ObservableObject
    {
        public string PeriodName { get; set; } = string.Empty;
        public DateTime PeriodDate { get; set; }

        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Refund { get; set; }
        public decimal Profit => Revenue - Cost - Refund;
        public string ProfitMargin => Revenue == 0 ? "0%" : $"{(Profit / Revenue):P1}";

        public List<FinancialDetailModel> Details { get; set; } = new List<FinancialDetailModel>();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
    }

    public class FinancialDetailModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Refund { get; set; }

        // 🟢 新增：销售利润 (收入 - 成本)
        public decimal SalesProfit => Revenue - Cost;

        // 最终贡献毛利
        public decimal Profit => Revenue - Cost - Refund;
    }
}
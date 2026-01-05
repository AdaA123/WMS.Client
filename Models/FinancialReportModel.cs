using System;
using System.Collections.Generic;

namespace WMS.Client.Models
{
    public class FinancialReportModel
    {
        public string PeriodName { get; set; } // 顯示名稱 (如 2023-10 月)
        public DateTime PeriodDate { get; set; } // 🟢 新增：實際日期，用於排序和篩選

        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Refund { get; set; }
        public decimal Profit => Revenue - Cost - Refund;
        public string ProfitMargin => Revenue == 0 ? "0%" : $"{(Profit / Revenue):P1}";

        public List<FinancialDetailModel> Details { get; set; } = new List<FinancialDetailModel>();
    }

    public class FinancialDetailModel
    {
        public string ProductName { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Refund { get; set; }
        public decimal Profit => Revenue - Cost - Refund;
    }
}
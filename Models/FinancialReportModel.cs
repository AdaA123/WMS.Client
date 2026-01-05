using System.Collections.Generic;

namespace WMS.Client.Models
{
    // 主报表模型 (月/年)
    public class FinancialReportModel
    {
        public string PeriodName { get; set; } // 时间段名称
        public decimal Revenue { get; set; }   // 总收入
        public decimal Cost { get; set; }      // 总成本
        public decimal Refund { get; set; }    // 总退款
        public decimal Profit => Revenue - Cost - Refund; // 毛利
        public string ProfitMargin => Revenue == 0 ? "0%" : $"{(Profit / Revenue):P1}";

        // 🟢 新增：该时间段内的详细数据列表
        public List<FinancialDetailModel> Details { get; set; } = new List<FinancialDetailModel>();
    }

    // 🟢 新增：明细模型 (具体产品的财务数据)
    public class FinancialDetailModel
    {
        public string ProductName { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Refund { get; set; }
        public decimal Profit => Revenue - Cost - Refund;
    }
}
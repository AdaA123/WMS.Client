namespace WMS.Client.Models
{
    public class FinancialSummaryModel
    {
        // 初始化为 string.Empty 消除 CS8618 警告
        public string ProductName { get; set; } = string.Empty;

        public decimal TotalCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefund { get; set; }

        // 🟢 新增：销售利润 (纯销售带来的利润：收入 - 成本，不含退款)
        public decimal SalesProfit => TotalRevenue - TotalCost;

        // 最终毛利 (减去退款后)
        public decimal GrossProfit => TotalRevenue - TotalCost - TotalRefund;
    }
}
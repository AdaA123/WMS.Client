namespace WMS.Client.Models
{
    public class FinancialSummaryModel
    {
        // 🟢 修复：初始化为 string.Empty 消除 CS8618 警告
        public string ProductName { get; set; } = string.Empty;

        public decimal TotalCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefund { get; set; }

        public decimal GrossProfit => TotalRevenue - TotalCost - TotalRefund;
    }
}
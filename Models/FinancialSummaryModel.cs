namespace WMS.Client.Models
{
    public class FinancialSummaryModel
    {
        public string ProductName { get; set; }

        // 采购总成本 (入库数量 * 单价)
        public decimal TotalCost { get; set; }

        // 销售总收入 (出库数量 * 售价)
        public decimal TotalRevenue { get; set; }

        // 退款总额
        public decimal TotalRefund { get; set; }

        // 简单毛利计算 (收入 - 成本 - 退款)
        // 注意：这更接近“现金流”概念，如果正在大量备货，此数值可能为负
        public decimal GrossProfit => TotalRevenue - TotalCost - TotalRefund;
    }
}
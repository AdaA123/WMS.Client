namespace WMS.Client.Models
{
    public class InventorySummaryModel
    {
        public string ProductName { get; set; } = string.Empty; // 产品名称
        public int TotalInbound { get; set; }  // 入库总数
        public int TotalOutbound { get; set; } // 出库总数
        public int CurrentStock { get; set; }  // 当前库存
    }
}
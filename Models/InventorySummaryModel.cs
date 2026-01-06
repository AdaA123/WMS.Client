namespace WMS.Client.Models
{
    public class InventorySummaryModel
    {
        public string? ProductName { get; set; }
        public int TotalInbound { get; set; }
        public int TotalOutbound { get; set; }
        public int CurrentStock { get; set; }

        // 新增：库存总值
        public decimal TotalAmount { get; set; }
    }
}
using SQLite;
using System;

namespace WMS.Client.Models
{
    public class InboundModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // ✅ 初始化为空字符串，消除警告
        public string OrderNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public string Supplier { get; set; } = string.Empty;

        public DateTime InboundDate { get; set; }
    }
}
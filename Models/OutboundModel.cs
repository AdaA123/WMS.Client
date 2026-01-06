using SQLite;
using System;

namespace WMS.Client.Models
{
    public class OutboundModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? OrderNo { get; set; }
        public string? ProductName { get; set; }
        public string? Customer { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // 售价
        public DateTime OutboundDate { get; set; }

        // 新增：总金额
        [Ignore]
        public decimal TotalAmount => Quantity * Price;
    }
}
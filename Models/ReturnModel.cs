using SQLite;
using System;

namespace WMS.Client.Models
{
    public class ReturnModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string? ReturnNo { get; set; }
        public string? ProductName { get; set; }
        public string? Customer { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // 退款单价
        public string? Reason { get; set; }
        public DateTime ReturnDate { get; set; }

        // 新增：总金额
        [Ignore]
        public decimal TotalAmount => Quantity * Price;
    }
}
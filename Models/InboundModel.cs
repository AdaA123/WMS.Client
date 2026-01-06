using SQLite;
using System;

namespace WMS.Client.Models
{
    public class InboundModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string? OrderNo { get; set; }

        public string? ProductName { get; set; }

        public string? Supplier { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; } // 单价

        public DateTime InboundDate { get; set; }

        // ==========================================
        // 🟢 新增：总金额计算属性
        // ==========================================
        // [Ignore] 告诉 SQLite：这个属性只存在于内存中，不要存到数据库表里
        [Ignore]
        public decimal TotalAmount => Quantity * Price;
    }
}
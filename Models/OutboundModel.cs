using SQLite; // 引入这个
using System;

namespace WMS.Client.Models
{
    public class OutboundModel
    {
        // 1. 加上主键和自增
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string OutboundNo { get; set; } = string.Empty;
        // ✅ 新增：产品名称
        public string ProductName { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime Date { get; set; }
    }
}
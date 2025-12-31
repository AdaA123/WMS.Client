using SQLite;
using System;

namespace WMS.Client.Models
{
    public class OutboundModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string OrderNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal Price { get; set; }
        public string Customer { get; set; } = string.Empty;

        public DateTime OutboundDate { get; set; }
    }
}
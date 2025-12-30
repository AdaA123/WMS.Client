using System;

namespace WMS.Client.Models
{
    public class InboundModel
    {
        // 给所有 string 属性加上 = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public int Count { get; set; }
        public DateTime Date { get; set; }
    }
}
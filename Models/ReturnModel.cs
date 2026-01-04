using SQLite;
using System;

namespace WMS.Client.Models
{
    public class ReturnModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ReturnNo { get; set; } = string.Empty;     // 退货单号
        public string ProductName { get; set; } = string.Empty;  // 产品名称

        public int Quantity { get; set; }        // 退货数量 (会增加库存)
        public decimal Price { get; set; }       // 退款金额/单价

        public string Customer { get; set; } = string.Empty;     // 客户
        public string Reason { get; set; } = string.Empty;       // 退货原因

        public DateTime ReturnDate { get; set; } // 退货日期
    }
}
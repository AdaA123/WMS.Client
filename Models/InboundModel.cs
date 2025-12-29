using System;

namespace WMS.Client.Models
{
    public class InboundModel
    {
        public string OrderNo { get; set; }     // 入库单号
        public string Supplier { get; set; }    // 供应商
        public string Status { get; set; }      // 状态 (待入库/已完成)
        public int Count { get; set; }          // 数量
        public DateTime Date { get; set; }      // 日期
    }
}
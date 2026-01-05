using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using WMS.Client.Models;

namespace WMS.Client.Services
{
    public class ExportService
    {
        private void SaveCsv(string title, string header, IEnumerable<string> lines)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = $"导出 {title}",
                    Filter = "CSV 文件 (*.csv)|*.csv",
                    FileName = $"{title}_{DateTime.Now:yyyyMMddHHmm}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    using (var sw = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true)))
                    {
                        sw.WriteLine(header);
                        foreach (var line in lines)
                        {
                            sw.WriteLine(line);
                        }
                    }
                    MessageBox.Show("导出成功！文件已保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出过程中发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🟢 财务报表导出
        public void ExportFinancials(IEnumerable<FinancialSummaryModel> data)
        {
            string header = "产品名称,采购总成本,销售总收入,退款总额,毛利润(现金流)";
            var lines = new List<string>();
            foreach (var item in data)
            {
                string name = item.ProductName?.Replace(",", "，") ?? "";
                lines.Add($"{name},{item.TotalCost},{item.TotalRevenue},{item.TotalRefund},{item.GrossProfit}");
            }
            SaveCsv("财务收支报表", header, lines);
        }

        public void ExportInventory(IEnumerable<InventorySummaryModel> data)
        {
            string header = "产品名称,入库总量,出库总量,当前库存";
            var lines = new List<string>();
            foreach (var item in data)
            {
                string name = item.ProductName?.Replace(",", "，") ?? "";
                lines.Add($"{name},{item.TotalInbound},{item.TotalOutbound},{item.CurrentStock}");
            }
            SaveCsv("当前库存报表", header, lines);
        }

        public void ExportInbound(IEnumerable<InboundModel> data)
        {
            string header = "单号,产品名称,供应商,数量,单价,总金额,日期";
            var lines = new List<string>();
            foreach (var item in data)
            {
                string name = item.ProductName?.Replace(",", "，") ?? "";
                string supp = item.Supplier?.Replace(",", "，") ?? "";
                decimal total = item.Quantity * item.Price;
                lines.Add($"{item.OrderNo},{name},{supp},{item.Quantity},{item.Price},{total},{item.InboundDate:yyyy-MM-dd HH:mm}");
            }
            SaveCsv("入库记录", header, lines);
        }

        public void ExportOutbound(IEnumerable<OutboundModel> data)
        {
            string header = "单号,产品名称,客户,数量,售价,总金额,日期";
            var lines = new List<string>();
            foreach (var item in data)
            {
                string name = item.ProductName?.Replace(",", "，") ?? "";
                string cust = item.Customer?.Replace(",", "，") ?? "";
                decimal total = item.Quantity * item.Price;
                lines.Add($"{item.OrderNo},{name},{cust},{item.Quantity},{item.Price},{total},{item.OutboundDate:yyyy-MM-dd HH:mm}");
            }
            SaveCsv("出库记录", header, lines);
        }

        public void ExportReturn(IEnumerable<ReturnModel> data)
        {
            string header = "单号,产品名称,客户,数量,退款金额,原因,日期";
            var lines = new List<string>();
            foreach (var item in data)
            {
                string name = item.ProductName?.Replace(",", "，") ?? "";
                string cust = item.Customer?.Replace(",", "，") ?? "";
                string reason = item.Reason?.Replace(",", "，") ?? "";
                lines.Add($"{item.ReturnNo},{name},{cust},{item.Quantity},{item.Price},{reason},{item.ReturnDate:yyyy-MM-dd HH:mm}");
            }
            SaveCsv("退货记录", header, lines);
        }
    }
}
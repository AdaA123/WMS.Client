using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using WMS.Client.Models;
using WMS.Client.Views;

namespace WMS.Client.Services
{
    public class PrintService
    {
        private void PrintDocument(FlowDocument doc, string documentName)
        {
            doc.PageWidth = 794;
            doc.PageHeight = 1123;
            doc.PagePadding = new Thickness(40);
            doc.ColumnWidth = double.PositiveInfinity;

            MemoryStream ms = new MemoryStream();
            Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
            Uri packUri = new Uri("pack://temp.xps");

            if (PackageStore.GetPackage(packUri) != null)
                PackageStore.RemovePackage(packUri);
            PackageStore.AddPackage(packUri, package);

            XpsDocument xpsDoc = new XpsDocument(package, CompressionOption.NotCompressed, packUri.ToString());
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
            writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator);

            FixedDocumentSequence fixedDoc = xpsDoc.GetFixedDocumentSequence();

            var previewWindow = new PrintPreviewWindow(fixedDoc);
            previewWindow.Title = $"打印预览 - {documentName}";

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            previewWindow.ShowDialog();

            xpsDoc.Close();
            package.Close();
            ms.Close();
            PackageStore.RemovePackage(packUri);
        }

        // --- 🟢 新增：周期流水明细打印 ---
        public void PrintPeriodDetails(string title, IEnumerable<InboundModel> inbounds, IEnumerable<OutboundModel> outbounds, IEnumerable<ReturnModel> returns)
        {
            var doc = CreateDoc(title);
            AddHeader(doc, $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // 入库记录
            AddSectionTitle(doc, "入库记录");
            var t1 = CreateTable(new string[] { "日期", "产品名称", "供应商", "进货数", "验收数", "单价" });
            foreach (var i in inbounds)
            {
                var r = new TableRow();
                r.Cells.Add(CreateCell(i.InboundDate.ToString("yyyy-MM-dd")));
                r.Cells.Add(CreateCell(i.ProductName ?? ""));
                r.Cells.Add(CreateCell(i.Supplier ?? ""));
                r.Cells.Add(CreateCell(i.Quantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(i.AcceptedQuantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(i.Price.ToString("C2"), false, TextAlignment.Right));
                t1.RowGroups[1].Rows.Add(r);
            }
            doc.Blocks.Add(t1);

            // 出库记录
            AddSectionTitle(doc, "出库记录");
            var t2 = CreateTable(new string[] { "日期", "产品名称", "客户", "数量", "售价" });
            foreach (var o in outbounds)
            {
                var r = new TableRow();
                r.Cells.Add(CreateCell(o.OutboundDate.ToString("yyyy-MM-dd")));
                r.Cells.Add(CreateCell(o.ProductName ?? ""));
                r.Cells.Add(CreateCell(o.Customer ?? ""));
                r.Cells.Add(CreateCell(o.Quantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(o.Price.ToString("C2"), false, TextAlignment.Right));
                t2.RowGroups[1].Rows.Add(r);
            }
            doc.Blocks.Add(t2);

            // 退货记录
            if (returns.Any())
            {
                AddSectionTitle(doc, "退货记录");
                var t3 = CreateTable(new string[] { "日期", "产品名称", "客户", "数量", "原因" });
                foreach (var ret in returns)
                {
                    var r = new TableRow();
                    r.Cells.Add(CreateCell(ret.ReturnDate.ToString("yyyy-MM-dd")));
                    r.Cells.Add(CreateCell(ret.ProductName ?? ""));
                    r.Cells.Add(CreateCell(ret.Customer ?? ""));
                    r.Cells.Add(CreateCell(ret.Quantity.ToString(), false, TextAlignment.Center));
                    r.Cells.Add(CreateCell(ret.Reason ?? ""));
                    t3.RowGroups[1].Rows.Add(r);
                }
                doc.Blocks.Add(t3);
            }

            PrintDocument(doc, "PeriodDetailReport");
        }

        // --- 保持原有的其他打印方法 ---
        public void PrintWholesaleOrder(WholesaleOrder order, IEnumerable<WholesaleItem> items)
        {
            var doc = CreateDoc("批发销售单");
            AddHeader(doc, $"单号：{order.OrderNo}", $"日期：{order.OrderDate:yyyy-MM-dd HH:mm:ss}", $"客户：{order.Customer}", $"地址：{order.Address}");
            var headers = new string[] { "产品名称", "数量", "单价", "小计" };
            var table = CreateTable(headers);
            foreach (var item in items)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.SubTotal.ToString("C2"), false, TextAlignment.Right));
                table.RowGroups[1].Rows.Add(row);
            }
            doc.Blocks.Add(table);
            AddFooter(doc, $"整单合计：{order.TotalAmount:C2}", $"备注：{order.Remark ?? ""}");
            PrintDocument(doc, $"Wholesale_{order.OrderNo}");
        }

        public void PrintSupplierDetails(SupplierModel supplier, IEnumerable<InboundModel> inbounds)
        {
            var doc = CreateDoc("供应商档案详情");
            AddHeader(doc, $"名称：{supplier.Name}", $"联系人：{supplier.ContactPerson}", $"电话：{supplier.Phone}", $"地址：{supplier.Address}");
            AddSectionTitle(doc, "供货记录 (入库)");
            var headers = new string[] { "日期", "单号", "产品名称", "进货数", "验收数", "单价", "状态" };
            var table = CreateTable(headers);
            foreach (var item in inbounds)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.InboundDate.ToString("yyyy-MM-dd")));
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.AcceptedQuantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Status ?? ""));
                table.RowGroups[1].Rows.Add(row);
            }
            doc.Blocks.Add(table);
            if (!string.IsNullOrEmpty(supplier.Remark)) AddFooter(doc, "", $"备注：{supplier.Remark}");
            PrintDocument(doc, $"Supplier_{supplier.Name}");
        }

        public void PrintCustomerDetails(CustomerModel customer, IEnumerable<OutboundModel> outbounds, IEnumerable<ReturnModel> returns)
        {
            var doc = CreateDoc("客户档案详情");
            AddHeader(doc, $"名称：{customer.Name}", $"联系人：{customer.ContactPerson}", $"电话：{customer.Phone}", $"地址：{customer.Address}");
            AddSectionTitle(doc, "销售记录 (出库)");
            var h1 = new string[] { "日期", "单号", "产品名称", "数量", "单价", "小计" };
            var t1 = CreateTable(h1);
            foreach (var item in outbounds)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OutboundDate.ToString("yyyy-MM-dd")));
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell((item.Quantity * item.Price).ToString("C2"), false, TextAlignment.Right));
                t1.RowGroups[1].Rows.Add(row);
            }
            doc.Blocks.Add(t1);
            if (returns.Any())
            {
                AddSectionTitle(doc, "退货记录");
                var h2 = new string[] { "日期", "单号", "产品名称", "数量", "退款额", "原因" };
                var t2 = CreateTable(h2);
                foreach (var item in returns)
                {
                    var row = new TableRow();
                    row.Cells.Add(CreateCell(item.ReturnDate.ToString("yyyy-MM-dd")));
                    row.Cells.Add(CreateCell(item.ReturnNo ?? ""));
                    row.Cells.Add(CreateCell(item.ProductName ?? ""));
                    row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                    row.Cells.Add(CreateCell((item.Quantity * item.Price).ToString("C2"), false, TextAlignment.Right));
                    row.Cells.Add(CreateCell(item.Reason ?? ""));
                    t2.RowGroups[1].Rows.Add(row);
                }
                doc.Blocks.Add(t2);
            }
            if (!string.IsNullOrEmpty(customer.Remark)) AddFooter(doc, "", $"备注：{customer.Remark}");
            PrintDocument(doc, $"Customer_{customer.Name}");
        }

        public void PrintProductDetails(ProductModel product, IEnumerable<InboundModel> inbounds, IEnumerable<OutboundModel> outbounds, IEnumerable<ReturnModel> returns)
        {
            var doc = CreateDoc("商品档案详情");
            AddHeader(doc, $"品名：{product.Name}", $"规格：{product.Spec}", $"单位：{product.Unit}", $"参考价：{product.Price:C2}");
            int inQty = inbounds.Where(x => x.Status == "已验收").Sum(x => x.AcceptedQuantity);
            int outQty = outbounds.Sum(x => x.Quantity);
            int retQty = returns.Sum(x => x.Quantity);
            int stock = inQty - outQty + retQty;
            Paragraph summary = new Paragraph(new Run($"库存概览：总入库 {inQty} | 总出库 {outQty} | 总退货 {retQty} | 当前库存 {stock}"));
            summary.FontSize = 14; summary.FontWeight = FontWeights.Bold; summary.Margin = new Thickness(0, 0, 0, 10);
            doc.Blocks.Add(summary);
            AddSectionTitle(doc, "入库记录");
            var t1 = CreateTable(new string[] { "日期", "供应商", "进货数", "验收数", "单价" });
            foreach (var i in inbounds)
            {
                var r = new TableRow();
                r.Cells.Add(CreateCell(i.InboundDate.ToString("yyyy-MM-dd")));
                r.Cells.Add(CreateCell(i.Supplier ?? ""));
                r.Cells.Add(CreateCell(i.Quantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(i.AcceptedQuantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(i.Price.ToString("C2"), false, TextAlignment.Right));
                t1.RowGroups[1].Rows.Add(r);
            }
            doc.Blocks.Add(t1);
            AddSectionTitle(doc, "出库记录");
            var t2 = CreateTable(new string[] { "日期", "客户", "数量", "售价" });
            foreach (var o in outbounds)
            {
                var r = new TableRow();
                r.Cells.Add(CreateCell(o.OutboundDate.ToString("yyyy-MM-dd")));
                r.Cells.Add(CreateCell(o.Customer ?? ""));
                r.Cells.Add(CreateCell(o.Quantity.ToString(), false, TextAlignment.Center));
                r.Cells.Add(CreateCell(o.Price.ToString("C2"), false, TextAlignment.Right));
                t2.RowGroups[1].Rows.Add(r);
            }
            doc.Blocks.Add(t2);
            PrintDocument(doc, $"Product_{product.Name}");
        }

        public void PrintInboundReport(IEnumerable<InboundModel> data) => PrintReport("入库单汇总报表", new[] { "单号", "产品", "供应商", "进价", "进货", "验收", "拒收", "状态", "日期" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.OrderNo)); r.Cells.Add(CreateCell(i.ProductName)); r.Cells.Add(CreateCell(i.Supplier));
            r.Cells.Add(CreateCell(i.Price.ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.Quantity.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell(i.AcceptedQuantity.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell(i.RejectedQuantity.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell(i.Status)); r.Cells.Add(CreateCell(i.InboundDate.ToString("yyyy-MM-dd")));
        }, $"总金额估算: {data.Sum(x => x.Price * (x.Status == "已验收" ? x.AcceptedQuantity : x.Quantity)):C2}");

        public void PrintOutboundReport(IEnumerable<OutboundModel> data) => PrintReport("出库单汇总报表", new[] { "单号", "产品", "客户", "售价", "数量", "小计", "日期" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.OrderNo)); r.Cells.Add(CreateCell(i.ProductName)); r.Cells.Add(CreateCell(i.Customer));
            r.Cells.Add(CreateCell(i.Price.ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.Quantity.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell((i.Price * i.Quantity).ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.OutboundDate.ToString("yyyy-MM-dd")));
        }, $"销售总金额: {data.Sum(x => x.Price * x.Quantity):C2}");

        public void PrintReturnReport(IEnumerable<ReturnModel> data) => PrintReport("退货单汇总报表", new[] { "单号", "产品", "客户", "单价", "数量", "退款", "原因", "日期" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.ReturnNo)); r.Cells.Add(CreateCell(i.ProductName)); r.Cells.Add(CreateCell(i.Customer));
            r.Cells.Add(CreateCell(i.Price.ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.Quantity.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell((i.Price * i.Quantity).ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.Reason)); r.Cells.Add(CreateCell(i.ReturnDate.ToString("yyyy-MM-dd")));
        }, $"退款总金额: {data.Sum(x => x.Price * x.Quantity):C2}");

        public void PrintFinancialReport(IEnumerable<FinancialSummaryModel> data) => PrintReport("财务收支统计报表", new[] { "产品名称", "采购总成本", "销售总收入", "退款总额", "毛利" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.ProductName)); r.Cells.Add(CreateCell(i.TotalCost.ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.TotalRevenue.ToString("C2"), false, TextAlignment.Right)); r.Cells.Add(CreateCell(i.TotalRefund.ToString("C2"), false, TextAlignment.Right));
            var cell = CreateCell(i.GrossProfit.ToString("C2"), false, TextAlignment.Right); cell.Foreground = i.GrossProfit < 0 ? Brushes.Red : Brushes.Green; r.Cells.Add(cell);
        }, $"总毛利: {data.Sum(x => x.GrossProfit):C2}");

        public void PrintPeriodReport(IEnumerable<FinancialReportModel> data, string title) => PrintReport(title, new[] { "时间段", "收入", "成本", "退款", "利润" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.PeriodName)); r.Cells.Add(CreateCell(i.Revenue.ToString("C2"), false, TextAlignment.Right));
            r.Cells.Add(CreateCell(i.Cost.ToString("C2"), false, TextAlignment.Right)); r.Cells.Add(CreateCell(i.Refund.ToString("C2"), false, TextAlignment.Right));
            var cell = CreateCell(i.Profit.ToString("C2"), false, TextAlignment.Right); cell.Foreground = i.Profit < 0 ? Brushes.Red : Brushes.Green; r.Cells.Add(cell);
        }, $"总利润: {data.Sum(x => x.Profit):C2}");

        public void PrintInventoryReport(IEnumerable<InventorySummaryModel> data) => PrintReport("当前库存汇总报表", new[] { "产品", "入库", "出库", "库存", "货值" }, data, (r, i) => {
            r.Cells.Add(CreateCell(i.ProductName)); r.Cells.Add(CreateCell(i.TotalInbound.ToString(), false, TextAlignment.Center));
            r.Cells.Add(CreateCell(i.TotalOutbound.ToString(), false, TextAlignment.Center));
            var cell = CreateCell(i.CurrentStock.ToString(), false, TextAlignment.Center); if (i.CurrentStock < 10) cell.Foreground = Brushes.Red; r.Cells.Add(cell);
            r.Cells.Add(CreateCell(i.TotalAmount.ToString("C2"), false, TextAlignment.Right));
        }, $"库存总货值: {data.Sum(x => x.TotalAmount):C2}");

        private void PrintReport<T>(string title, string[] headers, IEnumerable<T> data, Action<TableRow, T> fillRow, string footerText)
        {
            var doc = CreateDoc(title);
            var table = CreateTable(headers);
            foreach (var item in data) { var row = new TableRow(); fillRow(row, item); table.RowGroups[1].Rows.Add(row); }
            doc.Blocks.Add(table);
            if (!string.IsNullOrEmpty(footerText)) AddFooter(doc, footerText);
            PrintDocument(doc, title);
        }

        private FlowDocument CreateDoc(string title)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Microsoft YaHei"), FontSize = 12, PagePadding = new Thickness(40), ColumnWidth = double.PositiveInfinity };
            doc.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 24, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });
            return doc;
        }

        private Table CreateTable(string[] headers)
        {
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            for (int i = 0; i < headers.Length; i++) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup(); var row = new TableRow { Background = Brushes.LightGray };
            foreach (var h in headers) row.Cells.Add(CreateCell(h, true, TextAlignment.Center));
            group.Rows.Add(row); table.RowGroups.Add(group); table.RowGroups.Add(new TableRowGroup());
            return table;
        }

        private TableCell CreateCell(string? text, bool isHeader = false, TextAlignment align = TextAlignment.Left)
        {
            return new TableCell(new Paragraph(new Run(text ?? "")) { Margin = new Thickness(5), TextAlignment = align }) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5), FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal };
        }

        private void AddHeader(FlowDocument doc, params string[] lines)
        {
            var p = new Paragraph { FontSize = 14, LineHeight = 24 };
            foreach (var l in lines) p.Inlines.Add(new Run(l + "\n"));
            doc.Blocks.Add(p); doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Separator()));
        }

        private void AddFooter(FlowDocument doc, string rightText, string leftText = "")
        {
            var t = new Table { CellSpacing = 0 }; t.Columns.Add(new TableColumn()); t.Columns.Add(new TableColumn());
            var r = new TableRow();
            if (!string.IsNullOrEmpty(leftText)) r.Cells.Add(new TableCell(new Paragraph(new Run(leftText)) { Foreground = Brushes.Gray }));
            if (!string.IsNullOrEmpty(rightText)) r.Cells.Add(new TableCell(new Paragraph(new Run(rightText)) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, FontSize = 14 }));
            var g = new TableRowGroup(); g.Rows.Add(r); t.RowGroups.Add(g); doc.Blocks.Add(t);
        }

        private void AddSectionTitle(FlowDocument doc, string title)
        {
            doc.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 15, 0, 5), Foreground = Brushes.DimGray });
        }
    }
}
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
        // 核心打印方法：生成 XPS 并调用预览窗口
        private void PrintDocument(FlowDocument doc, string documentName)
        {
            doc.PageWidth = 794; // A4 宽度
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

        // --- 1. 批发销售单打印 (单据样式) ---
        public void PrintWholesaleOrder(WholesaleOrder order, IEnumerable<WholesaleItem> items)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;

            // 标题
            Paragraph title = new Paragraph(new Run("批发销售单"));
            title.FontSize = 24; title.FontWeight = FontWeights.Bold; title.TextAlignment = TextAlignment.Center;
            doc.Blocks.Add(title);

            // 头部
            Paragraph header = new Paragraph();
            header.FontSize = 14; header.LineHeight = 24;
            header.Inlines.Add(new Run($"单号：{order.OrderNo}   "));
            header.Inlines.Add(new Run($"日期：{order.OrderDate:yyyy-MM-dd HH:mm:ss}\n"));
            header.Inlines.Add(new Run($"客户：{order.Customer}\n"));
            header.Inlines.Add(new Run($"地址：{order.Address}"));
            doc.Blocks.Add(header);

            doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Separator()));

            // 表格
            Table table = new Table();
            table.CellSpacing = 0; table.BorderBrush = Brushes.Black; table.BorderThickness = new Thickness(1);

            table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) }); // 产品
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 数量
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 单价
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 小计

            TableRowGroup group = new TableRowGroup();

            // 表头
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            headerRow.Cells.Add(CreateCell("产品名称", true));
            headerRow.Cells.Add(CreateCell("数量", true, TextAlignment.Center));
            headerRow.Cells.Add(CreateCell("单价", true, TextAlignment.Right));
            headerRow.Cells.Add(CreateCell("小计", true, TextAlignment.Right));
            group.Rows.Add(headerRow);

            // 数据
            foreach (var item in items)
            {
                TableRow row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.SubTotal.ToString("C2"), false, TextAlignment.Right));
                group.Rows.Add(row);
            }
            table.RowGroups.Add(group);
            doc.Blocks.Add(table);

            // 底部合计
            Paragraph footer = new Paragraph();
            footer.Inlines.Add(new Run($"\n整单合计：{order.TotalAmount:C2}"));
            footer.FontSize = 16; footer.FontWeight = FontWeights.Bold; footer.TextAlignment = TextAlignment.Right;
            doc.Blocks.Add(footer);

            if (!string.IsNullOrEmpty(order.Remark))
                doc.Blocks.Add(new Paragraph(new Run($"备注：{order.Remark}")) { Foreground = Brushes.Gray });

            PrintDocument(doc, $"Wholesale_{order.OrderNo}");
        }

        // --- 2. 入库报表 (增加验收数据和汇总) ---
        public void PrintInboundReport(IEnumerable<InboundModel> data)
        {
            var headers = new string[] { "单号", "产品名称", "供应商", "进价", "进货数", "验收数", "拒收数", "状态", "日期" };
            var doc = CreateReportDocument("入库单汇总报表", headers);
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];

            decimal totalAmount = 0;

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Supplier ?? ""));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                // 🟢 验收详情
                row.Cells.Add(CreateCell(item.AcceptedQuantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.RejectedQuantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Status ?? ""));
                row.Cells.Add(CreateCell(item.InboundDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);

                // 计算有效金额 (按验收数量或进货数量算成本)
                var validQty = (item.Status == "已验收" || item.Status == "已退货") ? item.AcceptedQuantity : item.Quantity;
                totalAmount += item.Price * validQty;
            }

            // 🟢 添加底部合计
            AddFooterRow(table, headers.Length, $"总金额估算: {totalAmount:C2}");
            PrintDocument(doc, "InboundReport");
        }

        // --- 3. 出库报表 (增加汇总) ---
        public void PrintOutboundReport(IEnumerable<OutboundModel> data)
        {
            var headers = new string[] { "单号", "产品名称", "客户", "售价", "数量", "小计", "日期" };
            var doc = CreateReportDocument("出库单汇总报表", headers);
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];

            decimal totalAmount = 0;

            foreach (var item in data)
            {
                var subTotal = item.Price * item.Quantity;
                totalAmount += subTotal;

                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Customer ?? ""));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(subTotal.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.OutboundDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);
            }

            // 🟢 添加底部合计
            AddFooterRow(table, headers.Length, $"销售总金额: {totalAmount:C2}");
            PrintDocument(doc, "OutboundReport");
        }

        // --- 4. 退货报表 (增加汇总) ---
        public void PrintReturnReport(IEnumerable<ReturnModel> data)
        {
            var headers = new string[] { "单号", "产品名称", "客户", "单价", "数量", "退款额", "原因", "日期" };
            var doc = CreateReportDocument("退货单汇总报表", headers);
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];

            decimal totalAmount = 0;

            foreach (var item in data)
            {
                var refund = item.Price * item.Quantity;
                totalAmount += refund;

                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ReturnNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Customer ?? ""));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(refund.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Reason ?? ""));
                row.Cells.Add(CreateCell(item.ReturnDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);
            }

            // 🟢 添加底部合计
            AddFooterRow(table, headers.Length, $"退款总金额: {totalAmount:C2}");
            PrintDocument(doc, "ReturnReport");
        }

        // --- 5. 财务/库存/周期报表 ---
        public void PrintFinancialReport(IEnumerable<FinancialSummaryModel> data)
        {
            var headers = new string[] { "产品名称", "采购总成本", "销售总收入", "退款总额", "毛利/结余" };
            var doc = CreateReportDocument("财务收支统计报表", headers);
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];

            decimal tCost = 0, tRev = 0, tRef = 0, tProf = 0;

            foreach (var item in data)
            {
                tCost += item.TotalCost; tRev += item.TotalRevenue; tRef += item.TotalRefund; tProf += item.GrossProfit;
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.TotalCost.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.TotalRevenue.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.TotalRefund.ToString("C2"), false, TextAlignment.Right));
                var pCell = CreateCell(item.GrossProfit.ToString("C2"), false, TextAlignment.Right);
                pCell.Foreground = item.GrossProfit < 0 ? Brushes.Red : Brushes.Green;
                row.Cells.Add(pCell);
                rowGroup.Rows.Add(row);
            }

            // 🟢 底部汇总
            var footerGroup = new TableRowGroup();
            var footerRow = new TableRow();
            footerRow.Background = Brushes.WhiteSmoke;
            footerRow.Cells.Add(CreateCell("合计:", true, TextAlignment.Right));
            footerRow.Cells.Add(CreateCell(tCost.ToString("C2"), true, TextAlignment.Right));
            footerRow.Cells.Add(CreateCell(tRev.ToString("C2"), true, TextAlignment.Right));
            footerRow.Cells.Add(CreateCell(tRef.ToString("C2"), true, TextAlignment.Right));
            var tProfCell = CreateCell(tProf.ToString("C2"), true, TextAlignment.Right);
            tProfCell.Foreground = tProf < 0 ? Brushes.Red : Brushes.Green;
            footerRow.Cells.Add(tProfCell);
            footerGroup.Rows.Add(footerRow);
            table.RowGroups.Add(footerGroup);

            PrintDocument(doc, "FinancialReport");
        }

        public void PrintPeriodReport(IEnumerable<FinancialReportModel> data, string reportTitle)
        {
            var headers = new string[] { "时间段", "总收入", "总成本", "总退款", "净利润" };
            var doc = CreateReportDocument(reportTitle, headers);
            FillSimpleData(doc, data, (item) => new List<string> {
                item.PeriodName ?? "", item.Revenue.ToString("C2"), item.Cost.ToString("C2"),
                item.Refund.ToString("C2"), item.Profit.ToString("C2")
            });
            PrintDocument(doc, "PeriodReport");
        }

        public void PrintInventoryReport(IEnumerable<InventorySummaryModel> data)
        {
            var headers = new string[] { "产品名称", "入库总量", "出库总量", "当前库存", "库存货值" };
            var doc = CreateReportDocument("当前库存汇总报表", headers);
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];

            decimal totalVal = 0;
            foreach (var item in data)
            {
                totalVal += item.TotalAmount;
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.TotalInbound.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.TotalOutbound.ToString(), false, TextAlignment.Center));
                var stockCell = CreateCell(item.CurrentStock.ToString(), false, TextAlignment.Center);
                if (item.CurrentStock < 10) stockCell.Foreground = Brushes.Red;
                row.Cells.Add(stockCell);
                row.Cells.Add(CreateCell(item.TotalAmount.ToString("C2"), false, TextAlignment.Right));
                rowGroup.Rows.Add(row);
            }
            AddFooterRow(table, headers.Length, $"库存总货值: {totalVal:C2}");
            PrintDocument(doc, "InventoryReport");
        }

        // --- 辅助方法 ---

        private FlowDocument CreateReportDocument(string title, string[] headers)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12; doc.TextAlignment = TextAlignment.Left;

            Paragraph titlePara = new Paragraph(new Run(title));
            titlePara.FontSize = 24; titlePara.FontWeight = FontWeights.Bold; titlePara.TextAlignment = TextAlignment.Center; titlePara.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(titlePara);

            Table table = new Table();
            table.CellSpacing = 0; table.BorderBrush = Brushes.Gray; table.BorderThickness = new Thickness(1);

            for (int i = 0; i < headers.Length; i++) table.Columns.Add(new TableColumn());

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            foreach (var h in headers) headerRow.Cells.Add(CreateCell(h, true, TextAlignment.Center));
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);
            table.RowGroups.Add(new TableRowGroup());
            doc.Blocks.Add(table);
            return doc;
        }

        private void FillSimpleData<T>(FlowDocument doc, IEnumerable<T> data, Func<T, List<string>> formatFunc)
        {
            var table = doc.Blocks.OfType<Table>().First();
            var rowGroup = table.RowGroups[1];
            foreach (var item in data)
            {
                var row = new TableRow();
                var values = formatFunc(item);
                foreach (var v in values) row.Cells.Add(CreateCell(v, false, TextAlignment.Center));
                rowGroup.Rows.Add(row);
            }
        }

        private void AddFooterRow(Table table, int colSpan, string text)
        {
            var footerGroup = new TableRowGroup();
            var row = new TableRow();
            var cell = CreateCell(text, true, TextAlignment.Right);
            cell.ColumnSpan = colSpan;
            cell.Padding = new Thickness(10);
            cell.Background = Brushes.WhiteSmoke;
            cell.FontWeight = FontWeights.Bold;
            row.Cells.Add(cell);
            footerGroup.Rows.Add(row);
            table.RowGroups.Add(footerGroup);
        }

        private TableCell CreateCell(string text, bool isHeader = false, TextAlignment alignment = TextAlignment.Left)
        {
            Paragraph p = new Paragraph(new Run(text));
            p.Margin = new Thickness(5);
            p.TextAlignment = alignment;
            TableCell cell = new TableCell(p);
            cell.BorderBrush = Brushes.Gray;
            cell.BorderThickness = new Thickness(0.5);
            if (isHeader) cell.FontWeight = FontWeights.Bold;
            return cell;
        }
    }
}
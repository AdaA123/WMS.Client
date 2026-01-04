using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows; // 必须引用 System.Windows 用于设置 Owner
using System.Windows.Controls;
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
        // 核心打印与预览逻辑
        private void PrintDocument(FlowDocument doc, string documentName)
        {
            // 1. 设置文档规格 (A4)
            doc.PageWidth = 794;
            doc.PageHeight = 1123;
            doc.PagePadding = new Thickness(50);
            doc.ColumnWidth = double.PositiveInfinity;

            // 2. 将 FlowDocument 转换为 FixedDocument (内存中转换)
            MemoryStream ms = new MemoryStream();
            Package package = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
            Uri packUri = new Uri("pack://temp.xps");

            if (PackageStore.GetPackage(packUri) != null)
            {
                PackageStore.RemovePackage(packUri);
            }
            PackageStore.AddPackage(packUri, package);

            XpsDocument xpsDoc = new XpsDocument(package, CompressionOption.NotCompressed, packUri.ToString());
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
            writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator);

            FixedDocumentSequence fixedDoc = xpsDoc.GetFixedDocumentSequence();

            // 3. 打开预览窗口
            var previewWindow = new PrintPreviewWindow(fixedDoc);

            // 🔴 关键修复：设置 Owner (父窗口)
            // 这样预览窗口就会永远浮在主窗口上面，不会因为点了取消而被藏到后面去
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 让它在主窗口正中间弹出
            }

            previewWindow.ShowDialog();

            // 4. 清理资源
            xpsDoc.Close();
            package.Close();
            ms.Close();
            PackageStore.RemovePackage(packUri);
        }

        // ==========================================
        // 下面的报表生成逻辑完全保持不变
        // ==========================================

        // 1. 打印入库单
        public void PrintInboundReport(IEnumerable<InboundModel> data)
        {
            var doc = CreateFlowDocument("入库单汇总报表", new string[] { "单号", "产品名称", "供应商", "数量", "单价", "日期" });

            var table = doc.Blocks.OfType<Table>().FirstOrDefault();
            if (table == null) return;
            var rowGroup = table.RowGroups[1];

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OrderNo));
                row.Cells.Add(CreateCell(item.ProductName));
                row.Cells.Add(CreateCell(item.Supplier));
                row.Cells.Add(CreateCell(item.Quantity.ToString()));
                row.Cells.Add(CreateCell(item.Price.ToString("C2")));
                row.Cells.Add(CreateCell(item.InboundDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);
            }
            PrintDocument(doc, "InboundReport");
        }

        // 2. 打印出库单
        public void PrintOutboundReport(IEnumerable<OutboundModel> data)
        {
            var doc = CreateFlowDocument("出库单汇总报表", new string[] { "单号", "产品名称", "客户", "数量", "售价", "日期" });

            var table = doc.Blocks.OfType<Table>().FirstOrDefault();
            if (table == null) return;
            var rowGroup = table.RowGroups[1];

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OrderNo));
                row.Cells.Add(CreateCell(item.ProductName));
                row.Cells.Add(CreateCell(item.Customer));
                row.Cells.Add(CreateCell(item.Quantity.ToString()));
                row.Cells.Add(CreateCell(item.Price.ToString("C2")));
                row.Cells.Add(CreateCell(item.OutboundDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);
            }
            PrintDocument(doc, "OutboundReport");
        }

        // 3. 打印库存汇总
        public void PrintInventoryReport(IEnumerable<InventorySummaryModel> data)
        {
            var doc = CreateFlowDocument("当前库存汇总报表", new string[] { "产品名称", "入库总量", "出库总量", "当前库存" });

            var table = doc.Blocks.OfType<Table>().FirstOrDefault();
            if (table == null) return;
            var rowGroup = table.RowGroups[1];

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName));
                row.Cells.Add(CreateCell(item.TotalInbound.ToString()));
                row.Cells.Add(CreateCell(item.TotalOutbound.ToString()));

                var stockCell = CreateCell(item.CurrentStock.ToString());
                if (item.CurrentStock < 10) stockCell.Foreground = Brushes.Red;
                row.Cells.Add(stockCell);

                rowGroup.Rows.Add(row);
            }
            PrintDocument(doc, "InventoryReport");
        }

        // --- 辅助方法 ---
        private FlowDocument CreateFlowDocument(string title, string[] headers)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;
            doc.TextAlignment = TextAlignment.Left;

            // 标题
            Paragraph titlePara = new Paragraph(new Run(title));
            titlePara.FontSize = 24;
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(titlePara);

            // 表格
            Table table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Gray;
            table.BorderThickness = new Thickness(1);

            for (int i = 0; i < headers.Length; i++)
                table.Columns.Add(new TableColumn());

            // 表头
            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            foreach (var header in headers)
            {
                headerRow.Cells.Add(CreateCell(header, true));
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // 内容占位
            table.RowGroups.Add(new TableRowGroup());

            doc.Blocks.Add(table);
            return doc;
        }

        private TableCell CreateCell(string text, bool isHeader = false)
        {
            Paragraph p = new Paragraph(new Run(text));
            p.Margin = new Thickness(5);
            p.TextAlignment = isHeader ? TextAlignment.Center : TextAlignment.Left;

            TableCell cell = new TableCell(p);
            cell.BorderBrush = Brushes.Gray;
            cell.BorderThickness = new Thickness(0.5);
            if (isHeader) cell.FontWeight = FontWeights.Bold;
            return cell;
        }
    }
}
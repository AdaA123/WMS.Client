using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WMS.Client.Models;

namespace WMS.Client.Services
{
    public class PrintService
    {
        // 通用打印方法：接收一个文档并打印
        private void PrintDocument(FlowDocument doc, string documentName)
        {
            PrintDialog printDialog = new PrintDialog();

            // 弹出打印对话框，让用户选择打印机
            if (printDialog.ShowDialog() == true)
            {
                // 设置文档宽度适应打印纸张
                doc.PageHeight = printDialog.PrintableAreaHeight;
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.PagePadding = new Thickness(50);
                doc.ColumnGap = 0;
                doc.ColumnWidth = printDialog.PrintableAreaWidth;

                // 开始打印
                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, documentName);
            }
        }

        // 1. 打印入库单报表
        public void PrintInboundReport(IEnumerable<InboundModel> data)
        {
            var doc = CreateFlowDocument("入库单汇总报表", new string[] { "单号", "产品名称", "供应商", "数量", "单价", "日期" });
            var table = doc.Blocks.FirstBlock as Table;
            var rowGroup = table.RowGroups[1]; // 内容行组

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.OrderNo));
                row.Cells.Add(CreateCell(item.ProductName));
                row.Cells.Add(CreateCell(item.Supplier));
                row.Cells.Add(CreateCell(item.Quantity.ToString()));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"))); // 金额格式
                row.Cells.Add(CreateCell(item.InboundDate.ToString("yyyy-MM-dd")));
                rowGroup.Rows.Add(row);
            }

            PrintDocument(doc, "InboundReport");
        }

        // 2. 打印出库单报表
        public void PrintOutboundReport(IEnumerable<OutboundModel> data)
        {
            var doc = CreateFlowDocument("出库单汇总报表", new string[] { "单号", "产品名称", "客户", "数量", "售价", "日期" });
            var table = doc.Blocks.FirstBlock as Table;
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

        // 3. 打印库存汇总报表
        public void PrintInventoryReport(IEnumerable<InventorySummaryModel> data)
        {
            var doc = CreateFlowDocument("当前库存汇总报表", new string[] { "产品名称", "入库总量", "出库总量", "当前库存" });
            var table = doc.Blocks.FirstBlock as Table;
            var rowGroup = table.RowGroups[1];

            foreach (var item in data)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName));
                row.Cells.Add(CreateCell(item.TotalInbound.ToString()));
                row.Cells.Add(CreateCell(item.TotalOutbound.ToString()));

                // 库存不足高亮显示
                var stockCell = CreateCell(item.CurrentStock.ToString());
                if (item.CurrentStock < 10) stockCell.Foreground = Brushes.Red;
                row.Cells.Add(stockCell);

                rowGroup.Rows.Add(row);
            }

            PrintDocument(doc, "InventoryReport");
        }

        // --- 辅助方法：生成表格结构 ---
        private FlowDocument CreateFlowDocument(string title, string[] headers)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;

            // 1. 标题
            Paragraph titlePara = new Paragraph(new Run(title));
            titlePara.FontSize = 24;
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(titlePara);

            // 2. 表格
            Table table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Gray;
            table.BorderThickness = new Thickness(1);

            // 定义列
            for (int i = 0; i < headers.Length; i++)
                table.Columns.Add(new TableColumn());

            // 表头行
            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            foreach (var header in headers)
            {
                headerRow.Cells.Add(CreateCell(header, true));
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // 内容行容器
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
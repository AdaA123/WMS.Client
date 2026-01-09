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
        // 核心打印方法：生成 XPS 并调用预览窗口 (自带打印按钮)
        private void PrintDocument(FlowDocument doc, string documentName)
        {
            doc.PageWidth = 794; // A4 宽度 (像素)
            doc.PageHeight = 1123;
            doc.PagePadding = new Thickness(50);
            doc.ColumnWidth = double.PositiveInfinity; // 防止分栏

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

            // 使用统一的预览窗口，包含打印按钮
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

        // 🟢 新增：打印批发销售单 (单据样式)
        public void PrintWholesaleOrder(WholesaleOrder order, IEnumerable<WholesaleItem> items)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;

            // 1. 标题
            Paragraph title = new Paragraph(new Run("批发销售单"));
            title.FontSize = 24; title.FontWeight = FontWeights.Bold; title.TextAlignment = TextAlignment.Center;
            doc.Blocks.Add(title);

            // 2. 头部信息 (单号、客户、地址、时间)
            Paragraph header = new Paragraph();
            header.FontSize = 14;
            header.LineHeight = 24;
            header.Inlines.Add(new Run($"单号：{order.OrderNo}   "));
            header.Inlines.Add(new Run($"日期：{order.OrderDate:yyyy-MM-dd HH:mm:ss}\n")); // 精确到秒
            header.Inlines.Add(new Run($"客户：{order.Customer}\n"));
            header.Inlines.Add(new Run($"地址：{order.Address}")); // 显示地址
            doc.Blocks.Add(header);

            doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Separator()));

            // 3. 明细表格
            Table table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black; table.BorderThickness = new Thickness(1);

            // 定义列宽
            table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) }); // 产品
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 数量
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 单价
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 小计

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            headerRow.Cells.Add(CreateCell("产品名称", true));
            headerRow.Cells.Add(CreateCell("数量", true, TextAlignment.Center));
            headerRow.Cells.Add(CreateCell("单价", true, TextAlignment.Right));
            headerRow.Cells.Add(CreateCell("小计", true, TextAlignment.Right));
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            TableRowGroup dataGroup = new TableRowGroup();
            foreach (var item in items)
            {
                TableRow row = new TableRow();
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.SubTotal.ToString("C2"), false, TextAlignment.Right));
                dataGroup.Rows.Add(row);
            }
            table.RowGroups.Add(dataGroup);
            doc.Blocks.Add(table);

            // 4. 合计
            Paragraph footer = new Paragraph();
            footer.Inlines.Add(new Run($"\n整单合计：{order.TotalAmount:C2}"));
            footer.FontSize = 16; footer.FontWeight = FontWeights.Bold; footer.TextAlignment = TextAlignment.Right;
            doc.Blocks.Add(footer);

            if (!string.IsNullOrEmpty(order.Remark))
            {
                doc.Blocks.Add(new Paragraph(new Run($"备注：{order.Remark}")) { Foreground = Brushes.Gray });
            }

            PrintDocument(doc, $"Wholesale_{order.OrderNo}");
        }

        // --- 以下为列表报表打印 ---

        public void PrintFinancialReport(IEnumerable<FinancialSummaryModel> data)
        {
            var doc = CreateFlowDocument("财务收支统计报表", new string[] { "产品名称", "采购总成本", "销售总收入", "退款总额", "毛利/结余" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.TotalCost.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.TotalRevenue.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.TotalRefund.ToString("C2"), false, TextAlignment.Right));
                var profitCell = CreateCell(item.GrossProfit.ToString("C2"), false, TextAlignment.Right);
                profitCell.Foreground = item.GrossProfit < 0 ? Brushes.Red : Brushes.Green;
                row.Cells.Add(profitCell);
            });
            PrintDocument(doc, "FinancialReport");
        }

        public void PrintPeriodReport(IEnumerable<FinancialReportModel> data, string reportTitle)
        {
            var doc = CreateFlowDocument(reportTitle, new string[] { "时间段", "总收入", "总成本", "总退款", "净利润" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.PeriodName ?? ""));
                row.Cells.Add(CreateCell(item.Revenue.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Cost.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.Refund.ToString("C2"), false, TextAlignment.Right));
                var profitCell = CreateCell(item.Profit.ToString("C2"), false, TextAlignment.Right);
                profitCell.Foreground = item.Profit < 0 ? Brushes.Red : Brushes.Green;
                row.Cells.Add(profitCell);
            });
            PrintDocument(doc, "PeriodReport");
        }

        public void PrintInboundReport(IEnumerable<InboundModel> data)
        {
            var doc = CreateFlowDocument("入库单汇总报表", new string[] { "单号", "产品名称", "供应商", "数量", "单价", "日期" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Supplier ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.InboundDate.ToString("yyyy-MM-dd")));
            });
            PrintDocument(doc, "InboundReport");
        }

        public void PrintOutboundReport(IEnumerable<OutboundModel> data)
        {
            var doc = CreateFlowDocument("出库单汇总报表", new string[] { "单号", "产品名称", "客户", "数量", "售价", "日期" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.OrderNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Customer ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Price.ToString("C2"), false, TextAlignment.Right));
                row.Cells.Add(CreateCell(item.OutboundDate.ToString("yyyy-MM-dd")));
            });
            PrintDocument(doc, "OutboundReport");
        }

        public void PrintInventoryReport(IEnumerable<InventorySummaryModel> data)
        {
            var doc = CreateFlowDocument("当前库存汇总报表", new string[] { "产品名称", "入库总量", "出库总量", "当前库存" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.TotalInbound.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.TotalOutbound.ToString(), false, TextAlignment.Center));
                var stockCell = CreateCell(item.CurrentStock.ToString(), false, TextAlignment.Center);
                if (item.CurrentStock < 10) stockCell.Foreground = Brushes.Red;
                row.Cells.Add(stockCell);
            });
            PrintDocument(doc, "InventoryReport");
        }

        // 🟢 新增：退货单打印
        public void PrintReturnReport(IEnumerable<ReturnModel> data)
        {
            var doc = CreateFlowDocument("退货单汇总报表", new string[] { "单号", "产品名称", "客户", "数量", "原因", "日期" });
            FillTableData(doc, data, (row, item) => {
                row.Cells.Add(CreateCell(item.ReturnNo ?? ""));
                row.Cells.Add(CreateCell(item.ProductName ?? ""));
                row.Cells.Add(CreateCell(item.Customer ?? ""));
                row.Cells.Add(CreateCell(item.Quantity.ToString(), false, TextAlignment.Center));
                row.Cells.Add(CreateCell(item.Reason ?? ""));
                row.Cells.Add(CreateCell(item.ReturnDate.ToString("yyyy-MM-dd")));
            });
            PrintDocument(doc, "ReturnReport");
        }

        // --- 辅助方法 ---

        private FlowDocument CreateFlowDocument(string title, string[] headers)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Microsoft YaHei");
            doc.FontSize = 12;
            doc.TextAlignment = TextAlignment.Left;

            Paragraph titlePara = new Paragraph(new Run(title));
            titlePara.FontSize = 24;
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.Margin = new Thickness(0, 0, 0, 20);
            doc.Blocks.Add(titlePara);

            Table table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Gray;
            table.BorderThickness = new Thickness(1);

            for (int i = 0; i < headers.Length; i++)
                table.Columns.Add(new TableColumn());

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            foreach (var header in headers)
            {
                headerRow.Cells.Add(CreateCell(header, true, TextAlignment.Center));
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);
            table.RowGroups.Add(new TableRowGroup()); // 数据行占位
            doc.Blocks.Add(table);
            return doc;
        }

        private void FillTableData<T>(FlowDocument doc, IEnumerable<T> data, Action<TableRow, T> fillRowAction)
        {
            var table = doc.Blocks.OfType<Table>().FirstOrDefault();
            if (table == null || table.RowGroups.Count < 2) return;
            var rowGroup = table.RowGroups[1]; // 第二个组是数据组

            foreach (var item in data)
            {
                var row = new TableRow();
                fillRowAction(row, item);
                rowGroup.Rows.Add(row);
            }
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
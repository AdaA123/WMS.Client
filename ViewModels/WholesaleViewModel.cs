using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // 用于打印
using System.Windows.Documents; // 用于生成打印文档
using System.Windows.Media; // 用于打印样式
using WMS.Client.Models;
using WMS.Client.Services;
using WMS.Client.Views;

namespace WMS.Client.ViewModels
{
    public partial class WholesaleViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        public ObservableCollection<WholesaleOrder> WholesaleList { get; } = new();

        // 弹窗需要的数据源
        public ObservableCollection<string> ProductList { get; } = new();
        public ObservableCollection<string> CustomerList { get; } = new();
        public ObservableCollection<WholesaleItem> OrderItems { get; } = new(); // 当前单据的明细

        [ObservableProperty] private WholesaleOrder _currentOrder = new();
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private string _dialogTitle = "新建批发单";
        [ObservableProperty] private decimal _totalOrderAmount;

        // 临时添加栏变量
        [ObservableProperty] private string _tempProductName = "";
        [ObservableProperty] private int _tempQuantity = 1;
        [ObservableProperty] private decimal _tempPrice = 0;

        public WholesaleViewModel()
        {
            _dbService = new DatabaseService();
            _ = LoadData();
        }

        [RelayCommand]
        private async Task LoadData()
        {
            var data = await _dbService.GetWholesaleOrdersAsync();

            // 修复空引用警告
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                data = data.Where(x => (x.OrderNo?.Contains(SearchText) ?? false) ||
                                       (x.Customer?.Contains(SearchText) ?? false)).ToList();
            }

            WholesaleList.Clear();
            foreach (var item in data) WholesaleList.Add(item);

            // 预加载下拉框数据
            var products = await _dbService.GetProductListAsync();
            ProductList.Clear(); foreach (var p in products) ProductList.Add(p);
            var customers = await _dbService.GetCustomerListAsync();
            CustomerList.Clear(); foreach (var c in customers) CustomerList.Add(c);
        }

        partial void OnSearchTextChanged(string value) => _ = LoadData();

        partial void OnTempProductNameChanged(string value) => _ = FillPrice(value);
        private async Task FillPrice(string name)
        {
            var lastOut = await _dbService.GetLastOutboundByProductAsync(name);
            if (lastOut != null) TempPrice = lastOut.Price;
        }

        // 打开新建弹窗
        [RelayCommand]
        private async Task OpenCreateDialog()
        {
            DialogTitle = "新建批发单";
            CurrentOrder = new WholesaleOrder
            {
                OrderNo = $"WS{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks % 10000:0000}",
                OrderDate = DateTime.Now
            };
            OrderItems.Clear();
            UpdateTotal();

            TempProductName = ""; TempQuantity = 1; TempPrice = 0;

            var view = new WholesaleDialog { DataContext = this };
            var result = await DialogHost.Show(view, "RootDialog");

            // 🟢 修复1：兼容字符串 "True" 和布尔值 true
            // DialogHost CloseDialogCommand 传回来的可能是字符串
            bool isConfirm = result != null && (result.Equals(true) || result.ToString()!.Equals("True", StringComparison.OrdinalIgnoreCase));

            if (isConfirm)
            {
                await SaveOrder();
            }
        }

        // 打开编辑弹窗
        [RelayCommand]
        private async Task Edit(WholesaleOrder item)
        {
            DialogTitle = "编辑批发单";
            CurrentOrder = new WholesaleOrder
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                Customer = item.Customer,
                OrderDate = item.OrderDate,
                Remark = item.Remark
            };

            OrderItems.Clear();
            foreach (var i in item.Items)
                OrderItems.Add(new WholesaleItem { Id = i.Id, OrderId = i.OrderId, ProductName = i.ProductName, Quantity = i.Quantity, Price = i.Price });

            UpdateTotal();

            var view = new WholesaleDialog { DataContext = this };
            var result = await DialogHost.Show(view, "RootDialog");

            // 🟢 修复1：同上
            bool isConfirm = result != null && (result.Equals(true) || result.ToString()!.Equals("True", StringComparison.OrdinalIgnoreCase));

            if (isConfirm)
            {
                await SaveOrder();
            }
        }

        [RelayCommand]
        private void AddItem()
        {
            if (string.IsNullOrEmpty(TempProductName)) return;
            if (TempQuantity <= 0) return;

            OrderItems.Add(new WholesaleItem
            {
                ProductName = TempProductName,
                Quantity = TempQuantity,
                Price = TempPrice
            });

            UpdateTotal();
            TempProductName = ""; TempQuantity = 1;
        }

        [RelayCommand]
        private void RemoveItem(WholesaleItem item)
        {
            OrderItems.Remove(item);
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            TotalOrderAmount = OrderItems.Sum(x => x.SubTotal);
        }

        private async Task SaveOrder()
        {
            if (OrderItems.Count == 0) { MessageBox.Show("请至少添加一种商品"); return; }
            if (string.IsNullOrEmpty(CurrentOrder.Customer)) { MessageBox.Show("请选择客户"); return; }

            CurrentOrder.Items = OrderItems.ToList();
            CurrentOrder.TotalAmount = TotalOrderAmount;

            await _dbService.SaveWholesaleOrderAsync(CurrentOrder);

            if (!CustomerList.Contains(CurrentOrder.Customer))
                await _dbService.SaveCustomerAsync(new CustomerModel { Name = CurrentOrder.Customer });

            await LoadData(); // 🟢 这里会刷新列表
        }

        [RelayCommand]
        private async Task Delete(WholesaleOrder item)
        {
            if (MessageBox.Show("确定删除该单据吗？库存将回滚。", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _dbService.DeleteWholesaleOrderAsync(item);
                await LoadData();
            }
        }

        // 🟢 修复2：实现真实的单据打印
        [RelayCommand]
        private void PrintOrder()
        {
            if (OrderItems.Count == 0)
            {
                MessageBox.Show("没有商品明细，无法打印。", "提示");
                return;
            }

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // 创建流文档
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.FontFamily = new FontFamily("Microsoft YaHei");
                doc.ColumnWidth = 999999; // 防止分栏

                // 1. 标题
                Paragraph title = new Paragraph(new Run("批发销售单"));
                title.FontSize = 24;
                title.FontWeight = FontWeights.Bold;
                title.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(title);

                // 2. 头部信息
                Paragraph header = new Paragraph();
                header.Inlines.Add(new Run($"单号：{CurrentOrder.OrderNo}\n"));
                header.Inlines.Add(new Run($"客户：{CurrentOrder.Customer}\n"));
                header.Inlines.Add(new Run($"日期：{CurrentOrder.OrderDate:yyyy-MM-dd HH:mm}"));
                header.FontSize = 14;
                doc.Blocks.Add(header);

                // 3. 表格
                Table table = new Table();
                table.CellSpacing = 0;
                table.BorderBrush = Brushes.Black;
                table.BorderThickness = new Thickness(1);

                // 定义列
                table.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) }); // 产品
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 数量
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 单价
                table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // 小计

                // 表头
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = Brushes.LightGray;
                headerRow.Cells.Add(CreateCell("产品名称", true));
                headerRow.Cells.Add(CreateCell("数量", true));
                headerRow.Cells.Add(CreateCell("单价", true));
                headerRow.Cells.Add(CreateCell("小计", true));
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                // 数据行
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (var item in OrderItems)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(CreateCell(item.ProductName ?? ""));
                    row.Cells.Add(CreateCell(item.Quantity.ToString()));
                    row.Cells.Add(CreateCell(item.Price.ToString("C2")));
                    row.Cells.Add(CreateCell(item.SubTotal.ToString("C2")));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);
                doc.Blocks.Add(table);

                // 4. 合计
                Paragraph footer = new Paragraph();
                footer.Inlines.Add(new Run($"\n整单合计：{TotalOrderAmount:C2}"));
                footer.FontSize = 16;
                footer.FontWeight = FontWeights.Bold;
                footer.TextAlignment = TextAlignment.Right;
                doc.Blocks.Add(footer);

                // 5. 备注
                if (!string.IsNullOrEmpty(CurrentOrder.Remark))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"备注：{CurrentOrder.Remark}")) { Foreground = Brushes.Gray });
                }

                // 执行打印
                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "批发单据打印");
            }
        }

        // 辅助方法：创建表格单元格
        private TableCell CreateCell(string text, bool isHeader = false)
        {
            return new TableCell(new Paragraph(new Run(text))
            {
                Padding = new Thickness(5),
                TextAlignment = isHeader ? TextAlignment.Center : TextAlignment.Left
            })
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 1) // 只有下边框
            };
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WMS.Client.Views
{
    public partial class PrintPreviewWindow : Window
    {
        // 保存文档引用，以便后续手动触发打印
        private readonly IDocumentPaginatorSource _document;

        public PrintPreviewWindow(IDocumentPaginatorSource doc)
        {
            InitializeComponent();
            _document = doc;

            // 显示文档
            docViewer.Document = doc;
        }

        // 🔴 确认打印按钮逻辑
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();

            // 1. 弹出系统打印设置窗口 (选择打印机、份数等)
            if (printDialog.ShowDialog() == true)
            {
                // 2. 执行打印
                // 使用之前传入的文档进行打印
                printDialog.PrintDocument(_document.DocumentPaginator, "WMS报表打印任务");

                // 3. 🔴 关键点：打印提交后，自动关闭当前预览窗口
                this.Close();
            }
        }

        // 关闭按钮逻辑
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
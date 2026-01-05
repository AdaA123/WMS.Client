using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WMS.Client.Views
{
    public partial class FinancialView : UserControl
    {
        public FinancialView()
        {
            InitializeComponent();
        }

        // 🟢 修复 1：处理内部子表格(详细信息)的滚轮
        private void InnerDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 这里的逻辑是一样的：把事件往上传递
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        // 🟢 修复 2：处理主表格的滚轮 (解决鼠标悬停在数据上无法滚动页面的问题)
        private void MainDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                // 1. 拦截事件，防止 DataGrid 自己消化
                e.Handled = true;

                // 2. 创建一个新的滚轮事件
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;

                // 3. 手动向父级引发这个事件，让外层的 ScrollViewer 接收到
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}
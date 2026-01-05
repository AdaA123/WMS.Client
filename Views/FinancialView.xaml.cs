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

        // 🟢 核心修复：处理子表格的滚轮事件
        private void InnerDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                // 1. 标记事件已处理，防止子表格内部消化
                e.Handled = true;

                // 2. 构造一个新的滚轮事件
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;

                // 3. 获取父级元素 (StackPanel) 并向上冒泡事件
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}
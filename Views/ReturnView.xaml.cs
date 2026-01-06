using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WMS.Client.Views
{
    public partial class ReturnView : UserControl
    {
        public ReturnView()
        {
            InitializeComponent();
        }

        // 🟢 必须添加此方法
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
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
    }
}
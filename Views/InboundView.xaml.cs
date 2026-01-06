using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WMS.Client.Views
{
    public partial class InboundView : UserControl
    {
        public InboundView()
        {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 🟢 修复 CS0103: 直接调用当前类中定义的 FindVisualParent 方法
            var scrollViewer = FindVisualParent<ScrollViewer>(sender as DependencyObject);
            scrollViewer?.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // 🟢 修复 CS0103: 在这里直接定义辅助方法，无需引用外部 Helpers
        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void ComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            var cmb = sender as ComboBox;
            if (cmb?.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(cmb.ItemsSource);
            if (view == null) return;

            var text = cmb.Text;

            if (string.IsNullOrEmpty(text))
            {
                view.Filter = null;
            }
            else
            {
                // 🟢 修复 CS8602: 增加空值检查 (item != null)
                // 解释: item?.ToString() 可能返回 null，直接调用 .IndexOf 会报警告
                view.Filter = item =>
                {
                    if (item == null) return false;
                    string? s = item.ToString();
                    return s != null && s.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }

            cmb.IsDropDownOpen = true;
        }
    }
}
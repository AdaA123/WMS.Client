using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS.Client.ViewModels; // 引用

namespace WMS.Client.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            this.DataContext = new HomeViewModel(); // 绑定！
        }

        // 🟢 核心修复：必须添加这个方法，XAML 中的 PreviewMouseWheel 才能找到它
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                // 创建一个新的滚轮事件，手动转发给父容器（外层的 ScrollViewer）
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}
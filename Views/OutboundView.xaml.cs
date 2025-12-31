using System.Windows.Controls;
using WMS.Client.ViewModels; // 引用命名空间

namespace WMS.Client.Views
{
    public partial class OutboundView : UserControl
    {
        public OutboundView()
        {
            InitializeComponent();
            this.DataContext = new OutboundViewModel(); // 绑定 ViewModel
        }
    }
}
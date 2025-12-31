using System.Collections.Generic; // 引用 List
using System.Windows.Controls;

namespace WMS.Client.Views
{
    public partial class OutboundDialog : UserControl
    {
        // 接收两个参数：产品列表、客户列表
        public OutboundDialog(List<string>? productList = null, List<string>? customerList = null)
        {
            InitializeComponent();

            if (productList != null) CmbProduct.ItemsSource = productList;

            // 绑定客户列表
            if (customerList != null) CmbCustomer.ItemsSource = customerList;
        }

        public OutboundDialog() : this(null, null) { }
    }
}
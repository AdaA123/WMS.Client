using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WMS.Client.Views
{
    /// <summary>
    /// InboundDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InboundDialog : UserControl
    {
        // 🟢 修改：构造函数增加 productList 参数
        public InboundDialog(List<string>? supplierList = null, List<string>? productList = null)
        {
            InitializeComponent();

            if (supplierList != null) CmbSupplier.ItemsSource = supplierList;
            if (productList != null) CmbProduct.ItemsSource = productList;
        }

        public InboundDialog() : this(null, null) { }
    }
}
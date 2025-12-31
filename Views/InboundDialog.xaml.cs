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
        // 允许接收供应商列表
        public InboundDialog(List<string>? supplierList = null)
        {
            InitializeComponent();

            if (supplierList != null)
            {
                CmbSupplier.ItemsSource = supplierList;
            }
        }

        public InboundDialog() : this(null) { }
    }
}

using System.Windows.Controls;
using WMS.Client.ViewModels;

namespace WMS.Client.Views
{
    public partial class InboundView : UserControl
    {
        public InboundView()
        {
            InitializeComponent();
            // 这里我们直接绑定 ViewModel
            this.DataContext = new InboundViewModel();
        }
    }
}
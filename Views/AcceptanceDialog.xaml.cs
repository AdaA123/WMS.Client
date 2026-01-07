using System.Windows.Controls;
using WMS.Client.Models;

namespace WMS.Client.Views
{
    public partial class AcceptanceDialog : UserControl
    {
        private int _totalQty;

        public AcceptanceDialog(InboundModel model)
        {
            InitializeComponent();
            DataContext = model;
            _totalQty = model.Quantity;

            // 默认合格数为总数
            TxtAccepted.Text = _totalQty.ToString();
        }

        public AcceptanceDialog() { InitializeComponent(); }

        private void TxtAccepted_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TxtAccepted.Text, out int accepted))
            {
                if (accepted > _totalQty)
                {
                    accepted = _totalQty;
                    TxtAccepted.Text = accepted.ToString();
                }
                else if (accepted < 0)
                {
                    accepted = 0;
                    TxtAccepted.Text = "0";
                }

                if (TxtRejected != null)
                {
                    TxtRejected.Text = (_totalQty - accepted).ToString();
                }
            }
            else
            {
                if (TxtRejected != null) TxtRejected.Text = "-";
            }
        }
    }
}
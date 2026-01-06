using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WMS.Client.Views
{
    public partial class ReturnView : UserControl
    {
        public ReturnView()
        {
            InitializeComponent();
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
                // 🟢 修复 CS8602: 安全的空值判断
                view.Filter = item =>
                {
                    if (item == null) return false;
                    string? s = item.ToString();
                    // 确保 s 不为 null 再调用 IndexOf
                    return s != null && s.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }

            cmb.IsDropDownOpen = true;
        }
    }
}
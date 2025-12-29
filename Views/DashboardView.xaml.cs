using System.Collections.Generic;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using WMS.Client.Models;

namespace WMS.Client.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            var menuItems = new List<MenuItemModel>
            {
                new MenuItemModel("系统首页", PackIconKind.Home, typeof(HomeView)),
                new MenuItemModel("入库管理", PackIconKind.Dolly, typeof(InboundView)), // 暂时都指回去，防止报错
                new MenuItemModel("出库管理", PackIconKind.Truck, typeof(HomeView))
            };

            MenuListBox.ItemsSource = menuItems;
            MenuListBox.DisplayMemberPath = "Title"; // 简单显示文字

            // 默认选中第一个
            MenuListBox.SelectedIndex = 0;
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuListBox.SelectedItem is MenuItemModel item)
            {
                // 简单的反射创建页面
                MainContent.Content = System.Activator.CreateInstance(item.TargetViewType);
            }
        }
    }
}
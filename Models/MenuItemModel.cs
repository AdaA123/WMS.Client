using MaterialDesignThemes.Wpf;
using System;

namespace WMS.Client.Models
{
    public class MenuItemModel
    {
        public string Title { get; set; }
        public PackIconKind Icon { get; set; }
        public Type TargetViewType { get; set; } // 我们简化一下，直接导航到 View 类型

        public MenuItemModel(string title, PackIconKind icon, Type viewType)
        {
            Title = title;
            Icon = icon;
            TargetViewType = viewType;
        }
    }
}
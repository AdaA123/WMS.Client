using CommunityToolkit.Mvvm.ComponentModel;

namespace WMS.Client.ViewModels
{
    // 主窗口的 ViewModel，负责控制当前显示哪个大页面
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;
    }
}
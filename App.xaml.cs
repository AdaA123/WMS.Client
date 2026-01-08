using System.Windows;

namespace WMS.Client
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 啟動主窗口 (主窗口內部會先顯示登入頁)
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
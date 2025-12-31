using System.Windows;

namespace WMS.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ 手动启动逻辑，包裹在 try-catch 中
            try
            {
                // 实例化登录窗口
                var loginView = new Views.LoginView();
                loginView.Show();
            }
            catch (System.Exception ex)
            {
                // 🔴 如果报错，这里会弹窗告诉你具体原因！
                MessageBox.Show($"程序启动失败：\n{ex.Message}\n\n详细信息：{ex.InnerException?.Message}", "严重错误");
                Shutdown();
            }
        }
    }
}
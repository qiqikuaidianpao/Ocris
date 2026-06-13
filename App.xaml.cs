using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Ocris.Services;
using Ocris.ViewModels;
using Ocris.Views;

namespace Ocris
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 初始化服务容器
                ServiceContainer.Initialize();
                var container = ServiceContainer.Instance;

                // 应用配置中的主题（持久化：重启保留上次主题）
                string startupTheme = Services.ThemeService.Light;
                try
                {
                    var cfg = container.Resolve<Services.IConfigService>();
                    if (cfg != null && cfg.Config != null && !string.IsNullOrEmpty(cfg.Config.Theme))
                        startupTheme = cfg.Config.Theme;
                }
                catch (Exception themeEx)
                {
                }
                Services.ThemeService.ApplyTheme(startupTheme);

                // 异步初始化服务
                Task.Run(async () =>
                {
                    try
                    {
                        await container.InitializeServicesAsync();
                    }
                    catch (Exception initEx)
                    {
                    }
                });

                // 创建并显示主窗口
                var mainWindow = new MainWindow();
                
                // 使用服务容器解析MainViewModel的依赖
                var aiService = container.Resolve<Services.IAIService>();
                var logService = container.Resolve<Services.ILogService>();
                var screenshotService = container.Resolve<Services.IScreenshotService>();
                var ocrService = container.Resolve<Services.IOCRService>();
                var hotkeyService = container.Resolve<Services.IHotkeyService>();
                var configService = container.Resolve<Services.IConfigService>();
                
                mainWindow.DataContext = new MainViewModel(aiService, logService, screenshotService, ocrService, hotkeyService, configService);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("应用程序启动失败: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    errorMessage += string.Format("\n\n内部异常: {0}", ex.InnerException.Message);
                }
                errorMessage += string.Format("\n\n堆栈跟踪:\n{0}", ex.StackTrace);
                
                MessageBox.Show(errorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 清理资源
                ServiceContainer.Cleanup();
            }
            catch (Exception ex)
            {
                // 记录错误但不显示给用户
            }

            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBox.Show("应用程序发生未处理的异常: " + e.Exception.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
            catch
            {
                // 如果连错误显示都失败了，那就让应用程序崩溃
            }
        }
    }
}
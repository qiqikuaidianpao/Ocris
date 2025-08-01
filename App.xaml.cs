using System;
using System.Windows;
using AIAnswerTool.Services;
using AIAnswerTool.ViewModels;
using AIAnswerTool.Views;

namespace AIAnswerTool
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 添加全局异常处理
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("应用程序发生未处理的异常:\n{0}\n\n堆栈跟踪:\n{1}", e.Exception.Message, e.Exception.StackTrace);
            MessageBox.Show(errorMessage, "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // 防止应用程序崩溃
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string message = ex != null ? ex.Message : "未知错误";
            string stackTrace = ex != null ? ex.StackTrace : "无堆栈信息";
            string errorMessage = string.Format("应用程序域发生未处理的异常:\n{0}\n\n堆栈跟踪:\n{1}", message, stackTrace);
            MessageBox.Show(errorMessage, "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 创建服务实例
                var logService = new LogService();
                var configService = new ConfigService(logService);
                
                // 加载配置文件
                configService.LoadConfigAsync().Wait();
                
                // 根据配置设置日志目录
                var logPath = configService.GetConfigValue<string>("LogPath", "Logs");
                if (!string.IsNullOrEmpty(logPath))
                {
                    logService.SetLogDirectory(logPath);
                }
                
                var windowDetectionService = new WindowDetectionService(logService);
                var screenshotService = new ScreenshotService(logService, configService);
                var ocrService = new OCRService(logService, configService);
                var aiService = new AliCloudAIService(logService, configService);
                var hotkeyService = new HotkeyService(logService);
                
                // 确保AI服务重新加载配置
                aiService.LoadConfiguration();

                // 创建 ViewModel
                var mainViewModel = new MainViewModel(aiService, logService, screenshotService, ocrService, hotkeyService);

                // 创建并显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.DataContext = mainViewModel;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format("应用程序启动失败:\n{0}\n\n堆栈跟踪:\n{1}", ex.Message, ex.StackTrace);
                MessageBox.Show(errorMessage, "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown(1); // 退出应用程序
            }
        }
    }
}
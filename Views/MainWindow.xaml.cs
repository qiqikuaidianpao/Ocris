using System;
using System.Windows;
using AIAnswerTool.ViewModels;
using AIAnswerTool.Services;

namespace AIAnswerTool.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化界面
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 设置窗口图标和标题
            Title = "AI答题辅助工具 v1.0";
            
            // 绑定事件
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 初始化ViewModel
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("初始化失败: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 运行OCR测试
        /// </summary>
        private async System.Threading.Tasks.Task RunOCRTest()
        {
            try
            {
                var logService = new LogService();
                var configService = new ConfigService(logService);
                var ocrService = new OCRService(logService, configService);
                
                logService.Info("=== 开始OCR功能自动测试 ===");
                
                var testResult = await ocrService.TestOCRFunctionAsync();
                
                if (testResult)
                {
                    logService.Info("✓ OCR功能测试通过");
                    MessageBox.Show("OCR功能测试成功！请查看日志文件获取详细信息。", "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    logService.Error("✗ OCR功能测试失败");
                    MessageBox.Show("OCR功能测试失败！请查看日志文件获取详细信息。", "测试结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
                logService.Info("=== OCR功能测试完成 ===");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("OCR测试异常: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 清理资源
                // _viewModel?.Dispose(); // MainViewModel a besoin d'implémenter IDisposable
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止关闭
                System.Diagnostics.Debug.WriteLine(string.Format("关闭时发生错误: {0}", ex.Message));
            }
        }
    }
}
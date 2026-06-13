using System;
using System.Windows;
using Ocris.ViewModels;

namespace Ocris.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            
            // 初始化ViewModel
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
            
            // 绑定事件
            _viewModel.CloseRequested += ViewModel_CloseRequested;
            
            // 初始化界面
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 设置窗口图标和标题
            Title = "设置 - AI答题辅助工具";
            
            // 绑定窗口事件
            Loaded += SettingsWindow_Loaded;
            Closing += SettingsWindow_Closing;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 加载设置
                _viewModel.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载设置失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理资源
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= ViewModel_CloseRequested;
            }
        }

        private void ViewModel_CloseRequested(object sender, EventArgs e)
        {
            // 关闭窗口
            this.Close();
        }
    }
}
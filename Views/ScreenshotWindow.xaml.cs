using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using Ocris.Services;
using Ocris.ViewModels;
using Ocris.Views;
using Ocris.Models;

namespace Ocris.Views
{
    /// <summary>
    /// 检测模式枚举
    /// </summary>
    public enum DetectionMode
    {
        Window,    // 窗口模式
        Element    // 控件模式
    }

    /// <summary>
    /// 截图选择窗口
    /// </summary>
    public partial class ScreenshotWindow : Window
    {
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        private System.Windows.Point _endPoint;
        private Bitmap _backgroundImage;
        private ScreenshotViewModel _viewModel;
        
        // 窗口检测相关字段
        private readonly IWindowDetectionService _windowDetectionService;
        private readonly ILogService _logService;
        private DetectionMode _currentDetectionMode = DetectionMode.Window;
        private System.Threading.Timer _detectionTimer;
        private System.Windows.Point _lastMousePosition;
        private IntPtr _lastHighlightedWindow = IntPtr.Zero;
        private bool _isHighlightingEnabled = true;
        


        /// <summary>
        /// 截图完成事件
        /// </summary>
        public event EventHandler<ScreenshotResult> ScreenshotCompleted;

        /// <summary>
        /// 截图取消事件
        /// </summary>
        public event EventHandler ScreenshotCancelled;

        public ScreenshotWindow(System.Drawing.Rectangle bounds) : this(bounds, null, null)
        {
        }
        
        public ScreenshotWindow(System.Drawing.Rectangle bounds, IWindowDetectionService windowDetectionService, ILogService logService = null)
        {
            _logService = logService;
            if (_logService != null) _logService.Debug("ScreenshotWindow 构造函数开始");
            InitializeComponent();
            _windowDetectionService = windowDetectionService;
            if (_logService != null) _logService.Debug("WindowDetectionService 是否为空: {0}", _windowDetectionService == null);
            if (_logService != null) _logService.Debug("_isHighlightingEnabled 初始值: {0}", _isHighlightingEnabled);
            
            InitializeWindow(bounds);
            if (_windowDetectionService != null)
            {
                if (_logService != null) _logService.Debug("开始初始化窗口检测功能");
                InitializeWindowDetection();
            }
            else
            {
                if (_logService != null) _logService.Warn("WindowDetectionService 为空，智能高亮功能不可用");
            }
            if (_logService != null) _logService.Debug("ScreenshotWindow 构造函数完成");
        }
        
        /// <summary>
        /// 带背景图像的构造函数
        /// </summary>
        public ScreenshotWindow(System.Drawing.Rectangle bounds, IWindowDetectionService windowDetectionService, ILogService logService, Bitmap backgroundImage)
        {
            _logService = logService;
            if (_logService != null) _logService.Debug("ScreenshotWindow 构造函数开始（使用自定义背景图像）");
            InitializeComponent();
            _windowDetectionService = windowDetectionService;
            _backgroundImage = backgroundImage; // 使用传入的背景图像
            
            if (_logService != null) _logService.Debug("WindowDetectionService 是否为空: {0}", _windowDetectionService == null);
            if (_logService != null) _logService.Debug("_isHighlightingEnabled 初始值: {0}", _isHighlightingEnabled);
            
            InitializeWindowWithCustomBackground(bounds);
            if (_windowDetectionService != null)
            {
                if (_logService != null) _logService.Debug("开始初始化窗口检测功能");
                InitializeWindowDetection();
            }
            else
            {
                if (_logService != null) _logService.Warn("WindowDetectionService 为空，智能高亮功能不可用");
            }
            if (_logService != null) _logService.Debug("ScreenshotWindow 构造函数完成");
        }

        /// <summary>
        /// 初始化窗口
        /// </summary>
        private void InitializeWindow(System.Drawing.Rectangle bounds)
        {
            try
            {
                // 设置窗口属性以确保覆盖所有屏幕
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                AllowsTransparency = true;
                
                // 获取虚拟屏幕边界和所有显示器信息
                var virtualScreen = SystemInformation.VirtualScreen;
                var allScreens = Screen.AllScreens;
                
                // 调试信息：输出显示器配置
                for (int i = 0; i < allScreens.Length; i++)
                {
                    var screen = allScreens[i];
                }
                
                // 设置窗口位置和大小，确保覆盖所有显示器
                Left = virtualScreen.Left;
                Top = virtualScreen.Top;
                Width = virtualScreen.Width;
                Height = virtualScreen.Height;
                
                
                // The window is shown by the service that creates it.
                
                // 捕获背景图像
                CaptureBackground();
                
                // 初始化视图模型
                _viewModel = new ScreenshotViewModel();
                DataContext = _viewModel;
                
                // 订阅模式变化事件
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                // 设置焦点以接收键盘事件
                Focusable = true;
                Focus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("初始化截图窗口失败: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        
        /// <summary>
        /// 使用自定义背景图像初始化窗口
        /// </summary>
        private void InitializeWindowWithCustomBackground(System.Drawing.Rectangle bounds)
        {
            try
            {
                // 设置窗口属性以确保覆盖所有屏幕
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                AllowsTransparency = true;
                
                // 获取虚拟屏幕边界和所有显示器信息
                var virtualScreen = SystemInformation.VirtualScreen;
                
                // 设置窗口位置和大小，确保覆盖所有显示器
                Left = virtualScreen.Left;
                Top = virtualScreen.Top;
                Width = virtualScreen.Width;
                Height = virtualScreen.Height;
                
                if (_logService != null) _logService.Debug("使用自定义背景图像，跳过背景捕获");
                
                // 初始化视图模型
                _viewModel = new ScreenshotViewModel();
                DataContext = _viewModel;
                
                // 订阅模式变化事件
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                // 设置焦点以接收键盘事件
                Focusable = true;
                Focus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("初始化截图窗口失败: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// 初始化窗口检测功能
        /// </summary>
        private void InitializeWindowDetection()
        {
            if (_windowDetectionService != null)
            {
                // 不使用定时器，改为使用鼠标移动事件检测
            }
        }

        /// <summary>
        /// 检测鼠标下的窗口
        /// </summary>
        private void DetectWindowUnderMouse()
        {
            if (!_isHighlightingEnabled || _isSelecting || _windowDetectionService == null)
            {
                if (_logService != null) _logService.Debug("检测跳过: _isHighlightingEnabled={0}, _isSelecting={1}, _windowDetectionService={2}", _isHighlightingEnabled, _isSelecting, _windowDetectionService != null);
                return;
            }

            try
            {
                if (_logService != null) _logService.Debug("=== 开始窗口检测 ===");
                
                // 直接使用当前鼠标位置，不再重复检查移动距离
                var mousePos = Mouse.GetPosition(this);
                if (_logService != null) _logService.Debug("鼠标窗口坐标: ({0}, {1})", mousePos.X, mousePos.Y);
                
                // 转换为屏幕坐标
                var screenPoint = PointToScreen(mousePos);
                var screenPos = new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y);
                
                if (_logService != null) _logService.Debug("屏幕坐标: ({0}, {1})", screenPos.X, screenPos.Y);
                if (_logService != null) _logService.Debug("当前检测模式: {0}", _currentDetectionMode);
                
                if (_currentDetectionMode == DetectionMode.Window)
                {
                    DetectWindow(screenPos);
                }
                else
                {
                    DetectElement(screenPos);
                }
                
                if (_logService != null) _logService.Debug("=== 窗口检测结束 ===");
            }
            catch (Exception ex)
            {
                if (_logService != null) _logService.Error(ex, "窗口检测错误");
            }
        }

        /// <summary>
        /// 检测窗口模式
        /// </summary>
        private void DetectWindow(System.Drawing.Point screenPos)
        {
            if (_logService != null) _logService.Debug("--- 开始窗口检测 ---");
            
            // 获取当前截图窗口的句柄，以便排除自身
            var currentWindowHandle = new WindowInteropHelper(this).Handle;
            if (_logService != null) _logService.Debug("当前截图窗口句柄: {0} (0x{1:X})", currentWindowHandle, currentWindowHandle.ToInt64());
            
            var windowHandle = _windowDetectionService.GetWindowUnderCursor(currentWindowHandle);
            if (_logService != null) _logService.Debug("检测到窗口句柄: {0} (0x{1:X})", windowHandle, windowHandle.ToInt64());
            
            // 移除窗口句柄变化的判断，让高亮始终跟随鼠标
            if (windowHandle != IntPtr.Zero && _windowDetectionService.IsValidWindow(windowHandle))
            {
                if (_logService != null) _logService.Debug("窗口有效，获取窗口信息...");
                
                // 优先使用客户区域边界，如果获取失败则使用整个窗口边界
                var clientBounds = _windowDetectionService.GetWindowClientBounds(windowHandle);
                var bounds = clientBounds.IsEmpty ? _windowDetectionService.GetWindowBounds(windowHandle) : clientBounds;
                var windowInfo = _windowDetectionService.GetWindowInfo(windowHandle);
                
                if (_logService != null) _logService.Debug("窗口标题: {0}", windowInfo.Title);
                if (_logService != null) _logService.Debug("窗口类名: {0}", windowInfo.ClassName);
                if (_logService != null) _logService.Debug("窗口进程: {0}", windowInfo.ProcessName);
                if (_logService != null) _logService.Debug("窗口边界: X={0}, Y={1}, W={2}, H={3} (使用{4})", bounds.X, bounds.Y, bounds.Width, bounds.Height, clientBounds.IsEmpty ? "整个窗口" : "客户区域");
                
                var infoText = string.Format("窗口: {0}\n类名: {1}\n进程: {2}", windowInfo.Title, windowInfo.ClassName, windowInfo.ProcessName);
                if (_logService != null) _logService.Debug("调用UpdateHighlight...");
                UpdateHighlight(bounds, infoText);
                
                // 更新最后高亮的窗口句柄
                _lastHighlightedWindow = windowHandle;
            }
            else
            {
                if (_logService != null) _logService.Debug("窗口无效或句柄为零，隐藏高亮");
                HideHighlight();
                _lastHighlightedWindow = IntPtr.Zero;
            }
            
            if (_logService != null) _logService.Debug("--- 窗口检测结束 ---");
        }

        /// <summary>
        /// 检测控件模式
        /// </summary>
        private void DetectElement(System.Drawing.Point screenPos)
        {
            try
            {
                var element = _windowDetectionService.GetElementUnderPoint(screenPos);
                
                if (element != null)
                {
                    var bounds = _windowDetectionService.GetElementBounds(element);
                    var elementInfo = _windowDetectionService.GetElementInfo(element);
                    
                    UpdateHighlight(bounds, string.Format("控件: {0}\n类型: {1}\n自动化ID: {2}", elementInfo.Name, elementInfo.ControlType, elementInfo.AutomationId));
                }
                else
                {
                    HideHighlight();
                }
            }
            catch (Exception ex)
            {
                HideHighlight();
            }
        }

        /// <summary>
        /// 更新高亮显示
        /// </summary>
        private void UpdateHighlight(System.Drawing.Rectangle bounds, string info)
        {
            if (_logService != null) _logService.Debug("--- 开始更新高亮 ---");
            if (_logService != null) _logService.Debug("高亮边界: X={0}, Y={1}, W={2}, H={3}", bounds.X, bounds.Y, bounds.Width, bounds.Height);
            if (_logService != null) _logService.Debug("信息文本: {0}", info);
            
            try
            {
                if (_logService != null) _logService.Debug("在UI线程中执行高亮更新...");
                
                // 转换为窗口坐标
                var virtualScreen = SystemInformation.VirtualScreen;
                var windowX = bounds.X - virtualScreen.X;
                var windowY = bounds.Y - virtualScreen.Y;
                
                if (_logService != null) _logService.Debug("窗口坐标转换: windowX={0}, windowY={1}", windowX, windowY);
                
                // 更新高亮矩形
                if (_logService != null) _logService.Debug("设置HighlightRectangle可见性和位置...");
                Canvas.SetLeft(HighlightRectangle, windowX);
                Canvas.SetTop(HighlightRectangle, windowY);
                HighlightRectangle.Width = bounds.Width;
                HighlightRectangle.Height = bounds.Height;
                HighlightRectangle.Visibility = Visibility.Visible;
                
                if (_logService != null) _logService.Debug("HighlightRectangle设置完成: Left={0}, Top={1}, Width={2}, Height={3}, Visibility={4}", 
                    Canvas.GetLeft(HighlightRectangle), Canvas.GetTop(HighlightRectangle), 
                    HighlightRectangle.Width, HighlightRectangle.Height, HighlightRectangle.Visibility);
                
                // 更新信息提示
                if (_logService != null) _logService.Debug("设置WindowInfoPanel可见性和文本...");
                WindowInfoText.Text = info;
                Canvas.SetLeft(WindowInfoPanel, windowX + 5);
                Canvas.SetTop(WindowInfoPanel, windowY - 60);
                
                // 确保信息面板在屏幕内
                if (Canvas.GetTop(WindowInfoPanel) < 0)
                {
                    Canvas.SetTop(WindowInfoPanel, windowY + 5);
                }
                
                WindowInfoPanel.Visibility = Visibility.Visible;
                
                if (_logService != null) _logService.Debug("WindowInfoPanel设置完成: Left={0}, Top={1}, Visibility={2}", 
                    Canvas.GetLeft(WindowInfoPanel), Canvas.GetTop(WindowInfoPanel), WindowInfoPanel.Visibility);
                    
                if (_logService != null) _logService.Debug("UI更新完成");
            }
            catch (Exception ex)
            {
                if (_logService != null) _logService.Error(ex, "更新高亮显示错误");
            }
            
            if (_logService != null) _logService.Debug("--- 高亮更新结束 ---");
        }

        /// <summary>
        /// 隐藏高亮显示
        /// </summary>
        private void HideHighlight()
        {
            if (_logService != null) _logService.Debug("隐藏高亮显示");
            HighlightRectangle.Visibility = Visibility.Collapsed;
            WindowInfoPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 捕获背景图像
        /// </summary>
        private void CaptureBackground()
        {
            try
            {
                // 使用虚拟屏幕的完整边界来捕获所有显示器的内容
                var virtualScreen = SystemInformation.VirtualScreen;
                if (_logService != null) _logService.Debug("捕获背景图像: 虚拟屏幕 X={0}, Y={1}, W={2}, H={3}", virtualScreen.X, virtualScreen.Y, virtualScreen.Width, virtualScreen.Height);
                
                _backgroundImage = new Bitmap(virtualScreen.Width, virtualScreen.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(_backgroundImage))
                {
                    // 从虚拟屏幕的左上角开始捕获整个虚拟屏幕
                    graphics.CopyFromScreen(virtualScreen.X, virtualScreen.Y, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
                }
                
                if (_logService != null) _logService.Debug("背景图像创建成功: W={0}, H={1}", _backgroundImage.Width, _backgroundImage.Height);
            }
            catch (Exception ex)
            {
                if (_logService != null) _logService.Error(ex, "捕获背景图像失败");
            }
        }

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelScreenshot();
            }
            else if (e.Key == Key.Tab)
            {
                // 切换检测模式（窗口/控件）
                _currentDetectionMode = _currentDetectionMode == DetectionMode.Window 
                    ? DetectionMode.Element 
                    : DetectionMode.Window;
                
                // 清除当前高亮
                HideHighlight();
                _lastHighlightedWindow = IntPtr.Zero;
                
                if (_logService != null) _logService.Debug("切换到{0}检测模式", _currentDetectionMode == DetectionMode.Window ? "窗口" : "控件");
                e.Handled = true;
            }
        }

        /// <summary>
        /// 鼠标左键按下事件
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 参考通用做法：统一处理拖拽选择，不管是否有高亮窗口
            _isSelecting = true;
            _startPoint = e.GetPosition(this);
            _endPoint = _startPoint;
            
            // 如果有高亮窗口，先隐藏它
            if (HighlightRectangle.Visibility == Visibility.Visible)
            {
                HideHighlight();
            }
            
            SelectionRectangle.Visibility = Visibility.Visible;
            InfoPanel.Visibility = Visibility.Visible;
            
            CaptureMouse();
            UpdateSelection();
        }
        

        
        // 全屏截图和延时截图功能已移除，简化后不再需要

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_logService != null) _logService.Debug("*** Window_MouseMove 事件被触发 ***");
            
            if (_isSelecting)
            {
                // 正在选择时更新选择区域
                _endPoint = e.GetPosition(this);
                UpdateSelection();
            }
            else if (_isHighlightingEnabled && !_isSelecting)
            {
                // 智能模式：直接进行窗口检测
                var currentPosition = e.GetPosition(this);
                
                if (_logService != null) _logService.Debug("*** 智能高亮模式检查 ***");
                if (_logService != null) _logService.Debug("_isHighlightingEnabled: {0}", _isHighlightingEnabled);
                if (_logService != null) _logService.Debug("_isSelecting: {0}", _isSelecting);
                if (_logService != null) _logService.Debug("_windowDetectionService != null: {0}", _windowDetectionService != null);
                if (_logService != null) _logService.Debug("鼠标移动事件: 当前位置=({0}, {1}), 上次位置=({2}, {3})", 
                    currentPosition.X, currentPosition.Y, _lastMousePosition.X, _lastMousePosition.Y);
                
                // 检查鼠标是否移动了足够的距离
                if (Math.Abs(currentPosition.X - _lastMousePosition.X) >= 2 || 
                    Math.Abs(currentPosition.Y - _lastMousePosition.Y) >= 2)
                {
                    _lastMousePosition = new System.Windows.Point(currentPosition.X, currentPosition.Y);
                    
                    if (_logService != null) _logService.Debug("*** 鼠标移动距离足够，开始窗口检测... ***");
                    
                    // 调用检测方法
                    DetectWindowUnderMouse();
                }
                else
                {
                    if (_logService != null) _logService.Debug("鼠标移动距离不足，跳过检测");
                }
            }
            else
            {
                if (_logService != null) _logService.Debug("*** 跳过智能高亮检测 - _isHighlightingEnabled: {0}, _isSelecting: {1} ***", _isHighlightingEnabled, _isSelecting);
            }
        }

        /// <summary>
        /// 鼠标左键释放事件
        /// </summary>
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                ReleaseMouseCapture();
                
                var selectionRect = GetSelectionRectangle();
                if (selectionRect.Width > 10 && selectionRect.Height > 10)
                {
                    // 显示工具栏
                    ShowToolBar();
                }
                else
                {
                    // 选择区域太小，取消截图
                    CancelScreenshot();
                }
            }
        }

        /// <summary>
        /// 更新选择区域显示
        /// </summary>
        private void UpdateSelection()
        {
            var rect = GetSelectionRectangle();
            
            Canvas.SetLeft(SelectionRectangle, rect.X);
            Canvas.SetTop(SelectionRectangle, rect.Y);
            SelectionRectangle.Width = rect.Width;
            SelectionRectangle.Height = rect.Height;
            
            // 更新信息面板
            InfoText.Text = string.Format("区域: {0:F0} × {1:F0}", rect.Width, rect.Height);
            Canvas.SetLeft(InfoPanel, rect.X + 5);
            Canvas.SetTop(InfoPanel, rect.Y - 25);
            
            // 确保信息面板在屏幕内
            if (Canvas.GetTop(InfoPanel) < 0)
            {
                Canvas.SetTop(InfoPanel, rect.Y + 5);
            }

            // 更新工具栏位置
            Canvas.SetLeft(ToolBar, rect.Right - ToolBar.ActualWidth);
            Canvas.SetTop(ToolBar, rect.Bottom + 5);
        }

        /// <summary>
        /// 显示工具栏
        /// </summary>
        private void ShowToolBar()
        {
            var rect = GetSelectionRectangle();
            if (rect.Width > 0 && rect.Height > 0)
            {
                ToolBar.Visibility = Visibility.Visible;
                
                // 计算工具栏位置
                var toolBarX = rect.X + rect.Width - ToolBar.ActualWidth - 10;
                var toolBarY = rect.Y + rect.Height + 10;
                
                // 确保工具栏不超出屏幕边界
                if (toolBarX < 0) toolBarX = 10;
                if (toolBarY + ToolBar.ActualHeight > ActualHeight) 
                    toolBarY = rect.Y - ToolBar.ActualHeight - 10;
                
                Canvas.SetLeft(ToolBar, toolBarX);
                Canvas.SetTop(ToolBar, toolBarY);
                
                // 触发动画效果
                ToolBar.Opacity = 0;
                var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                ToolBar.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            }
        }

        /// <summary>
        /// 获取选择矩形
        /// </summary>
        /// <returns>选择矩形</returns>
        private Rect GetSelectionRectangle()
        {
            var x = Math.Min(_startPoint.X, _endPoint.X);
            var y = Math.Min(_startPoint.Y, _endPoint.Y);
            var width = Math.Abs(_endPoint.X - _startPoint.X);
            var height = Math.Abs(_endPoint.Y - _startPoint.Y);
            
            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// 确认按钮点击事件 - 直接进行OCR识别
        /// </summary>
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await CaptureSelectedRegionAsync();
                if (result != null && result.IsSuccess)
                {

                    
                    if (ScreenshotCompleted != null)
                    {
                        ScreenshotCompleted(this, result);
                    }
                    Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("截图失败: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开设置窗口
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this; // 设置父窗口
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                // 暂停检测定时器
                if (_detectionTimer != null)
                {
                    _detectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                
                // 使用非模态窗口
                settingsWindow.Show();
                
                // 监听设置窗口关闭事件
                settingsWindow.Closed += (s, args) =>
                {
                    // 重新启动检测定时器
                    if (_detectionTimer != null && _isHighlightingEnabled)
                    {
                        _detectionTimer.Change(100, 100);
                    }
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("无法打开设置窗口: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // 确保检测定时器重新启动
                if (_detectionTimer != null && _isHighlightingEnabled)
                {
                    _detectionTimer.Change(100, 100);
                }
            }
        }
        

        
        /// <summary>
        /// 视图模型属性变化事件处理
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentMode")
            {
                HandleModeChanged();
            }
        }
        
        /// <summary>
        /// 处理模式变化
        /// </summary>
        private void HandleModeChanged()
        {
            // 智能模式始终启用高亮
            _isHighlightingEnabled = true;
        }
        

        
        /// <summary>
        /// 取消选择
        /// </summary>
        private void CancelSelection()
        {
            _isSelecting = false;
            ReleaseMouseCapture();
            
            SelectionRectangle.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;
            ToolBar.Visibility = Visibility.Collapsed;
            
            _viewModel.CancelSelection();
        }

        /// <summary>
        /// 取消截图
        /// </summary>
        private void CancelScreenshot()
        {
            if (ScreenshotCancelled != null)
            {
                ScreenshotCancelled(this, EventArgs.Empty);
            }
            Close();
        }

        /// <summary>
        /// 捕获选中区域
        /// </summary>
        /// <returns>截图结果</returns>
        private async System.Threading.Tasks.Task<ScreenshotResult> CaptureSelectedRegionAsync()
        {
            try
            {
                var selectionRect = GetSelectionRectangle();
                var virtualScreen = SystemInformation.VirtualScreen;
                
                // 直接使用选择区域在虚拟屏幕中的位置
                // 因为窗口已经覆盖整个虚拟屏幕，选择区域的坐标就是在背景图像中的坐标
                var relativeX = (int)selectionRect.X;
                var relativeY = (int)selectionRect.Y;
                
                // 计算实际的屏幕坐标（用于返回结果）
                var screenRect = new System.Drawing.Rectangle(
                    virtualScreen.X + relativeX,
                    virtualScreen.Y + relativeY,
                    (int)selectionRect.Width,
                    (int)selectionRect.Height);
                
                // 确保坐标在有效范围内
                relativeX = Math.Max(0, Math.Min(relativeX, _backgroundImage.Width - 1));
                relativeY = Math.Max(0, Math.Min(relativeY, _backgroundImage.Height - 1));
                var cropWidth = Math.Min((int)selectionRect.Width, _backgroundImage.Width - relativeX);
                var cropHeight = Math.Min((int)selectionRect.Height, _backgroundImage.Height - relativeY);
                
                // 从背景图像中裁剪选中区域
                var croppedImage = new Bitmap(cropWidth, cropHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(croppedImage))
                {
                    var sourceRect = new System.Drawing.Rectangle(relativeX, relativeY, cropWidth, cropHeight);
                    graphics.DrawImage(_backgroundImage, new System.Drawing.Rectangle(0, 0, cropWidth, cropHeight), sourceRect, GraphicsUnit.Pixel);
                }
                
                return new ScreenshotResult
                {
                    Image = croppedImage,
                    CaptureRegion = screenRect,
                    CaptureTime = DateTime.Now,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 根据文件扩展名获取图像格式
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>图像格式</returns>
        private ImageFormat GetImageFormat(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".gif":
                    return ImageFormat.Gif;
                default:
                    return ImageFormat.Png;
            }
        }

        /// <summary>
        /// 将GDI+位图转换为WPF位图源
        /// </summary>
        /// <param name="bitmap">GDI+位图</param>
        /// <returns>WPF位图源</returns>
        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// 删除GDI对象
        /// </summary>
        /// <param name="hObject">对象句柄</param>
        /// <returns>是否成功</returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 清理检测定时器
            if (_detectionTimer != null)
            {
                _detectionTimer.Dispose();
                _detectionTimer = null;
            }
            
            // 清理背景位图
            if (_backgroundImage != null)
            {
                _backgroundImage.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
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
using AIAnswerTool.Services;
using AIAnswerTool.ViewModels;
using AIAnswerTool.Views;
using AIAnswerTool.Models;

namespace AIAnswerTool.Views
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
        private DetectionMode _currentDetectionMode = DetectionMode.Window;
        private System.Threading.Timer _detectionTimer;
        private System.Windows.Point _lastMousePosition;
        private IntPtr _lastHighlightedWindow = IntPtr.Zero;
        private bool _isHighlightingEnabled = true;
        
        // 延时截图相关字段
        private DispatcherTimer _countdownTimer;
        private int _countdownSeconds = 3;

        /// <summary>
        /// 截图完成事件
        /// </summary>
        public event EventHandler<ScreenshotResult> ScreenshotCompleted;

        /// <summary>
        /// 截图取消事件
        /// </summary>
        public event EventHandler ScreenshotCancelled;

        public ScreenshotWindow(System.Drawing.Rectangle bounds) : this(bounds, null)
        {
        }
        
        public ScreenshotWindow(System.Drawing.Rectangle bounds, IWindowDetectionService windowDetectionService)
        {
            InitializeComponent();
            _windowDetectionService = windowDetectionService;
            InitializeWindow(bounds);
            if (_windowDetectionService != null)
            {
                InitializeWindowDetection();
            }
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
                System.Diagnostics.Debug.WriteLine(string.Format("虚拟屏幕: X={0}, Y={1}, W={2}, H={3}", virtualScreen.X, virtualScreen.Y, virtualScreen.Width, virtualScreen.Height));
                for (int i = 0; i < allScreens.Length; i++)
                {
                    var screen = allScreens[i];
                    System.Diagnostics.Debug.WriteLine(string.Format("显示器{0}: X={1}, Y={2}, W={3}, H={4}, Primary={5}", i, screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height, screen.Primary));
                }
                
                // 设置窗口位置和大小，确保覆盖所有显示器
                Left = virtualScreen.Left;
                Top = virtualScreen.Top;
                Width = virtualScreen.Width;
                Height = virtualScreen.Height;
                
                System.Diagnostics.Debug.WriteLine(string.Format("窗口设置: Left={0}, Top={1}, Width={2}, Height={3}", Left, Top, Width, Height));
                
                // 强制窗口显示在所有屏幕上
                Show();
                
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
        /// 初始化窗口检测功能
        /// </summary>
        private void InitializeWindowDetection()
        {
            if (_windowDetectionService != null)
            {
                // 创建检测定时器，每100ms检测一次
                _detectionTimer = new System.Threading.Timer(DetectWindowUnderMouse, null, Timeout.Infinite, 100);
            }
        }

        /// <summary>
        /// 检测鼠标下的窗口
        /// </summary>
        private void DetectWindowUnderMouse(object state)
        {
            if (!_isHighlightingEnabled || _isSelecting || _windowDetectionService == null)
                return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    var mousePos = Mouse.GetPosition(this);
                    
                    // 避免过于频繁的检测
                    if (Math.Abs(mousePos.X - _lastMousePosition.X) < 5 && 
                        Math.Abs(mousePos.Y - _lastMousePosition.Y) < 5)
                        return;
                    
                    _lastMousePosition = mousePos;
                    
                    // 转换为屏幕坐标
                    var screenPoint = PointToScreen(mousePos);
                    var screenPos = new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y);
                    
                    if (_currentDetectionMode == DetectionMode.Window)
                    {
                        DetectWindow(screenPos);
                    }
                    else
                    {
                        DetectElement(screenPos);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("窗口检测错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 检测窗口模式
        /// </summary>
        private void DetectWindow(System.Drawing.Point screenPos)
        {
            var windowHandle = _windowDetectionService.GetWindowUnderCursor();
            
            if (windowHandle != _lastHighlightedWindow)
            {
                _lastHighlightedWindow = windowHandle;
                
                if (windowHandle != IntPtr.Zero && _windowDetectionService.IsValidWindow(windowHandle))
                {
                    var bounds = _windowDetectionService.GetWindowBounds(windowHandle);
                    var windowInfo = _windowDetectionService.GetWindowInfo(windowHandle);
                    
                    UpdateHighlight(bounds, string.Format("窗口: {0}\n类名: {1}\n进程: {2}", windowInfo.Title, windowInfo.ClassName, windowInfo.ProcessName));
                }
                else
                {
                    HideHighlight();
                }
            }
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
                System.Diagnostics.Debug.WriteLine(string.Format("控件检测错误: {0}", ex.Message));
                HideHighlight();
            }
        }

        /// <summary>
        /// 更新高亮显示
        /// </summary>
        private void UpdateHighlight(System.Drawing.Rectangle bounds, string info)
        {
            try
            {
                // 转换为窗口坐标
                var virtualScreen = SystemInformation.VirtualScreen;
                var windowX = bounds.X - virtualScreen.X;
                var windowY = bounds.Y - virtualScreen.Y;
                
                // 更新高亮矩形
                Canvas.SetLeft(HighlightRectangle, windowX);
                Canvas.SetTop(HighlightRectangle, windowY);
                HighlightRectangle.Width = bounds.Width;
                HighlightRectangle.Height = bounds.Height;
                HighlightRectangle.Visibility = Visibility.Visible;
                
                // 更新信息提示
                WindowInfoText.Text = info;
                Canvas.SetLeft(WindowInfoPanel, windowX + 5);
                Canvas.SetTop(WindowInfoPanel, windowY - 60);
                
                // 确保信息面板在屏幕内
                if (Canvas.GetTop(WindowInfoPanel) < 0)
                {
                    Canvas.SetTop(WindowInfoPanel, windowY + 5);
                }
                
                WindowInfoPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("更新高亮显示错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 隐藏高亮显示
        /// </summary>
        private void HideHighlight()
        {
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
                System.Diagnostics.Debug.WriteLine(string.Format("捕获背景图像: 虚拟屏幕 X={0}, Y={1}, W={2}, H={3}", virtualScreen.X, virtualScreen.Y, virtualScreen.Width, virtualScreen.Height));
                
                _backgroundImage = new Bitmap(virtualScreen.Width, virtualScreen.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(_backgroundImage))
                {
                    // 从虚拟屏幕的左上角开始捕获整个虚拟屏幕
                    graphics.CopyFromScreen(virtualScreen.X, virtualScreen.Y, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
                }
                
                System.Diagnostics.Debug.WriteLine(string.Format("背景图像创建成功: W={0}, H={1}", _backgroundImage.Width, _backgroundImage.Height));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("捕获背景图像失败: {0}", ex.Message));
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
                // 切换检测模式
                _currentDetectionMode = _currentDetectionMode == DetectionMode.Window 
                    ? DetectionMode.Element 
                    : DetectionMode.Window;
                
                // 清除当前高亮
                HideHighlight();
                _lastHighlightedWindow = IntPtr.Zero;
                
                System.Diagnostics.Debug.WriteLine(string.Format("切换到{0}检测模式", _currentDetectionMode == DetectionMode.Window ? "窗口" : "控件"));
                e.Handled = true;
            }
        }

        /// <summary>
        /// 鼠标左键按下事件
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (_viewModel.CurrentMode)
            {
                case ScreenshotMode.FreeSelection:
                    HandleFreeSelectionMouseDown(e);
                    break;
                case ScreenshotMode.SmartWindow:
                    HandleSmartWindowMouseDown(e);
                    break;
                case ScreenshotMode.FullScreen:
                    HandleFullScreenCapture();
                    break;
                case ScreenshotMode.DelayedCapture:
                    HandleDelayedCapture();
                    break;
            }
        }
        
        /// <summary>
        /// 处理自由选择模式的鼠标按下
        /// </summary>
        private void HandleFreeSelectionMouseDown(MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(this);
            _endPoint = _startPoint;
            
            SelectionRectangle.Visibility = Visibility.Visible;
            InfoPanel.Visibility = Visibility.Visible;
            
            CaptureMouse();
            UpdateSelection();
        }
        
        /// <summary>
        /// 处理智能窗口模式的鼠标按下
        /// </summary>
        private void HandleSmartWindowMouseDown(MouseButtonEventArgs e)
        {
            if (_windowDetectionService != null && HighlightRectangle.Visibility == Visibility.Visible)
            {
                // 使用当前高亮的窗口区域作为选择区域
                var highlightLeft = Canvas.GetLeft(HighlightRectangle);
                var highlightTop = Canvas.GetTop(HighlightRectangle);
                
                _startPoint = new System.Windows.Point(highlightLeft, highlightTop);
                _endPoint = new System.Windows.Point(highlightLeft + HighlightRectangle.Width, highlightTop + HighlightRectangle.Height);
                
                // 隐藏高亮，显示选择
                HideHighlight();
                SelectionRectangle.Visibility = Visibility.Visible;
                InfoPanel.Visibility = Visibility.Visible;
                
                UpdateSelection();
                ShowToolBar();
            }
        }
        
        /// <summary>
        /// 处理全屏截图
        /// </summary>
        private void HandleFullScreenCapture()
        {
            var virtualScreen = SystemInformation.VirtualScreen;
            _startPoint = new System.Windows.Point(0, 0);
            _endPoint = new System.Windows.Point(virtualScreen.Width, virtualScreen.Height);
            
            SelectionRectangle.Visibility = Visibility.Visible;
            InfoPanel.Visibility = Visibility.Visible;
            
            UpdateSelection();
            ShowToolBar();
        }
        
        /// <summary>
        /// 处理延时截图
        /// </summary>
        private void HandleDelayedCapture()
        {
            StartCountdown();
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isSelecting && _viewModel.CurrentMode == ScreenshotMode.FreeSelection)
            {
                _endPoint = e.GetPosition(this);
                UpdateSelection();
            }
            else if (_isHighlightingEnabled && !_isSelecting && _viewModel.CurrentMode == ScreenshotMode.SmartWindow)
            {
                // 更新鼠标位置用于窗口检测
                var currentPosition = e.GetPosition(this);
                _lastMousePosition = new System.Windows.Point(currentPosition.X, currentPosition.Y);
                
                // 启动检测定时器
                if (_detectionTimer != null)
                {
                    _detectionTimer.Change(50, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// 鼠标左键释放事件
        /// </summary>
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _viewModel.CurrentMode == ScreenshotMode.FreeSelection)
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

        private void SmartModeButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到智能窗口模式
            _viewModel.CurrentMode = ScreenshotMode.SmartWindow;
            
            // 更新按钮图标颜色以反映当前模式
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var path = button.Content as System.Windows.Shapes.Path;
                if (path != null)
                {
                    path.Fill = new SolidColorBrush(Colors.Orange);
                }
            }
            
            // 重置其他按钮颜色
            ResetButtonColors(button);
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到全屏截图模式
            _viewModel.CurrentMode = ScreenshotMode.FullScreen;
            
            // 更新按钮图标颜色
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var path = button.Content as System.Windows.Shapes.Path;
                if (path != null)
                {
                    path.Fill = new SolidColorBrush(Colors.Green);
                }
            }
            
            // 重置其他按钮颜色
            ResetButtonColors(button);
            
            // 立即执行全屏截图
            HandleFullScreenCapture();
        }

        private void DelayButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到延时截图模式
            _viewModel.CurrentMode = ScreenshotMode.DelayedCapture;
            
            // 更新按钮图标颜色
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var path = button.Content as System.Windows.Shapes.Path;
                if (path != null)
                {
                    path.Fill = new SolidColorBrush(Colors.Red);
                }
            }
            
            // 重置其他按钮颜色
            ResetButtonColors(button);
            
            // 开始延时截图
            HandleDelayedCapture();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开设置窗口
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format("无法打开设置窗口: {0}", ex.Message), "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// 自由选择模式按钮点击事件
        /// </summary>
        private void FreeSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到自由选择模式
            _viewModel.CurrentMode = ScreenshotMode.FreeSelection;
            
            // 更新按钮图标颜色
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var path = button.Content as System.Windows.Shapes.Path;
                if (path != null)
                {
                    path.Fill = new SolidColorBrush(Colors.Blue);
                }
            }
            
            // 重置其他按钮颜色
            ResetButtonColors(button);
        }
        
        /// <summary>
        /// 重置其他按钮颜色
        /// </summary>
        private void ResetButtonColors(System.Windows.Controls.Button activeButton)
        {
            // 获取工具栏中的所有按钮
            var toolbar = FindName("ToolBarPanel") as StackPanel;
            if (toolbar != null)
            {
                foreach (var child in toolbar.Children)
                {
                    var button = child as System.Windows.Controls.Button;
                    if (button != null && button != activeButton)
                    {
                        var path = button.Content as System.Windows.Shapes.Path;
                        if (path != null)
                        {
                            // 设置为默认颜色（白色）
                            path.Fill = new SolidColorBrush(Colors.White);
                        }
                    }
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
            // 停止当前操作
            StopCountdown();
            CancelSelection();
            
            // 根据新模式调整UI状态
            switch (_viewModel.CurrentMode)
            {
                case ScreenshotMode.FreeSelection:
                    _isHighlightingEnabled = false;
                    HideHighlight();
                    break;
                case ScreenshotMode.SmartWindow:
                    _isHighlightingEnabled = true;
                    break;
                case ScreenshotMode.FullScreen:
                case ScreenshotMode.DelayedCapture:
                    _isHighlightingEnabled = false;
                    HideHighlight();
                    break;
            }
        }
        
        /// <summary>
        /// 开始倒计时
        /// </summary>
        private void StartCountdown()
        {
            _countdownSeconds = 3;
            _viewModel.StartCountdown(_countdownSeconds);
            
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }
        
        /// <summary>
        /// 倒计时定时器事件
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _countdownSeconds--;
            _viewModel.CountdownSeconds = _countdownSeconds;
            
            if (_countdownSeconds <= 0)
            {
                StopCountdown();
                // 执行全屏截图
                HandleFullScreenCapture();
            }
        }
        
        /// <summary>
        /// 停止倒计时
        /// </summary>
        private void StopCountdown()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }
            _viewModel.StopCountdown();
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
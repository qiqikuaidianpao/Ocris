using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AIAnswerTool.Services;
using AIAnswerTool.ViewModels;

namespace AIAnswerTool.Views
{
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

        /// <summary>
        /// 截图完成事件
        /// </summary>
        public event EventHandler<ScreenshotResult> ScreenshotCompleted;

        /// <summary>
        /// 截图取消事件
        /// </summary>
        public event EventHandler ScreenshotCancelled;

        public ScreenshotWindow(Rectangle bounds)
        {
            InitializeComponent();
            InitializeWindow(bounds);
        }

        /// <summary>
        /// 初始化窗口
        /// </summary>
        private void InitializeWindow(Rectangle bounds)
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
        /// 捕获背景图像
        /// </summary>
        private void CaptureBackground()
        {
            try
            {
                // 使用虚拟屏幕的完整边界来捕获所有显示器的内容
                var virtualScreen = SystemInformation.VirtualScreen;
                System.Diagnostics.Debug.WriteLine(string.Format("捕获背景图像: 虚拟屏幕 X={0}, Y={1}, W={2}, H={3}", virtualScreen.X, virtualScreen.Y, virtualScreen.Width, virtualScreen.Height));
                
                _backgroundImage = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppArgb);
                
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
        }

        /// <summary>
        /// 鼠标左键按下事件
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        /// 鼠标移动事件
        /// </summary>
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _endPoint = e.GetPosition(this);
                UpdateSelection();
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
                    ToolBar.Visibility = Visibility.Visible;
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
            CancelScreenshot();
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
                var screenRect = new Rectangle(
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
                var croppedImage = new Bitmap(cropWidth, cropHeight, PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(croppedImage))
                {
                    var sourceRect = new Rectangle(relativeX, relativeY, cropWidth, cropHeight);
                    graphics.DrawImage(_backgroundImage, new Rectangle(0, 0, cropWidth, cropHeight), sourceRect, GraphicsUnit.Pixel);
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
            var extension = Path.GetExtension(fileName).ToLower();
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
            if (_backgroundImage != null)
            {
                _backgroundImage.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
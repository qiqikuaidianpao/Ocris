using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using AIAnswerTool.Views;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 截图服务实现
    /// </summary>
    public class ScreenshotService : IScreenshotService
    {
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private ScreenshotWindow _screenshotWindow;

        /// <summary>
        /// 截图完成事件
        /// </summary>
        public event EventHandler<ScreenshotResult> ScreenshotCaptured;

        public ScreenshotService(ILogService logService, IConfigService configService)
        {
            if (logService == null)
                throw new ArgumentNullException("logService");
            if (configService == null)
                throw new ArgumentNullException("configService");
            _logService = logService;
            _configService = configService;
        }

        /// <summary>
        /// 截取全屏
        /// </summary>
        /// <returns>截图结果</returns>
        public async Task<ScreenshotResult> CaptureFullScreenAsync()
        {
            try
            {
                var bounds = GetVirtualScreenBounds();
                return await CaptureAreaAsync(bounds);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "全屏截图失败");
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="area">截图区域</param>
        /// <returns>截图结果</returns>
        public async Task<ScreenshotResult> CaptureAreaAsync(Rectangle area)
        {
            try
            {
                using (var bitmap = new Bitmap(area.Width, area.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size);
                    _logService.Info(string.Format("原始截图成功，尺寸：{0}x{1}", bitmap.Width, bitmap.Height));

                    var clonedBitmap = (Bitmap)bitmap.Clone();
                    _logService.Info(string.Format("克隆截图成功，尺寸：{0}x{1}", clonedBitmap.Width, clonedBitmap.Height));

                    var result = new ScreenshotResult
                    {
                        Image = clonedBitmap,
                        CaptureArea = area,
                        CaptureRegion = area,
                        CaptureTime = DateTime.Now,
                        IsSuccess = true
                    };

                    if (ScreenshotCaptured != null)
                    {
                        ScreenshotCaptured.Invoke(this, result);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "区域截图失败");
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 截取指定窗口
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>截图结果</returns>
        public async Task<ScreenshotResult> CaptureWindowAsync(IntPtr windowHandle)
        {
            try
            {
                RECT rect;
                if (!GetWindowRect(windowHandle, out rect))
                {
                    throw new InvalidOperationException("无法获取窗口位置信息");
                }

                var area = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                return await CaptureAreaAsync(area);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "窗口截图失败");
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 交互式区域选择截图
        /// </summary>
        /// <returns>截图结果</returns>
        public async Task<ScreenshotResult> CaptureInteractiveAreaAsync()
        {
            try
            {
                return await StartInteractiveScreenshotAsync();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "交互式截图失败");
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }



        private async Task<ScreenshotResult> StartInteractiveScreenshotAsync()
        {
            try
            {
                var tcs = new TaskCompletionSource<ScreenshotResult>();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_screenshotWindow != null)
                        {
                            _screenshotWindow.Close();
                        }

                        var bounds = GetVirtualScreenBounds();
                        _screenshotWindow = new ScreenshotWindow(bounds);
                        _screenshotWindow.ScreenshotCompleted += (sender, result) =>
                        {
                            try
                            {
                                if (ScreenshotCaptured != null)
                                {
                                    ScreenshotCaptured.Invoke(this, result);
                                }
                                tcs.SetResult(result);
                            }
                            catch (Exception ex)
                            {
                                _logService.Error(ex, "截图完成事件处理失败");
                                tcs.SetException(ex);
                            }
                        };

                        _screenshotWindow.Show();
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "创建截图窗口失败");
                        tcs.SetException(ex);
                    }
                });

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "交互式截图失败");
                return new ScreenshotResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 获取虚拟屏幕（所有显示器组合成的矩形）的边界
        /// </summary>
        /// <returns>虚拟屏幕的边界</returns>
        private Rectangle GetVirtualScreenBounds()
        {
            return SystemInformation.VirtualScreen;
        }

        /// <summary>
        /// 获取主显示器信息
        /// </summary>
        /// <returns>显示器尺寸</returns>
        public Rectangle GetPrimaryScreenBounds()
        {
            try
            {
                return Screen.PrimaryScreen.Bounds;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取主显示器信息失败");
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// 获取所有显示器信息
        /// </summary>
        /// <returns>显示器列表</returns>
        public Rectangle[] GetAllScreenBounds()
        {
            try
            {
                var screens = Screen.AllScreens;
                var bounds = new Rectangle[screens.Length];
                for (int i = 0; i < screens.Length; i++)
                {
                    bounds[i] = screens[i].Bounds;
                }
                return bounds;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取所有显示器信息失败");
                return new Rectangle[0];
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_screenshotWindow != null)
            {
                _screenshotWindow.Close();
                _screenshotWindow = null;
            }
        }

        // Windows API
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
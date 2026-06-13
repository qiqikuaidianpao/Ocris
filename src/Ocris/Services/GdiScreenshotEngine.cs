using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Ocris.Views;
using Application = System.Windows.Application;

namespace Ocris.Services
{
    /// <summary>
    /// 基于 GDI 的轻量截图引擎（去 ShareX 化）。
    /// 捕获使用 CopyFromScreen；交互式选区复用自包含的 ScreenshotWindow
    /// （自带全屏背景 + UIA 窗口智能高亮 + 拖拽选区）。
    /// </summary>
    public class GdiScreenshotEngine : IScreenshotEngine, IAsyncInitializable, IDisposable
    {
        private readonly ILogService _logService;
        private readonly IWindowDetectionService _windowDetectionService;
        private bool _disposed;

        public event EventHandler<ScreenshotEventArgs> ScreenshotCompleted;

        public GdiScreenshotEngine(ILogService logService, IWindowDetectionService windowDetectionService)
        {
            if (logService == null) throw new ArgumentNullException("logService");
            _logService = logService;
            _windowDetectionService = windowDetectionService; // 可为空（智能高亮不可用）
        }

        /// <summary>
        /// IAsyncInitializable —— GDI 无需初始化
        /// </summary>
        public Task InitializeAsync()
        {
            return Task.FromResult(0);
        }

        public Task<bool> InitializeWithResultAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// 交互式区域选择截图 —— 调用 ScreenshotWindow（UIA 智能高亮 + 拖拽选区）。
        /// 必须在 UI 线程执行（WPF 窗口）。
        /// </summary>
        public Task<Bitmap> CaptureInteractiveAsync()
        {
            var tcs = new TaskCompletionSource<Bitmap>();

            Action showWindow = () =>
            {
                ScreenshotResult captured = null;
                try
                {
                    var bounds = GetVirtualScreenBounds();
                    var window = new ScreenshotWindow(bounds, _windowDetectionService, _logService);

                    EventHandler<ScreenshotResult> onCompleted = (s, e) => { captured = e; };
                    EventHandler onCancelled = (s, e) => { captured = null; };
                    EventHandler onClosed = null;
                    onClosed = (s, e) =>
                    {
                        window.ScreenshotCompleted -= onCompleted;
                        window.ScreenshotCancelled -= onCancelled;
                        window.Closed -= onClosed;

                        Bitmap bitmap = captured != null && captured.IsSuccess ? captured.Image : null;
                        Rectangle region = captured != null ? captured.CaptureRegion : Rectangle.Empty;

                        // 先触发引擎事件（让适配器拿到选区 Region）
                        OnScreenshotCompleted(bitmap, region, bitmap != null);
                        // 再完成返回 Task
                        tcs.TrySetResult(bitmap);
                    };

                    window.ScreenshotCompleted += onCompleted;
                    window.ScreenshotCancelled += onCancelled;
                    window.Closed += onClosed;
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    _logService.Error(string.Format("交互式截图失败: {0}", ex.Message));
                    OnScreenshotCompleted(null, Rectangle.Empty, false);
                    tcs.TrySetResult(null);
                }
            };

            // ScreenshotWindow 是 WPF 窗口，必须在 UI 线程创建
            var app = Application.Current;
            Dispatcher dispatcher = app != null ? app.Dispatcher : null;
            if (dispatcher == null)
            {
                showWindow();
            }
            else if (dispatcher.CheckAccess())
            {
                showWindow();
            }
            else
            {
                dispatcher.Invoke(showWindow);
            }

            return tcs.Task;
        }

        public Task<Bitmap> CaptureFullScreenAsync()
        {
            return Task.Run(() => CaptureRectangle(GetVirtualScreenBounds()));
        }

        public Task<Bitmap> CaptureRegionAsync(Rectangle region)
        {
            return Task.Run(() => CaptureRectangle(region));
        }

        public Task<Bitmap> CaptureWindowAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    Rectangle bounds = _windowDetectionService != null
                        ? _windowDetectionService.GetWindowBounds(windowHandle)
                        : Rectangle.Empty;
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                        bounds = GetVirtualScreenBounds();
                    return CaptureRectangle(bounds);
                }
                catch (Exception ex)
                {
                    _logService.Error(string.Format("窗口截图失败: {0}", ex.Message));
                    return null;
                }
            });
        }

        /// <summary>
        /// 固定区域截图（同步，用于快速重复识别）
        /// </summary>
        public Bitmap CaptureFixedArea(int x, int y, int width, int height)
        {
            return CaptureRectangle(new Rectangle(x, y, width, height));
        }

        public Rectangle[] GetDisplayBounds()
        {
            var screens = Screen.AllScreens;
            var bounds = new Rectangle[screens.Length];
            for (int i = 0; i < screens.Length; i++)
            {
                bounds[i] = screens[i].Bounds;
            }
            return bounds;
        }

        public void SetImageQuality(int quality) { }
        public void SetImageFormat(string format) { }

        /// <summary>
        /// GDI CopyFromScreen 截取指定矩形
        /// </summary>
        private Bitmap CaptureRectangle(Rectangle rect)
        {
            try
            {
                if (rect.Width <= 0 || rect.Height <= 0) return null;
                var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("截图失败 ({0}): {1}", rect, ex.Message));
                return null;
            }
        }

        private Rectangle GetVirtualScreenBounds()
        {
            return new Rectangle(
                SystemInformation.VirtualScreen.X,
                SystemInformation.VirtualScreen.Y,
                SystemInformation.VirtualScreen.Width,
                SystemInformation.VirtualScreen.Height);
        }

        private void OnScreenshotCompleted(Bitmap image, Rectangle region, bool success)
        {
            try
            {
                var handler = ScreenshotCompleted;
                if (handler != null)
                {
                    handler.Invoke(this, new ScreenshotEventArgs
                    {
                        Image = image,
                        Region = region,
                        Success = success,
                        Duration = TimeSpan.Zero
                    });
                }
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("触发截图完成事件失败: {0}", ex.Message));
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}

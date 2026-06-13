using System;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ocris.Services;
using Ocris.Utils;

namespace Ocris.Services
{
    /// <summary>
    /// 截图服务适配器
    /// </summary>
    public class ScreenshotAdapter : IScreenshotService, IAsyncInitializable, IDisposable
    {
        private readonly IScreenshotEngine _engine;
        private readonly ILogService _logService;
        private bool _disposed = false;

        public event EventHandler<ScreenshotResult> ScreenshotCompleted;
        public event EventHandler<ScreenshotResult> ScreenshotCaptured;

        public ScreenshotAdapter(IScreenshotEngine engine, ILogService logService)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");
            if (logService == null)
                throw new ArgumentNullException("logService");
                
            _engine = engine;
            _logService = logService;
            
            // 订阅截图引擎事件
            _engine.ScreenshotCompleted += OnEngineScreenshotCompleted;
        }

        /// <summary>
        /// 初始化截图引擎
        /// </summary>
        public async Task InitializeAsync()
        {
            await _logService.LogExecutionTimeAsync(async () =>
            {
                try
                {
                    _logService.Info("正在初始化截图引擎...");
                    await _engine.InitializeWithResultAsync();
                    _logService.Info("截图引擎初始化完成");
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "截图引擎初始化失败: {0}", ex.Message);
                    throw;
                }
            });
        }

        /// <summary>
        /// 全屏截图
        /// </summary>
        public async Task<ScreenshotResult> CaptureFullScreenAsync()
        {
            return await _logService.LogExecutionTimeAsync(async () =>
            {
                try
                {
                    _logService.Info("开始全屏截图...");

                    var bitmap = await _engine.CaptureFullScreenAsync();
                    var result = new ScreenshotResult
                    {
                        Image = bitmap,
                        CaptureRegion = GetVirtualScreenBounds(),
                        CaptureTime = DateTime.Now,
                        IsSuccess = true
                    };

                    _logService.Info("全屏截图完成");
                    OnScreenshotCompleted(result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "全屏截图失败: {0}", ex.Message);
                    
                    var result = new ScreenshotResult
                    {
                        CaptureTime = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };

                    OnScreenshotCompleted(result);
                    return result;
                }
            });
        }

        /// <summary>
        /// 区域截图
        /// </summary>
        public async Task<ScreenshotResult> CaptureRegionAsync(Rectangle region)
        {
            return await _logService.LogExecutionTimeAsync(async () =>
            {
                try
                {
                    _logService.Info("开始区域截图: {0}", region);

                    var bitmap = await _engine.CaptureRegionAsync(region);
                    var result = new ScreenshotResult
                    {
                        Image = bitmap,
                        CaptureRegion = region,
                        CaptureTime = DateTime.Now,
                        IsSuccess = true
                    };

                    _logService.Info("区域截图完成");
                    OnScreenshotCompleted(result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "区域截图失败: {0}", ex.Message);
                    
                    var result = new ScreenshotResult
                    {
                        CaptureRegion = region,
                        CaptureTime = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };

                    OnScreenshotCompleted(result);
                    return result;
                }
            });
        }

        /// <summary>
        /// 窗口截图
        /// </summary>
        public async Task<ScreenshotResult> CaptureWindowAsync(IntPtr windowHandle)
        {
            return await _logService.LogExecutionTimeAsync(async () =>
            {
                try
                {
                    _logService.Info("开始窗口截图: {0}", windowHandle);

                    var bitmap = await _engine.CaptureWindowAsync(windowHandle);
                    var result = new ScreenshotResult
                    {
                        Image = bitmap,
                        CaptureTime = DateTime.Now,
                        IsSuccess = true
                    };

                    _logService.Info("窗口截图完成");
                    OnScreenshotCompleted(result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "窗口截图失败: {0}", ex.Message);
                    
                    var result = new ScreenshotResult
                    {
                        CaptureTime = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };

                    OnScreenshotCompleted(result);
                    return result;
                }
            });
        }

        /// <summary>
        /// 区域截图（别名方法，兼容IScreenshotService接口）
        /// </summary>
        public async Task<ScreenshotResult> CaptureAreaAsync(Rectangle area)
        {
            return await CaptureRegionAsync(area);
        }

        /// <summary>
        /// 交互式区域选择截图
        /// </summary>
        public async Task<ScreenshotResult> CaptureInteractiveAsync()
        {
            return await _logService.LogExecutionTimeAsync(async () =>
            {
                try
                {
                    _logService.Info("开始交互式截图...");

                    // 使用TaskCompletionSource来等待截图引擎的事件回调
                    var tcs = new TaskCompletionSource<ScreenshotEventArgs>();
                    
                    EventHandler<ScreenshotEventArgs> tempHandler = (sender, e) =>
                    {
                        tcs.TrySetResult(e);
                    };
                    
                    // 临时订阅事件
                    _engine.ScreenshotCompleted += tempHandler;
                    
                    try
                    {
                        // 开始截图操作
                        var bitmapTask = _engine.CaptureInteractiveAsync();
                        
                        // 等待事件回调，获取区域信息
                        var eventArgs = await tcs.Task;
                        
                        // 等待截图完成
                        var bitmap = await bitmapTask;
                        
                        var result = new ScreenshotResult
                        {
                            Image = bitmap,
                            CaptureRegion = eventArgs.Region, // 使用从事件中获取的区域信息
                            CaptureTime = DateTime.Now,
                            IsSuccess = eventArgs.Success && bitmap != null,
                            ErrorMessage = !eventArgs.Success ? eventArgs.ErrorMessage : (bitmap == null ? "用户取消截图" : null)
                        };

                        _logService.Info("交互式截图完成，区域: {0}", eventArgs.Region);
                        
                        // 不需要再次触发OnScreenshotCompleted，因为事件已经处理过了
                        return result;
                    }
                    finally
                    {
                        // 取消临时订阅
                        _engine.ScreenshotCompleted -= tempHandler;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "交互式截图失败: {0}", ex.Message);
                    
                    var result = new ScreenshotResult
                    {
                        CaptureTime = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };

                    OnScreenshotCompleted(result);
                    return result;
                }
            });
        }

        /// <summary>
        /// 交互式区域选择截图
        /// </summary>
        public async Task<ScreenshotResult> CaptureInteractiveAreaAsync()
        {
            return await CaptureInteractiveAsync();
        }

        /// <summary>
        /// 获取显示器信息
        /// </summary>
        public Rectangle[] GetDisplayBounds()
        {
            try
            {
                return _engine.GetDisplayBounds();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取显示器信息失败: {0}", ex.Message);
                return new Rectangle[] { GetVirtualScreenBounds() };
            }
        }

        /// <summary>
        /// 获取所有屏幕边界（别名方法，兼容IScreenshotService接口）
        /// </summary>
        public Rectangle[] GetAllScreenBounds()
        {
            return GetDisplayBounds();
        }

        /// <summary>
        /// 获取主屏幕边界
        /// </summary>
        public Rectangle GetPrimaryScreenBounds()
        {
            try
            {
                return Screen.PrimaryScreen.Bounds;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取主屏幕边界失败: {0}", ex.Message);
                return GetVirtualScreenBounds();
            }
        }

        /// <summary>
        /// 获取虚拟屏幕边界
        /// </summary>
        private Rectangle GetVirtualScreenBounds()
        {
            return new Rectangle(
                SystemInformation.VirtualScreen.X,
                SystemInformation.VirtualScreen.Y,
                SystemInformation.VirtualScreen.Width,
                SystemInformation.VirtualScreen.Height
            );
        }

        /// <summary>
        /// 处理截图引擎截图完成事件
        /// </summary>
        private void OnEngineScreenshotCompleted(object sender, ScreenshotEventArgs e)
        {
            try
            {
                var result = new ScreenshotResult
                {
                    Image = e.Image,
                    CaptureRegion = e.Region,
                    CaptureTime = DateTime.Now,
                    IsSuccess = e.Success,
                    ErrorMessage = e.ErrorMessage
                };

                // 记录性能信息
                if (e.Duration.TotalMilliseconds > 1000)
                {
                    _logService.Warn("截图耗时较长: {0}ms", e.Duration.TotalMilliseconds);
                }

                OnScreenshotCompleted(result);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "处理截图事件失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 触发截图完成事件
        /// </summary>
        private void OnScreenshotCompleted(ScreenshotResult result)
        {
            try
            {
                if (ScreenshotCompleted != null)
                {
                    ScreenshotCompleted.Invoke(this, result);
                }
                if (ScreenshotCaptured != null)
                {
                    ScreenshotCaptured.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "触发截图完成事件失败: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // 取消订阅事件
                    if (_engine != null)
                    {
                        _engine.ScreenshotCompleted -= OnEngineScreenshotCompleted;
                    }

                    // 释放截图引擎资源
                    if (_engine != null)
                    {
                        _engine.Dispose();
                    }

                    if (_logService != null)
                    {
                        _logService.Info("截图适配器已释放资源");
                    }
                }
                catch (Exception ex)
                {
                    if (_logService != null)
                    {
                        _logService.Error(ex, "释放截图适配器资源时发生异常: {0}", ex.Message);
                    }
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}
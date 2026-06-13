using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Ocris.Services
{
    /// <summary>
    /// 截图引擎接口
    /// 定义截图功能与配置
    /// </summary>
    public interface IScreenshotEngine
    {
        /// <summary>
        /// 初始化截图引擎
        /// </summary>
        /// <returns>初始化是否成功</returns>
        Task<bool> InitializeWithResultAsync();

        /// <summary>
        /// 全屏截图
        /// </summary>
        /// <returns>截图结果</returns>
        Task<Bitmap> CaptureFullScreenAsync();

        /// <summary>
        /// 区域截图
        /// </summary>
        /// <param name="region">截图区域</param>
        /// <returns>截图结果</returns>
        Task<Bitmap> CaptureRegionAsync(Rectangle region);

        /// <summary>
        /// 窗口截图
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>截图结果</returns>
        Task<Bitmap> CaptureWindowAsync(IntPtr windowHandle);

        /// <summary>
        /// 交互式截图
        /// </summary>
        /// <returns>截图结果</returns>
        Task<Bitmap> CaptureInteractiveAsync();

        /// <summary>
        /// 固定区域截图（同步方法，用于快速重复截图）
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>截图结果</returns>
        Bitmap CaptureFixedArea(int x, int y, int width, int height);

        /// <summary>
        /// 获取所有显示器信息
        /// </summary>
        /// <returns>显示器信息数组</returns>
        Rectangle[] GetDisplayBounds();

        /// <summary>
        /// 设置截图质量
        /// </summary>
        /// <param name="quality">质量等级 (1-100)</param>
        void SetImageQuality(int quality);

        /// <summary>
        /// 设置截图格式
        /// </summary>
        /// <param name="format">图像格式</param>
        void SetImageFormat(string format);

        /// <summary>
        /// 释放资源
        /// </summary>
        void Dispose();

        /// <summary>
        /// 截图完成事件
        /// </summary>
        event EventHandler<ScreenshotEventArgs> ScreenshotCompleted;
    }

    /// <summary>
    /// 交互式截图结果
    /// </summary>
    public class InteractiveScreenshotResult
    {
        public Bitmap Image { get; set; }
        public Rectangle Region { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 截图事件参数
    /// </summary>
    public class ScreenshotEventArgs : EventArgs
    {
        public Bitmap Image { get; set; }
        public Rectangle Region { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 截图结果
    /// </summary>
    public class ScreenshotResult
    {
        /// <summary>
        /// 截图图像
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// 截图区域
        /// </summary>
        public Rectangle CaptureArea { get; set; }

        /// <summary>
        /// 截图区域（兼容性属性）
        /// </summary>
        public Rectangle CaptureRegion { get; set; }

        /// <summary>
        /// 截图时间
        /// </summary>
        public DateTime CaptureTime { get; set; }



        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 屏幕截图服务接口
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// 截取全屏
        /// </summary>
        /// <returns>截图结果</returns>
        Task<ScreenshotResult> CaptureFullScreenAsync();

        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="area">截图区域</param>
        /// <returns>截图结果</returns>
        Task<ScreenshotResult> CaptureAreaAsync(Rectangle area);

        /// <summary>
        /// 截取指定窗口
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        /// <returns>截图结果</returns>
        Task<ScreenshotResult> CaptureWindowAsync(IntPtr windowHandle);

        /// <summary>
        /// 交互式区域选择截图
        /// </summary>
        /// <returns>截图结果</returns>
        Task<ScreenshotResult> CaptureInteractiveAreaAsync();



        /// <summary>
        /// 获取主显示器信息
        /// </summary>
        /// <returns>显示器尺寸</returns>
        Rectangle GetPrimaryScreenBounds();

        /// <summary>
        /// 获取所有显示器信息
        /// </summary>
        /// <returns>显示器列表</returns>
        Rectangle[] GetAllScreenBounds();

        /// <summary>
        /// 截图完成事件
        /// </summary>
        event EventHandler<ScreenshotResult> ScreenshotCaptured;
    }
}
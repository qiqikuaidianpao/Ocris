using System;

namespace Ocris.Models
{
    /// <summary>
    /// 截图模式枚举 - 简化版本，类似微信截图
    /// </summary>
    public enum ScreenshotMode
    {
        /// <summary>
        /// 智能截图模式 - 默认模式，自动识别窗口并支持自由选择
        /// 鼠标移动时自动高亮窗口，点击可选择窗口或拖拽自由选择区域
        /// </summary>
        Smart
    }
    
    /// <summary>
    /// 截图模式扩展方法
    /// </summary>
    public static class ScreenshotModeExtensions
    {
        /// <summary>
        /// 获取截图模式的显示名称
        /// </summary>
        /// <param name="mode">截图模式</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(this ScreenshotMode mode)
        {
            switch (mode)
            {
                case ScreenshotMode.Smart:
                    return "智能截图";
                default:
                    return "智能截图";
            }
        }
        
        /// <summary>
        /// 获取截图模式的描述
        /// </summary>
        /// <param name="mode">截图模式</param>
        /// <returns>模式描述</returns>
        public static string GetDescription(this ScreenshotMode mode)
        {
            switch (mode)
            {
                case ScreenshotMode.Smart:
                    return "移动鼠标自动识别窗口，点击选择窗口或拖拽选择区域";
                default:
                    return "移动鼠标自动识别窗口，点击选择窗口或拖拽选择区域";
            }
        }
    }
}
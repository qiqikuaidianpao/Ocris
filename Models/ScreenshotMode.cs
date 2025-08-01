using System;

namespace AIAnswerTool.Models
{
    /// <summary>
    /// 截图模式枚举
    /// </summary>
    public enum ScreenshotMode
    {
        /// <summary>
        /// 自由选择模式 - 用户拖拽选择截图区域
        /// </summary>
        FreeSelection,
        
        /// <summary>
        /// 智能窗口模式 - 自动识别并选择窗口
        /// </summary>
        SmartWindow,
        
        /// <summary>
        /// 全屏截图模式 - 截取整个屏幕
        /// </summary>
        FullScreen,
        
        /// <summary>
        /// 延时截图模式 - 延时后自动截图
        /// </summary>
        DelayedCapture
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
                case ScreenshotMode.FreeSelection:
                    return "自由选择";
                case ScreenshotMode.SmartWindow:
                    return "智能窗口";
                case ScreenshotMode.FullScreen:
                    return "全屏截图";
                case ScreenshotMode.DelayedCapture:
                    return "延时截图";
                default:
                    return "未知模式";
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
                case ScreenshotMode.FreeSelection:
                    return "拖拽鼠标选择截图区域";
                case ScreenshotMode.SmartWindow:
                    return "单击选择窗口进行截图";
                case ScreenshotMode.FullScreen:
                    return "截取整个屏幕内容";
                case ScreenshotMode.DelayedCapture:
                    return "3秒延时后自动截图";
                default:
                    return "未知模式描述";
            }
        }
    }
}
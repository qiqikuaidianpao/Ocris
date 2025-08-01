using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 窗口检测服务接口，提供Windows API窗口检测功能
    /// </summary>
    public interface IWindowDetectionService
    {
        /// <summary>
        /// 获取鼠标位置下的窗口句柄
        /// </summary>
        /// <returns>窗口句柄，如果没有找到有效窗口则返回IntPtr.Zero</returns>
        IntPtr GetWindowUnderCursor();

        /// <summary>
        /// 获取指定窗口的边界矩形
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口边界矩形，如果获取失败则返回空矩形</returns>
        Rectangle GetWindowBounds(IntPtr hwnd);

        /// <summary>
        /// 获取窗口信息（类名和标题）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口信息，包含类名和标题</returns>
        WindowInfo GetWindowInfo(IntPtr hwnd);

        /// <summary>
        /// 验证窗口是否有效
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>如果窗口有效则返回true，否则返回false</returns>
        bool IsValidWindow(IntPtr hwnd);

        /// <summary>
        /// 获取当前鼠标位置
        /// </summary>
        /// <returns>鼠标位置坐标</returns>
        Point GetCursorPosition();
        
        /// <summary>
        /// 获取窗口的UI Automation元素
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>UI Automation元素</returns>
        AutomationElement GetAutomationElement(IntPtr hWnd);
        
        /// <summary>
        /// 获取指定点下的UI Automation元素
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>UI Automation元素</returns>
        AutomationElement GetElementUnderPoint(Point point);
        
        /// <summary>
        /// 获取UI Automation元素的边界
        /// </summary>
        /// <param name="element">UI Automation元素</param>
        /// <returns>元素边界矩形</returns>
        Rectangle GetElementBounds(AutomationElement element);
        
        /// <summary>
        /// 获取UI Automation元素信息
        /// </summary>
        /// <param name="element">UI Automation元素</param>
        /// <returns>元素信息</returns>
        ElementInfo GetElementInfo(AutomationElement element);
    }

    /// <summary>
    /// 窗口信息结构
    /// </summary>
    public class WindowInfo
    {
        /// <summary>
        /// 窗口类名
        /// </summary>
        public string ClassName { get; set; }
        
        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// 窗口边界
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// 窗口是否可见
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 窗口是否最小化
        /// </summary>
        public bool IsMinimized { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; }
    }
    
    /// <summary>
    /// UI Automation元素信息结构
    /// </summary>
    public class ElementInfo
    {
        /// <summary>
        /// 元素名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 元素类名
        /// </summary>
        public string ClassName { get; set; }
        
        /// <summary>
        /// 自动化ID
        /// </summary>
        public string AutomationId { get; set; }
        
        /// <summary>
        /// 控件类型
        /// </summary>
        public ControlType ControlType { get; set; }
        
        /// <summary>
        /// 元素边界
        /// </summary>
        public Rectangle Bounds { get; set; }
        
        /// <summary>
        /// 元素是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// 元素是否可见
        /// </summary>
        public bool IsVisible { get; set; }
    }
}
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using Ocris.Services;

namespace Ocris.Services
{
    /// <summary>
    /// 窗口检测服务实现，提供Windows API窗口检测功能
    /// </summary>
    public class WindowDetectionService : IWindowDetectionService
    {
        private readonly ILogService _logService;

        public WindowDetectionService(ILogService logService)
        {
            if (logService == null)
                throw new ArgumentNullException("logService");
            _logService = logService;
        }

        #region Windows API 声明

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("psapi.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const uint GW_OWNER = 4;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        /// <summary>
        /// 获取鼠标位置下的窗口句柄
        /// </summary>
        /// <returns>窗口句柄，如果没有找到有效窗口则返回IntPtr.Zero</returns>
        public IntPtr GetWindowUnderCursor()
        {
            return GetWindowUnderCursor(IntPtr.Zero);
        }

        /// <summary>
        /// 获取鼠标位置下的窗口句柄，可以排除指定窗口
        /// </summary>
        /// <param name="excludeWindow">要排除的窗口句柄</param>
        /// <returns>窗口句柄，如果没有找到有效窗口则返回IntPtr.Zero</returns>
        public IntPtr GetWindowUnderCursor(IntPtr excludeWindow)
        {
            try
            {
                POINT cursorPos;
                if (GetCursorPos(out cursorPos))
                {
                    IntPtr hwnd = WindowFromPoint(cursorPos);
                    
                    // 获取顶级窗口
                    IntPtr topLevelWindow = GetTopLevelWindow(hwnd);
                    
                    // 如果检测到的是要排除的窗口，尝试获取其下方的窗口
                    if (excludeWindow != IntPtr.Zero && topLevelWindow == excludeWindow)
                    {
                        // 临时设置窗口为透明，让鼠标事件穿透
                        var originalExStyle = GetWindowLong(excludeWindow, GWL_EXSTYLE);
                        SetWindowLong(excludeWindow, GWL_EXSTYLE, originalExStyle | WS_EX_TRANSPARENT);
                        
                        try
                        {
                            // 重新获取窗口
                            hwnd = WindowFromPoint(cursorPos);
                            topLevelWindow = GetTopLevelWindow(hwnd);
                        }
                        finally
                        {
                            // 恢复原始样式
                            SetWindowLong(excludeWindow, GWL_EXSTYLE, originalExStyle);
                        }
                    }
                    
                    if (IsValidWindow(topLevelWindow) && topLevelWindow != excludeWindow)
                    {
                        if (_logService != null)
                            _logService.Info(string.Format("Found window under cursor: {0}", topLevelWindow));
                        return topLevelWindow;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting window under cursor: {0}", ex.Message));
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 获取指定窗口的边界矩形
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口边界矩形，如果获取失败则返回空矩形</returns>
        public Rectangle GetWindowBounds(IntPtr hwnd)
        {
            try
            {
                RECT rect;
                if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out rect))
                {
                    return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                }
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting window bounds for {0}: {1}", hwnd, ex.Message));
            }

            return Rectangle.Empty;
        }

        /// <summary>
        /// 获取指定窗口的客户区域边界矩形（不包含标题栏和边框）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口客户区域边界矩形，如果获取失败则返回空矩形</returns>
        public Rectangle GetWindowClientBounds(IntPtr hwnd)
        {
            try
            {
                RECT clientRect;
                if (hwnd != IntPtr.Zero && GetClientRect(hwnd, out clientRect))
                {
                    // 将客户区域坐标转换为屏幕坐标
                    POINT topLeft = new POINT { X = clientRect.Left, Y = clientRect.Top };
                    POINT bottomRight = new POINT { X = clientRect.Right, Y = clientRect.Bottom };
                    
                    if (ClientToScreen(hwnd, ref topLeft) && ClientToScreen(hwnd, ref bottomRight))
                    {
                        return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting window client bounds for {0}: {1}", hwnd, ex.Message));
            }

            return Rectangle.Empty;
        }

        /// <summary>
        /// 获取窗口信息（类名和标题）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口信息，包含类名和标题</returns>
        public WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            var windowInfo = new WindowInfo
            {
                Handle = hwnd,
                Bounds = GetWindowBounds(hwnd),
                IsVisible = IsWindowVisible(hwnd),
                IsMinimized = IsIconic(hwnd),
                ProcessName = string.Empty
            };

            try
            {
                if (hwnd != IntPtr.Zero)
                {
                    // 获取窗口类名
                    var className = new StringBuilder(256);
                    if (GetClassName(hwnd, className, className.Capacity) > 0)
                    {
                        windowInfo.ClassName = className.ToString();
                    }

                    // 获取窗口标题
                    var windowTitle = new StringBuilder(256);
                    if (GetWindowText(hwnd, windowTitle, windowTitle.Capacity) > 0)
                    {
                        windowInfo.Title = windowTitle.ToString();
                    }

                    // 获取进程名
                    uint processId;
                    if (GetWindowThreadProcessId(hwnd, out processId) != 0)
                    {
                        IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
                        if (processHandle != IntPtr.Zero)
                        {
                            var processName = new StringBuilder(256);
                            if (GetModuleBaseName(processHandle, IntPtr.Zero, processName, (uint)processName.Capacity) > 0)
                            {
                                windowInfo.ProcessName = processName.ToString();
                            }
                            CloseHandle(processHandle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting window info for {0}: {1}", hwnd, ex.Message));
            }

            return windowInfo;
        }

        /// <summary>
        /// 验证窗口是否有效
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>如果窗口有效则返回true，否则返回false</returns>
        public bool IsValidWindow(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero)
                    return false;

                // 检查窗口是否存在
                if (!IsWindow(hwnd))
                    return false;

                // 检查窗口是否可见
                if (!IsWindowVisible(hwnd))
                    return false;

                // 获取窗口边界，确保窗口有有效的尺寸
                var bounds = GetWindowBounds(hwnd);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error validating window {0}: {1}", hwnd, ex.Message));
                return false;
            }
        }

        public Point GetCursorPosition()
        {
            try
            {
                POINT cursorPos;
                if (GetCursorPos(out cursorPos))
                {
                    return new Point(cursorPos.X, cursorPos.Y);
                }
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting cursor position: {0}", ex.Message));
            }

            return Point.Empty;
        }

        /// <summary>
        /// 获取顶级窗口句柄
        /// </summary>
        /// <param name="hwnd">子窗口句柄</param>
        /// <returns>顶级窗口句柄</returns>
        private IntPtr GetTopLevelWindow(IntPtr hwnd)
        {
            try
            {
                IntPtr current = hwnd;
                IntPtr parent;

                // 向上遍历窗口层次结构，找到顶级窗口
                while ((parent = GetParent(current)) != IntPtr.Zero)
                {
                    current = parent;
                }

                // 检查是否有拥有者窗口
                IntPtr owner = GetWindow(current, GW_OWNER);
                if (owner != IntPtr.Zero && IsWindowVisible(owner))
                {
                    current = owner;
                }

                return current;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("Error getting top level window for {0}: {1}", hwnd, ex.Message));
                return hwnd;
            }
        }

        #region UI Automation Methods

        /// <summary>
        /// 获取窗口的UI Automation元素
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>UI Automation元素</returns>
        public AutomationElement GetAutomationElement(IntPtr hWnd)
        {
            try
            {
                if (hWnd == IntPtr.Zero)
                {
                    if (_logService != null)
                        _logService.Warn("窗口句柄为空，无法获取UI Automation元素");
                    return null;
                }

                var element = AutomationElement.FromHandle(hWnd);
                if (_logService != null)
                    _logService.Info(string.Format("成功获取窗口 {0} 的UI Automation元素", hWnd));
                return element;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("获取UI Automation元素失败: {0}", ex.Message));
                return null;
            }
        }

        /// <summary>
        /// 获取指定点下的UI Automation元素
        /// </summary>
        /// <param name="point">屏幕坐标点</param>
        /// <returns>UI Automation元素</returns>
        public AutomationElement GetElementUnderPoint(Point point)
        {
            try
            {
                var systemPoint = new System.Windows.Point(point.X, point.Y);
                var element = AutomationElement.FromPoint(systemPoint);
                if (_logService != null)
                    _logService.Info(string.Format("成功获取坐标 ({0}, {1}) 下的UI Automation元素", point.X, point.Y));
                return element;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("获取指定点下的UI Automation元素失败: {0}", ex.Message));
                return null;
            }
        }

        /// <summary>
        /// 获取UI Automation元素的边界
        /// </summary>
        /// <param name="element">UI Automation元素</param>
        /// <returns>元素边界矩形</returns>
        public Rectangle GetElementBounds(AutomationElement element)
        {
            try
            {
                if (element == null)
                {
                    if (_logService != null)
                        _logService.Warn("UI Automation元素为空，无法获取边界");
                    return Rectangle.Empty;
                }

                var boundingRect = element.Current.BoundingRectangle;
                var rectangle = new Rectangle(
                    (int)boundingRect.X,
                    (int)boundingRect.Y,
                    (int)boundingRect.Width,
                    (int)boundingRect.Height
                );

                if (_logService != null)
                    _logService.Info(string.Format("成功获取UI Automation元素边界: {0}", rectangle));
                return rectangle;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("获取UI Automation元素边界失败: {0}", ex.Message));
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// 获取UI Automation元素信息
        /// </summary>
        /// <param name="element">UI Automation元素</param>
        /// <returns>元素信息</returns>
        public ElementInfo GetElementInfo(AutomationElement element)
        {
            try
            {
                if (element == null)
                {
                    if (_logService != null)
                        _logService.Warn("UI Automation元素为空，无法获取信息");
                    return new ElementInfo
                    {
                        Name = string.Empty,
                        ClassName = string.Empty,
                        AutomationId = string.Empty
                    };
                }

                var current = element.Current;
                var bounds = GetElementBounds(element);

                var elementInfo = new ElementInfo
                {
                    Name = current.Name ?? string.Empty,
                    ClassName = current.ClassName ?? string.Empty,
                    AutomationId = current.AutomationId ?? string.Empty,
                    ControlType = current.ControlType,
                    Bounds = bounds,
                    IsEnabled = current.IsEnabled,
                    IsVisible = !current.IsOffscreen
                };

                if (_logService != null)
                    _logService.Info(string.Format("成功获取UI Automation元素信息: {0} ({1})", elementInfo.Name, elementInfo.ControlType));
                return elementInfo;
            }
            catch (Exception ex)
            {
                if (_logService != null)
                    _logService.Error(string.Format("获取UI Automation元素信息失败: {0}", ex.Message));
                    return new ElementInfo
                    {
                        Name = string.Empty,
                        ClassName = string.Empty,
                        AutomationId = string.Empty
                    };
            }
        }

        #endregion
    }
}
using System;
using System.Drawing;
using System.Threading;
using Ocris.Services;

namespace Ocris.Tests
{
    /// <summary>
    /// WindowDetectionService 单元测试类
    /// </summary>
    public class WindowDetectionServiceTests
    {
        private readonly IWindowDetectionService _windowDetectionService;
        private readonly ILogService _logService;

        public WindowDetectionServiceTests()
        {
            _logService = new LogService();
            _windowDetectionService = new WindowDetectionService(_logService);
        }

        /// <summary>
        /// 测试获取鼠标位置功能
        /// </summary>
        /// <returns>测试是否通过</returns>
        public bool TestGetCursorPosition()
        {
            try
            {
                var cursorPos = _windowDetectionService.GetCursorPosition();
                
                // 验证鼠标位置是否有效（不为空点且在合理范围内）
                bool isValid = !cursorPos.IsEmpty && 
                              cursorPos.X >= 0 && cursorPos.Y >= 0 &&
                              cursorPos.X <= 10000 && cursorPos.Y <= 10000; // 假设最大屏幕尺寸
                
                _logService.Info(string.Format("Cursor position test: {0}, Valid: {1}", cursorPos, isValid));
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("TestGetCursorPosition failed: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 测试获取鼠标下窗口功能
        /// </summary>
        /// <returns>测试是否通过</returns>
        public bool TestGetWindowUnderCursor()
        {
            try
            {
                var hwnd = _windowDetectionService.GetWindowUnderCursor();
                
                // 验证是否获取到有效的窗口句柄
                bool isValid = hwnd != IntPtr.Zero;
                
                _logService.Info(string.Format("Window under cursor test: {0}, Valid: {1}", hwnd, isValid));
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("TestGetWindowUnderCursor failed: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 测试获取窗口边界功能
        /// </summary>
        /// <returns>测试是否通过</returns>
        public bool TestGetWindowBounds()
        {
            try
            {
                var hwnd = _windowDetectionService.GetWindowUnderCursor();
                if (hwnd == IntPtr.Zero)
                {
                    _logService.Warn("No window found under cursor for bounds test");
                    return false;
                }

                var bounds = _windowDetectionService.GetWindowBounds(hwnd);
                
                // 验证边界是否有效
                bool isValid = !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0;
                
                _logService.Info(string.Format("Window bounds test: {0}, Valid: {1}", bounds, isValid));
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("TestGetWindowBounds failed: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 测试获取窗口信息功能
        /// </summary>
        /// <returns>测试是否通过</returns>
        public bool TestGetWindowInfo()
        {
            try
            {
                var hwnd = _windowDetectionService.GetWindowUnderCursor();
                if (hwnd == IntPtr.Zero)
                {
                    _logService.Warn("No window found under cursor for info test");
                    return false;
                }

                var windowInfo = _windowDetectionService.GetWindowInfo(hwnd);
                
                // 验证窗口信息是否有效
                bool isValid = windowInfo != null && 
                              windowInfo.Handle == hwnd &&
                              !string.IsNullOrEmpty(windowInfo.ClassName);
                
                _logService.Info(string.Format("Window info test: Handle={0}, ClassName={1}, Title={2}, Valid: {3}", windowInfo != null ? windowInfo.Handle : IntPtr.Zero, windowInfo != null ? windowInfo.ClassName : "", windowInfo != null ? windowInfo.Title : "", isValid));
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("TestGetWindowInfo failed: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 测试窗口有效性验证功能
        /// </summary>
        /// <returns>测试是否通过</returns>
        public bool TestIsValidWindow()
        {
            try
            {
                // 测试无效窗口句柄
                bool invalidTest = !_windowDetectionService.IsValidWindow(IntPtr.Zero);
                
                // 测试有效窗口句柄
                var hwnd = _windowDetectionService.GetWindowUnderCursor();
                bool validTest = hwnd != IntPtr.Zero ? _windowDetectionService.IsValidWindow(hwnd) : true;
                
                bool isValid = invalidTest && validTest;
                
                _logService.Info(string.Format("Window validation test: Invalid={0}, Valid={1}, Overall: {2}", invalidTest, validTest, isValid));
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(string.Format("TestIsValidWindow failed: {0}", ex.Message));
                return false;
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        /// <returns>所有测试是否都通过</returns>
        public bool RunAllTests()
        {
            _logService.Info("Starting WindowDetectionService tests...");
            
            bool test1 = TestGetCursorPosition();
            Thread.Sleep(100); // 短暂延迟
            
            bool test2 = TestGetWindowUnderCursor();
            Thread.Sleep(100);
            
            bool test3 = TestGetWindowBounds();
            Thread.Sleep(100);
            
            bool test4 = TestGetWindowInfo();
            Thread.Sleep(100);
            
            bool test5 = TestIsValidWindow();
            
            bool allPassed = test1 && test2 && test3 && test4 && test5;
            
            _logService.Info(string.Format("WindowDetectionService tests completed. All passed: {0}", allPassed));
            _logService.Info(string.Format("Individual results: CursorPos={0}, WindowUnderCursor={1}, WindowBounds={2}, WindowInfo={3}, ValidWindow={4}", test1, test2, test3, test4, test5));
            
            return allPassed;
        }
    }
}
using System;
using System.Drawing;
using System.Windows.Automation;
using AIAnswerTool.Services;

namespace AIAnswerTool.Tests
{
    /// <summary>
    /// UI Automation功能测试类
    /// </summary>
    public class UIAutomationTests
    {
        private readonly WindowDetectionService _windowDetectionService;
        private readonly ILogService _logService;

        public UIAutomationTests()
        {
            // 创建模拟的日志服务
            _logService = new LogService();
            _windowDetectionService = new WindowDetectionService(_logService);
        }

        /// <summary>
        /// 测试获取窗口的UI Automation元素
        /// </summary>
        public void TestGetAutomationElement()
        {
            try
            {
                Console.WriteLine("=== 测试获取UI Automation元素 ===");
                
                // 获取当前鼠标下的窗口
                var hWnd = _windowDetectionService.GetWindowUnderCursor();
                if (hWnd != IntPtr.Zero)
                {
                    var element = _windowDetectionService.GetAutomationElement(hWnd);
                    if (element != null)
                    {
                        Console.WriteLine($"成功获取UI Automation元素: {element.Current.Name}");
                        Console.WriteLine($"控件类型: {element.Current.ControlType.ProgrammaticName}");
                        Console.WriteLine($"类名: {element.Current.ClassName}");
                    }
                    else
                    {
                        Console.WriteLine("未能获取UI Automation元素");
                    }
                }
                else
                {
                    Console.WriteLine("未能获取窗口句柄");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试获取指定点下的UI Automation元素
        /// </summary>
        public void TestGetElementUnderPoint()
        {
            try
            {
                Console.WriteLine("\n=== 测试获取指定点下的UI Automation元素 ===");
                
                var cursorPos = _windowDetectionService.GetCursorPosition();
                Console.WriteLine($"当前鼠标位置: ({cursorPos.X}, {cursorPos.Y})");
                
                var element = _windowDetectionService.GetElementUnderPoint(cursorPos);
                if (element != null)
                {
                    var elementInfo = _windowDetectionService.GetElementInfo(element);
                    Console.WriteLine($"元素名称: {elementInfo.Name}");
                    Console.WriteLine($"控件类型: {elementInfo.ControlType.ProgrammaticName}");
                    Console.WriteLine($"类名: {elementInfo.ClassName}");
                    Console.WriteLine($"自动化ID: {elementInfo.AutomationId}");
                    Console.WriteLine($"边界: {elementInfo.Bounds}");
                    Console.WriteLine($"是否启用: {elementInfo.IsEnabled}");
                    Console.WriteLine($"是否可见: {elementInfo.IsVisible}");
                }
                else
                {
                    Console.WriteLine("未能获取指定点下的UI Automation元素");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试获取UI Automation元素边界
        /// </summary>
        public void TestGetElementBounds()
        {
            try
            {
                Console.WriteLine("\n=== 测试获取UI Automation元素边界 ===");
                
                var cursorPos = _windowDetectionService.GetCursorPosition();
                var element = _windowDetectionService.GetElementUnderPoint(cursorPos);
                
                if (element != null)
                {
                    var bounds = _windowDetectionService.GetElementBounds(element);
                    Console.WriteLine($"元素边界: X={bounds.X}, Y={bounds.Y}, Width={bounds.Width}, Height={bounds.Height}");
                    
                    // 验证边界是否合理
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        Console.WriteLine("边界数据有效");
                    }
                    else
                    {
                        Console.WriteLine("警告: 边界数据可能无效");
                    }
                }
                else
                {
                    Console.WriteLine("未能获取UI Automation元素");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试层次化检测（窗口 -> 控件）
        /// </summary>
        public void TestHierarchicalDetection()
        {
            try
            {
                Console.WriteLine("\n=== 测试层次化检测 ===");
                
                var cursorPos = _windowDetectionService.GetCursorPosition();
                
                // 1. 先获取窗口信息
                var hWnd = _windowDetectionService.GetWindowUnderCursor();
                if (hWnd != IntPtr.Zero)
                {
                    var windowInfo = _windowDetectionService.GetWindowInfo(hWnd);
                    Console.WriteLine($"窗口信息: {windowInfo.Title} ({windowInfo.ClassName})");
                    Console.WriteLine($"窗口边界: {windowInfo.Bounds}");
                    
                    // 2. 再获取控件信息
                    var element = _windowDetectionService.GetElementUnderPoint(cursorPos);
                    if (element != null)
                    {
                        var elementInfo = _windowDetectionService.GetElementInfo(element);
                        Console.WriteLine($"控件信息: {elementInfo.Name} ({elementInfo.ControlType.ProgrammaticName})");
                        Console.WriteLine($"控件边界: {elementInfo.Bounds}");
                        
                        // 3. 验证控件是否在窗口内
                        if (windowInfo.Bounds.Contains(elementInfo.Bounds.Location))
                        {
                            Console.WriteLine("✓ 控件位置验证通过");
                        }
                        else
                        {
                            Console.WriteLine("⚠ 控件位置验证失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有UI Automation测试
        /// </summary>
        public void RunAllUIAutomationTests()
        {
            Console.WriteLine("开始运行UI Automation功能测试...");
            Console.WriteLine("请将鼠标移动到要测试的UI元素上，然后按任意键开始测试");
            Console.ReadKey();
            
            TestGetAutomationElement();
            TestGetElementUnderPoint();
            TestGetElementBounds();
            TestHierarchicalDetection();
            
            Console.WriteLine("\n=== UI Automation测试完成 ===");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// UI Automation测试程序入口
    /// </summary>
    public class UIAutomationTestProgram
    {
        public static void Main(string[] args)
        {
            var tests = new UIAutomationTests();
            tests.RunAllUIAutomationTests();
        }
    }
}
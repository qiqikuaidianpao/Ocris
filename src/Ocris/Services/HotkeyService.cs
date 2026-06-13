using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using NHotkey;
using NHotkey.Wpf;

namespace Ocris.Services
{
    /// <summary>
    /// 热键服务实现 - 使用NHotkey库提供真正的全局热键功能
    /// </summary>
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly Dictionary<string, bool> _registeredHotkeys = new Dictionary<string, bool>();
        private bool _disposed = false;

        public event Action<string> HotkeyPressed;
        public event Action<string> HotkeyRegistrationFailed;

        public HotkeyService(ILogService logService)
        {
            if (logService == null) throw new ArgumentNullException("logService");
            _logService = logService;
            _logService.Info("NHotkey服务初始化");
        }

        public void RegisterHotkey(string key, string actionName)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(actionName))
            {
                _logService.Error("Key and actionName cannot be null or empty");
                return;
            }

            _logService.Info("简单热键注册请求: {0} -> {1}", key, actionName);
        }

        public bool RegisterHotkey(string id, string name, ModifierKeys modifiers, Key key, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                _logService.Error("热键ID和名称不能为空");
                return false;
            }

            try
            {
                _logService.Info("尝试注册热键: {0} ({1}) -> {2}+{3}", id, name, modifiers, key);

                // 使用NHotkey注册全局热键
                HotkeyManager.Current.AddOrReplace(id, key, modifiers, (sender, e) => OnHotkeyTriggered(id));

                _registeredHotkeys[id] = true;
                _logService.Info("热键注册成功: {0} ({1})", id, name);
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键注册失败: {0} - {1}", name, ex.Message);
                if (HotkeyRegistrationFailed != null)
                    HotkeyRegistrationFailed.Invoke(string.Format("注册热键{0}失败: {1}", name, ex.Message));
                return false;
            }
        }

        private void OnHotkeyTriggered(string hotkeyId)
        {
            try
            {
                _logService.Info("热键触发: {0}", hotkeyId);
                if (HotkeyPressed != null)
                    HotkeyPressed.Invoke(hotkeyId);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键处理异常: {0}", ex.Message);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// 检测热键组合是否可用（通过 Win32 RegisterHotKey 试探，真实冲突检测）
        /// </summary>
        public bool IsHotkeyAvailable(ModifierKeys modifiers, Key key)
        {
            const int testId = 0x7FFF;
            var vk = KeyInterop.VirtualKeyFromKey(key);
            try
            {
                if (RegisterHotKey(IntPtr.Zero, testId, (uint)modifiers, (uint)vk))
                {
                    UnregisterHotKey(IntPtr.Zero, testId);
                    return true;
                }
                _logService.Warn("热键组合 {0}+{1} 已被其他程序占用", modifiers, key);
                return false;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "检测热键可用性异常: {0}", ex.Message);
                return false;
            }
        }

        public bool IsHotkeyActive(string id)
        {
            return _registeredHotkeys.ContainsKey(id) && _registeredHotkeys[id];
        }

        public void UnregisterHotkey(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                _logService.Error("ActionName cannot be null or empty");
                return;
            }

            try
            {
                HotkeyManager.Current.Remove(actionName);
                _registeredHotkeys.Remove(actionName);
                _logService.Info("热键注销成功: {0}", actionName);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "热键注销失败: {0} - {1}", actionName, ex.Message);
            }
        }

        public void UnregisterAll()
        {
            try
            {
                foreach (var hotkeyId in _registeredHotkeys.Keys)
                {
                    HotkeyManager.Current.Remove(hotkeyId);
                }
                _registeredHotkeys.Clear();
                _logService.Info("所有热键已注销");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "注销所有热键失败: {0}", ex.Message);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAll();
                _logService.Info("HotkeyService disposed");
                _disposed = true;
            }
        }
    }
}

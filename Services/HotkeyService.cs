using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using AIAnswerTool.Models;
using AIAnswerTool.Utils;

namespace AIAnswerTool.Services
{
    public class HotkeyService : IHotkeyService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly Dictionary<string, Models.HotkeyInfo> _registeredHotkeys;
        private readonly object _lockObject = new object();
        private bool _isServiceEnabled = true;
        private bool _disposed = false;

        public event EventHandler<Models.HotkeyEventArgs> HotkeyPressed;
        public event EventHandler<string> HotkeyRegistrationFailed;

        public HotkeyService(ILogService logService)
        {
            if (logService == null) throw new ArgumentNullException("logService");
            _logService = logService;
            _registeredHotkeys = new Dictionary<string, Models.HotkeyInfo>();
            _logService.LogInfo("HotkeyService initialized");
        }

        public bool RegisterHotkey(string id, string name, ModifierKeys modifiers, Key key, string description = "")
        {
            var hotkeyInfo = Models.HotkeyInfo.Create(id, name, modifiers, key, description);
            return RegisterHotkey(hotkeyInfo);
        }

        public bool RegisterHotkey(Models.HotkeyInfo hotkey)
        {
            if (hotkey == null || string.IsNullOrWhiteSpace(hotkey.Id))
            {
                _logService.LogError("Hotkey or Hotkey ID cannot be null or empty");
                return false;
            }

            if (!_isServiceEnabled)
            {
                _logService.LogWarn("HotkeyService is disabled, cannot register hotkey: {0}", hotkey.Id);
                return false;
            }

            lock (_lockObject)
            {
                try
                {
                    if (_registeredHotkeys.ContainsKey(hotkey.Id))
                    {
                        _logService.LogWarn("Hotkey with ID '{0}' is already registered", hotkey.Id);
                        return false;
                    }

                    if (IsHotkeyAvailable(hotkey.Modifiers, hotkey.Key) == false)
                    {
                        var errorMsg = string.Format("Hotkey conflict detected. '{0}' is already in use.", hotkey.ToString());
                        _logService.LogError(errorMsg);
                        OnHotkeyRegistrationFailed(errorMsg);
                        return false;
                    }

                    HotkeyManager.Current.AddOrReplace(hotkey.Id, hotkey.Key, hotkey.Modifiers, OnHotkeyPressed);
                    hotkey.IsRegistered = true;
                    _registeredHotkeys[hotkey.Id] = hotkey;
                    _logService.LogInfo("Successfully registered hotkey: {0} ({1})", hotkey.Id, hotkey.ToString());
                    return true;
                }
                catch (Exception ex)
                {
                    var errorMsg = string.Format("Failed to register hotkey '{0}' ({1}): {2}", hotkey.Id, hotkey.ToString(), ex.Message);
                    _logService.LogError(errorMsg, ex);
                    OnHotkeyRegistrationFailed(errorMsg);
                    return false;
                }
            }
        }

        public bool UnregisterHotkey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logService.LogError("Hotkey ID cannot be null or empty");
                return false;
            }

            lock (_lockObject)
            {
                try
                {
                    Models.HotkeyInfo hotkeyInfo;
                    if (!_registeredHotkeys.TryGetValue(id, out hotkeyInfo))
                    {
                        _logService.LogWarn("Hotkey with ID '{0}' is not registered", id);
                        return false;
                    }

                    if (!hotkeyInfo.IsRegistered)
                    {
                        _logService.LogWarn("Hotkey '{0}' is already unregistered", id);
                        return false;
                    }

                    HotkeyManager.Current.Remove(id);
                    hotkeyInfo.IsRegistered = false;
                    _logService.LogInfo("Successfully unregistered hotkey: {0}", id);
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.LogError("Failed to unregister hotkey '{0}': {1}", id, ex.Message, ex);
                    return false;
                }
            }
        }

        public void UnregisterAllHotkeys()
        {
            lock (_lockObject)
            {
                var hotkeyIds = _registeredHotkeys.Keys.ToList();
                foreach (var id in hotkeyIds)
                {
                    UnregisterHotkey(id);
                }
            }
        }

        public bool IsHotkeyRegistered(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            lock (_lockObject)
            {
                Models.HotkeyInfo hotkeyInfo;
                return _registeredHotkeys.TryGetValue(id, out hotkeyInfo) && hotkeyInfo.IsRegistered;
            }
        }

        public bool IsHotkeyAvailable(ModifierKeys modifiers, Key key)
        {
            lock (_lockObject)
            {
                return !_registeredHotkeys.Values.Any(h => h.IsRegistered && h.Modifiers == modifiers && h.Key == key);
            }
        }

        /// <summary>
        /// 检查热键是否处于活动状态（已注册且在系统中有效）
        /// </summary>
        /// <param name="hotkeyName">热键名称或ID</param>
        /// <returns>热键是否活动</returns>
        public bool IsHotkeyActive(string hotkeyName)
        {
            if (string.IsNullOrWhiteSpace(hotkeyName))
                return false;

            lock (_lockObject)
            {
                Models.HotkeyInfo hotkeyInfo;
                if (_registeredHotkeys.TryGetValue(hotkeyName, out hotkeyInfo))
                {
                    // 检查是否已注册且服务已启用
                    bool isActive = hotkeyInfo.IsRegistered && _isServiceEnabled;
                    _logService.LogDebug("Hotkey '{0}' active status: {1}", hotkeyName, isActive);
                    return isActive;
                }
                _logService.LogDebug("Hotkey '{0}' not found in registered hotkeys", hotkeyName);
                return false;
            }
        }

        public Models.HotkeyInfo GetHotkey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            lock (_lockObject)
            {
                Models.HotkeyInfo hotkeyInfo;
                _registeredHotkeys.TryGetValue(id, out hotkeyInfo);
                return hotkeyInfo;
            }
        }

        public Models.HotkeyInfo[] GetAllHotkeys()
        {
            lock (_lockObject)
            {
                return _registeredHotkeys.Values.ToArray();
            }
        }

        public bool UpdateHotkey(string id, ModifierKeys modifiers, Key key)
        {
            lock (_lockObject)
            {
                Models.HotkeyInfo hotkeyInfo;
                if (!_registeredHotkeys.TryGetValue(id, out hotkeyInfo))
                {
                    _logService.LogWarn("Cannot update non-existent hotkey with ID '{0}'", id);
                    return false;
                }

                if (UnregisterHotkey(id))
                {
                    hotkeyInfo.Modifiers = modifiers;
                    hotkeyInfo.Key = key;
                    return RegisterHotkey(hotkeyInfo);
                }
                return false;
            }
        }

        public void Enable()
        {
            lock (_lockObject)
            {
                if (_isServiceEnabled)
                {
                    _logService.LogInfo("HotkeyService is already enabled");
                    return;
                }

                _isServiceEnabled = true;
                _logService.LogInfo("HotkeyService enabled");

                var hotkeysToReregister = _registeredHotkeys.Values.Where(h => !h.IsRegistered).ToList();
                foreach (var hotkeyInfo in hotkeysToReregister)
                {
                    RegisterHotkey(hotkeyInfo);
                }
            }
        }

        public void Disable()
        {
            lock (_lockObject)
            {
                if (!_isServiceEnabled)
                {
                    _logService.LogInfo("HotkeyService is already disabled");
                    return;
                }

                UnregisterAllHotkeys();
                _isServiceEnabled = false;
                _logService.LogInfo("HotkeyService disabled");
            }
        }

        public bool IsEnabled
        {
            get { return _isServiceEnabled; }
        }

        private void OnHotkeyPressed(object sender, NHotkey.HotkeyEventArgs e)
        {
            if (!_isServiceEnabled)
            {
                _logService.LogWarn("Hotkey service is disabled, ignoring hotkey press: {0}", e.Name);
                return;
            }

            lock (_lockObject)
            {
                Models.HotkeyInfo hotkeyInfo;
                if (_registeredHotkeys.TryGetValue(e.Name, out hotkeyInfo))
                {
                    _logService.LogInfo("Hotkey pressed: {0} ({1})", e.Name, hotkeyInfo.HotkeyString);
                    
                    // 记录触发信息
                    hotkeyInfo.RecordTrigger();
                    
                    try
                    {
                        if (HotkeyPressed != null)
                        {
                            var eventArgs = new Models.HotkeyEventArgs(hotkeyInfo);
                            HotkeyPressed(this, eventArgs);
                            _logService.LogDebug("Hotkey event '{0}' handled successfully", e.Name);
                        }
                        else
                        {
                            _logService.LogWarn("No handlers registered for hotkey event: {0}", e.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError("Error handling hotkey event '{0}': {1}", e.Name, ex.Message, ex);
                    }
                }
                else
                {
                    _logService.LogWarn("Received trigger for unregistered hotkey: {0}", e.Name);
                }
            }
        }

        private void OnHotkeyRegistrationFailed(string reason)
        {
            if (HotkeyRegistrationFailed != null)
            {
                HotkeyRegistrationFailed(this, reason);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Disable();
                lock (_lockObject)
                {
                    _registeredHotkeys.Clear();
                }
                _logService.LogInfo("HotkeyService disposed");
            }

            _disposed = true;
        }
    }
}
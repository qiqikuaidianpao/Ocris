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
        private readonly Dictionary<string, HotkeyInfo> _registeredHotkeys;
        private readonly object _lockObject = new object();
        private bool _isServiceEnabled = true;
        private bool _disposed = false;

        public event EventHandler<HotkeyEventArgs> HotkeyPressed;
        public event EventHandler<string> HotkeyRegistrationFailed;

        public HotkeyService(ILogService logService)
        {
            if (logService == null) throw new ArgumentNullException("logService");
            _logService = logService;
            _registeredHotkeys = new Dictionary<string, HotkeyInfo>();
            _logService.LogInfo("HotkeyService initialized");
        }

        public bool RegisterHotkey(string id, string name, ModifierKeys modifiers, Key key, string description = "")
        {
            var hotkeyInfo = new HotkeyInfo
            {
                Id = id,
                Name = name,
                Modifiers = modifiers,
                Key = key,
                Description = description
            };
            return RegisterHotkey(hotkeyInfo);
        }

        public bool RegisterHotkey(HotkeyInfo hotkey)
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
                    HotkeyInfo hotkeyInfo;
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
                HotkeyInfo hotkeyInfo;
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

        public HotkeyInfo GetHotkey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            lock (_lockObject)
            {
                HotkeyInfo hotkeyInfo;
                _registeredHotkeys.TryGetValue(id, out hotkeyInfo);
                return hotkeyInfo;
            }
        }

        public HotkeyInfo[] GetAllHotkeys()
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
                HotkeyInfo hotkeyInfo;
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
            if (!_isServiceEnabled) return;

            lock (_lockObject)
            {
                HotkeyInfo hotkeyInfo;
                if (_registeredHotkeys.TryGetValue(e.Name, out hotkeyInfo))
                {
                    _logService.LogDebug("Hotkey triggered: {0}", e.Name);
                    if (HotkeyPressed != null)
                    {
                        HotkeyPressed(this, new HotkeyEventArgs(hotkeyInfo));
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
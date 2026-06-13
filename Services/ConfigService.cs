using System;
using System.IO;
using System.Threading.Tasks;
using Ocris.Models;
using Ocris.Services;
using Ocris.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ocris.Services
{
    /// <summary>
    /// 配置管理服务实现
    /// </summary>
    public class ConfigService : IConfigService, IAsyncInitializable
    {
        private readonly ILogService _logService;
        private ConfigModel _currentConfig;
        private readonly string _configFilePath;
        private readonly object _lockObject = new object();
        private FileSystemWatcher _fileWatcher;

        public ConfigService(ILogService logService)
        {
            _logService = logService;
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            _currentConfig = new ConfigModel();
            
            // 同步加载配置文件
            try
            {
                LoadConfigAsync().Wait();
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "构造函数中加载配置失败: {0}", ex.Message);
            }
            
            // 初始化文件监视器
            InitializeFileWatcher();
        }

        #region IConfigService Implementation

        public ConfigModel Config
        {
            get { return _currentConfig; }
        }

        public string ApiToken
        {
            get { return _currentConfig.Token; }
            set
            {
                if (_currentConfig.Token != value)
                {
                    _currentConfig.Token = value;
                    OnConfigChanged("ApiToken", value);
                }
            }
        }

        public string ApiBaseUrl
        {
            get { return _currentConfig.ApiBaseUrl; }
            set
            {
                if (_currentConfig.ApiBaseUrl != value)
                {
                    _currentConfig.ApiBaseUrl = value;
                    OnConfigChanged("ApiBaseUrl", value);
                }
            }
        }

        public string AliCloudApiKey
        {
            get { return _currentConfig.AliCloudApiKey; }
            set
            {
                if (_currentConfig.AliCloudApiKey != value)
                {
                    _currentConfig.AliCloudApiKey = value;
                    OnConfigChanged("AliCloudApiKey", value);
                }
            }
        }

        public string AliCloudApiBaseUrl
        {
            get { return _currentConfig.AliCloudApiBaseUrl; }
            set
            {
                if (_currentConfig.AliCloudApiBaseUrl != value)
                {
                    _currentConfig.AliCloudApiBaseUrl = value;
                    OnConfigChanged("AliCloudApiBaseUrl", value);
                }
            }
        }

        public string HotkeyModifier
        {
            get { return ExtractModifier(_currentConfig.ScreenShotKeys); }
            set
            {
                var key = ExtractKey(_currentConfig.ScreenShotKeys);
                var newHotkey = string.IsNullOrEmpty(value) ? key : string.Format("{0}+{1}", value, key);
                if (_currentConfig.ScreenShotKeys != newHotkey)
                {
                    _currentConfig.ScreenShotKeys = newHotkey;
                    OnConfigChanged("HotkeyModifier", value);
                }
            }
        }

        public string HotkeyKey
        {
            get { return ExtractKey(_currentConfig.ScreenShotKeys); }
            set
            {
                var modifier = ExtractModifier(_currentConfig.ScreenShotKeys);
                var newHotkey = string.IsNullOrEmpty(modifier) ? value : string.Format("{0}+{1}", modifier, value);
                if (_currentConfig.ScreenShotKeys != newHotkey)
                {
                    _currentConfig.ScreenShotKeys = newHotkey;
                    OnConfigChanged("HotkeyKey", value);
                }
            }
        }

        public bool AutoSaveScreenshots
        {
            get { return _currentConfig.AutoSaveScreenshots; }
            set
            {
                if (_currentConfig.AutoSaveScreenshots != value)
                {
                    _currentConfig.AutoSaveScreenshots = value;
                    OnConfigChanged("AutoSaveScreenshots", value);
                }
            }
        }

        public string ScreenshotPath
        {
            get { return _currentConfig.ScreenshotPath; }
            set
            {
                if (_currentConfig.ScreenshotPath != value)
                {
                    _currentConfig.ScreenshotPath = value;
                    OnConfigChanged("ScreenshotPath", value);
                }
            }
        }

        public bool AlwaysOnTop
        {
            get { return _currentConfig.AlwaysOnTop; }
            set
            {
                if (_currentConfig.AlwaysOnTop != value)
                {
                    _currentConfig.AlwaysOnTop = value;
                    OnConfigChanged("AlwaysOnTop", value);
                }
            }
        }

        public double WindowOpacity
        {
            get { return _currentConfig.WindowOpacity; }
            set
            {
                if (Math.Abs(_currentConfig.WindowOpacity - value) > 0.01)
                {
                    _currentConfig.WindowOpacity = value;
                    OnConfigChanged("WindowOpacity", value);
                }
            }
        }

        public event EventHandler ConfigChanged;
        public event EventHandler<ConfigChangedEventArgs> ConfigChangedDetailed;

        public async Task InitializeAsync()
        {
            await LoadConfigAsync();
        }

        public async Task LoadConfigAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    if (File.Exists(_configFilePath))
                    {
                        var json = File.ReadAllText(_configFilePath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var config = JsonConvert.DeserializeObject<ConfigModel>(json);
                            if (config != null)
                            {
                                _currentConfig = config;
                                _logService.Info("配置文件加载成功: {0}", _configFilePath);
                            }
                            else
                            {
                                _logService.Warn("配置文件格式无效，使用默认配置");
                                _currentConfig = ConfigModel.GetDefault();
                                _logService.Warn("尝试同步创建默认配置文件...");
                                CreateDefaultConfig(); // 改为同步调用
                                _logService.Info("默认配置文件创建完成。");
                            }
                        }
                        else
                        {
                            _logService.Warn("配置文件为空，使用默认配置");
                            _currentConfig = ConfigModel.GetDefault();
                            _logService.Warn("尝试同步创建默认配置文件...");
                            CreateDefaultConfig(); // 改为同步调用
                            _logService.Info("默认配置文件创建完成。");
                        }
                    }
                    else
                    {
                        _logService.Info("配置文件不存在，创建默认配置");
                        _currentConfig = ConfigModel.GetDefault();
                        CreateDefaultConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "加载配置文件失败: {0}", ex.Message);
                _currentConfig = ConfigModel.GetDefault();
                CreateDefaultConfig();
            }
        }

        public async Task SaveConfigAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var json = JsonConvert.SerializeObject(_currentConfig, Formatting.Indented);
                    
                    // 确保目录存在
                    var directory = Path.GetDirectoryName(_configFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 临时禁用文件监视器，避免触发重复加载
                    _fileWatcher.EnableRaisingEvents = false;
                    
                    File.WriteAllText(_configFilePath, json);
                    
                    // 重新启用文件监视器
                    _fileWatcher.EnableRaisingEvents = true;
                    
                    _logService.Info("配置文件保存成功: {0}", _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "保存配置文件失败: {0}", ex.Message);
                throw;
            }
        }

        public async Task ResetToDefaultAsync()
        {
            try
            {
                _currentConfig = ConfigModel.GetDefault();
                await SaveConfigAsync();
                _logService.Info("配置已重置为默认值");
                OnConfigChanged("Reset", null);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "重置配置失败: {0}", ex.Message);
                throw;
            }
        }

        public void ResetToDefault()
        {
            try
            {
                _currentConfig = ConfigModel.GetDefault();
                Task.Run(async () => await SaveConfigAsync());
                _logService.Info("配置已重置为默认值");
                OnConfigChanged("Reset", null);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "重置配置失败: {0}", ex.Message);
                throw;
            }
        }

        public bool ValidateConfig()
        {
            try
            {
                var isValid = _currentConfig.IsValid();
                if (!isValid)
                {
                    _logService.Warn("当前配置验证失败");
                }
                return isValid;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "配置验证失败: {0}", ex.Message);
                return false;
            }
        }

        public T GetConfigValue<T>(string key, T defaultValue = default(T))
        {
            try
            {
                var property = typeof(ConfigModel).GetProperty(key);
                if (property != null)
                {
                    var value = property.GetValue(_currentConfig);
                    if (value is T)
                    {
                        return (T)value;
                    }
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "获取配置值失败 [{0}]: {1}", key, ex.Message);
                return defaultValue;
            }
        }

        public void SetConfigValue<T>(string key, T value)
        {
            try
            {
                var property = typeof(ConfigModel).GetProperty(key);
                if (property != null && property.CanWrite)
                {
                    var oldValue = property.GetValue(_currentConfig);
                    if (!Equals(oldValue, value))
                    {
                        property.SetValue(_currentConfig, value);
                        OnConfigChanged(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "设置配置值失败 [{0}]: {1}", key, ex.Message);
            }
        }

        public ConfigModel GetCurrentConfig()
        {
            return _currentConfig.Clone();
        }

        #endregion

        #region Private Methods

        private void CreateDefaultConfig()
        {
            _currentConfig = ConfigModel.GetDefault();
            // 使用 .Wait() 同步执行, 确保在初始化路径上操作是阻塞的
            SaveConfigAsync().Wait();
        }

        private void InitializeFileWatcher()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                var fileName = Path.GetFileName(_configFilePath);

                _fileWatcher = new FileSystemWatcher(directory ?? AppDomain.CurrentDomain.BaseDirectory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += async (sender, e) =>
                {
                    try
                    {
                        // 延迟一下，确保文件写入完成
                        await Task.Delay(100);
                        await LoadConfigAsync();
                        OnConfigChanged("FileChanged", null);
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "文件监视器处理失败: {0}", ex.Message);
                    }
                };
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "初始化文件监视器失败: {0}", ex.Message);
            }
        }

        private string ExtractModifier(string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey) || !hotkey.Contains("+"))
                return string.Empty;

            var parts = hotkey.Split('+');
            return parts.Length > 1 ? string.Join("+", parts, 0, parts.Length - 1) : string.Empty;
        }

        private string ExtractKey(string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey))
                return string.Empty;

            var parts = hotkey.Split('+');
            return parts[parts.Length - 1];
        }

        private void OnConfigChanged(string propertyName, object value)
        {
            try
            {
                if (ConfigChanged != null)
                    ConfigChanged.Invoke(this, EventArgs.Empty);
                if (ConfigChangedDetailed != null)
                    ConfigChangedDetailed.Invoke(this, new ConfigChangedEventArgs(propertyName, value));
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "配置变更事件处理失败: {0}", ex.Message);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_fileWatcher != null)
                _fileWatcher.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigChangedEventArgs : EventArgs
    {
        public string PropertyName { get; private set; }
        public object Value { get; private set; }

        public ConfigChangedEventArgs(string propertyName, object value)
        {
            PropertyName = propertyName;
            Value = value;
        }
    }
}
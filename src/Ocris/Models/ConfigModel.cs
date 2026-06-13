using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Drawing;
using Newtonsoft.Json;
using Ocris.Services;

namespace Ocris.Models
{
    /// <summary>
    /// 配置数据模型
    /// </summary>
    public class ConfigModel : INotifyPropertyChanged
    {
        private string _token;
        private string _apiBaseUrl = "https://api.openai.com/v1";
        private string _screenShotKeys = "F1";
        private string _clipBoardKeys = "F2";
        private bool _winTopMust = true;
        private bool _autoSaveScreenshots;
        private string _screenshotPath = "Screenshots";
        private LogLevel _logLevel = LogLevel.Information;
        private string _logPath = "Logs";
        private string _currentModel = "gpt-3.5-turbo";
        private string _systemPrompt = "你是一个专业的答题助手，请根据题目内容提供准确的答案。";
        private int _requestTimeout = 30;
        private bool _enableProxy;
        private string _proxyAddress;
        private int _proxyPort;
        private double _windowOpacity = 0.95;
        private bool _alwaysOnTop = true;
        private bool _minimizeToTray = true;
        private bool _startWithWindows;

        private string _aliCloudApiKey;
        private string _aliCloudApiBaseUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
        
        public ConfigModel()
        {
            _token = "";
            _autoSaveScreenshots = false;
            _enableProxy = false;
            _proxyAddress = "";
            _proxyPort = 0;
            _startWithWindows = false;
            _aliCloudApiKey = "";
        }

        // 新增配置项
        private HotkeySettings _hotkeys = new HotkeySettings();
        private FixedAreaSettings _fixedArea = new FixedAreaSettings();
        private string _theme = "Light";
        /// <summary>
        /// API Token
        /// </summary>
        [JsonProperty("Token")]
        public string Token
        {
            get { return _token; }
            set { SetProperty(ref _token, value, "Token"); }
        }

        /// <summary>
        /// API基础URL
        /// </summary>
        [JsonProperty("ApiBaseUrl")]
        public string ApiBaseUrl
        {
            get { return _apiBaseUrl; }
            set { SetProperty(ref _apiBaseUrl, value, "ApiBaseUrl"); }
        }

        /// <summary>
        /// 截图热键
        /// </summary>
        [JsonProperty("ScreenShotKeys")]
        public string ScreenShotKeys
        {
            get { return _screenShotKeys; }
            set { SetProperty(ref _screenShotKeys, value, "ScreenShotKeys"); }
        }

        /// <summary>
        /// 剪贴板热键
        /// </summary>
        [JsonProperty("ClipBoardKeys")]
        public string ClipBoardKeys
        {
            get { return _clipBoardKeys; }
            set { SetProperty(ref _clipBoardKeys, value, "ClipBoardKeys"); }
        }

        /// <summary>
        /// 窗口置顶
        /// </summary>
        [JsonProperty("WinTopMust")]
        public bool WinTopMust
        {
            get { return _winTopMust; }
            set { SetProperty(ref _winTopMust, value, "WinTopMust"); }
        }

        /// <summary>
        /// 自动保存截图
        /// </summary>
        [JsonProperty("AutoSaveScreenshots")]
        public bool AutoSaveScreenshots
        {
            get { return _autoSaveScreenshots; }
            set { SetProperty(ref _autoSaveScreenshots, value, "AutoSaveScreenshots"); }
        }

        /// <summary>
        /// 截图保存路径
        /// </summary>
        [JsonProperty("ScreenshotPath")]
        public string ScreenshotPath
        {
            get { return _screenshotPath; }
            set { SetProperty(ref _screenshotPath, value, "ScreenshotPath"); }
        }

        /// <summary>
        /// 日志级别
        /// </summary>
        [JsonProperty("LogLevel")]
        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set { SetProperty(ref _logLevel, value, "LogLevel"); }
        }

        /// <summary>
        /// 日志路径
        /// </summary>
        [JsonProperty("LogPath")]
        public string LogPath
        {
            get { return _logPath; }
            set { SetProperty(ref _logPath, value, "LogPath"); }
        }

        /// <summary>
        /// 当前使用的AI模型
        /// </summary>
        [JsonProperty("CurrentModel")]
        public string CurrentModel
        {
            get { return _currentModel; }
            set { SetProperty(ref _currentModel, value, "CurrentModel"); }
        }

        /// <summary>
        /// 系统提示词
        /// </summary>
        [JsonProperty("SystemPrompt")]
        public string SystemPrompt
        {
            get { return _systemPrompt; }
            set { SetProperty(ref _systemPrompt, value, "SystemPrompt"); }
        }

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        [JsonProperty("RequestTimeout")]
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set { SetProperty(ref _requestTimeout, value, "RequestTimeout"); }
        }

        /// <summary>
        /// 启用代理
        /// </summary>
        [JsonProperty("EnableProxy")]
        public bool EnableProxy
        {
            get { return _enableProxy; }
            set { SetProperty(ref _enableProxy, value, "EnableProxy"); }
        }

        /// <summary>
        /// 代理地址
        /// </summary>
        [JsonProperty("ProxyAddress")]
        public string ProxyAddress
        {
            get { return _proxyAddress; }
            set { SetProperty(ref _proxyAddress, value, "ProxyAddress"); }
        }

        /// <summary>
        /// 代理端口
        /// </summary>
        [JsonProperty("ProxyPort")]
        public int ProxyPort
        {
            get { return _proxyPort; }
            set { SetProperty(ref _proxyPort, value, "ProxyPort"); }
        }

        /// <summary>
        /// 窗口透明度
        /// </summary>
        [JsonProperty("WindowOpacity")]
        public double WindowOpacity
        {
            get { return _windowOpacity; }
            set { SetProperty(ref _windowOpacity, Math.Max(0.1, Math.Min(1.0, value)), "WindowOpacity"); }
        }

        /// <summary>
        /// 总是置顶
        /// </summary>
        [JsonProperty("AlwaysOnTop")]
        public bool AlwaysOnTop
        {
            get { return _alwaysOnTop; }
            set { SetProperty(ref _alwaysOnTop, value, "AlwaysOnTop"); }
        }

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        [JsonProperty("MinimizeToTray")]
        public bool MinimizeToTray
        {
            get { return _minimizeToTray; }
            set { SetProperty(ref _minimizeToTray, value, "MinimizeToTray"); }
        }

        /// <summary>
        /// 开机启动
        /// </summary>
        [JsonProperty("StartWithWindows")]
        public bool StartWithWindows
        {
            get { return _startWithWindows; }
            set { SetProperty(ref _startWithWindows, value, "StartWithWindows"); }
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid()
        {
            // 检查必要的配置项
            if (string.IsNullOrWhiteSpace(Token))
                return false;

            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
                return false;

            if (RequestTimeout <= 0)
                return false;

            if (EnableProxy && (string.IsNullOrWhiteSpace(ProxyAddress) || ProxyPort <= 0))
                return false;

            return true;
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置实例</returns>
        public static ConfigModel GetDefault()
        {
            return new ConfigModel();
        }

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public ConfigModel Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ConfigModel>(json);
        }

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// 阿里云API Key
        /// </summary>
        [JsonProperty("AliCloudApiKey")]
        public string AliCloudApiKey
        {
            get { return _aliCloudApiKey; }
            set { SetProperty(ref _aliCloudApiKey, value, "AliCloudApiKey"); }
        }

        /// <summary>
        /// 阿里云API基础URL
        /// </summary>
        [JsonProperty("AliCloudApiBaseUrl")]
        public string AliCloudApiBaseUrl
        {
            get { return _aliCloudApiBaseUrl; }
            set { SetProperty(ref _aliCloudApiBaseUrl, value, "AliCloudApiBaseUrl"); }
        }

        /// <summary>
        /// 快捷键设置
        /// </summary>
        [JsonProperty("Hotkeys")]
        public HotkeySettings Hotkeys
        {
            get { return _hotkeys; }
            set { SetProperty(ref _hotkeys, value, "Hotkeys"); }
        }

        /// <summary>
        /// 主题（Light/Dark/Auto）
        /// </summary>
        [JsonProperty("Theme")]
        public string Theme
        {
            get { return _theme; }
            set { SetProperty(ref _theme, value, "Theme"); }
        }

        /// <summary>
        /// 固定区域设置
        /// </summary>
        [JsonProperty("FixedArea")]
        public FixedAreaSettings FixedArea
        {
            get { return _fixedArea; }
            set { SetProperty(ref _fixedArea, value, "FixedArea"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 快捷键设置
    /// </summary>
    public class HotkeySettings
    {
        /// <summary>
        /// 截图快捷键
        /// </summary>
        [JsonProperty("Screenshot")]
        public string Screenshot { get; set; }

        /// <summary>
        /// 清空快捷键
        /// </summary>
        [JsonProperty("Clear")]
        public string Clear { get; set; }

        /// <summary>
        /// 快速识别快捷键
        /// </summary>
        [JsonProperty("QuickOCR")]
        public string QuickOCR { get; set; }

        /// <summary>
        /// 唤醒/显隐主窗口快捷键
        /// </summary>
        [JsonProperty("ToggleWindow")]
        public string ToggleWindow { get; set; }

        /// <summary>
        /// 打开设置快捷键
        /// </summary>
        [JsonProperty("Settings")]
        public string Settings { get; set; }

        /// <summary>
        /// 退出程序快捷键
        /// </summary>
        [JsonProperty("Exit")]
        public string Exit { get; set; }
        
        /// <summary>
        /// 构造函数，设置默认值
        /// </summary>
        public HotkeySettings()
        {
            Screenshot = "Alt+Q";
            Clear = "Alt+C";
            QuickOCR = "F4";
            ToggleWindow = "Alt+W";
            Settings = "Alt+S";
            Exit = "Alt+X";
        }
    }

    /// <summary>
    /// 固定区域设置
    /// </summary>
    public class FixedAreaSettings
    {
        /// <summary>
        /// 是否启用固定区域
        /// </summary>
        [JsonProperty("IsEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 区域X坐标
        /// </summary>
        [JsonProperty("X")]
        public int X { get; set; }

        /// <summary>
        /// 区域Y坐标
        /// </summary>
        [JsonProperty("Y")]
        public int Y { get; set; }

        /// <summary>
        /// 区域宽度
        /// </summary>
        [JsonProperty("Width")]
        public int Width { get; set; }

        /// <summary>
        /// 区域高度
        /// </summary>
        [JsonProperty("Height")]
        public int Height { get; set; }

        /// <summary>
        /// 获取区域矩形
        /// </summary>
        [JsonIgnore]
        public Rectangle Rectangle 
        { 
            get { return new Rectangle(X, Y, Width, Height); } 
        }

        /// <summary>
        /// 设置区域矩形
        /// </summary>
        public void SetRectangle(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }
    }
}
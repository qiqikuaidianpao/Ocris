using System;
using System.Threading.Tasks;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 获取API Token
        /// </summary>
        string ApiToken { get; set; }

        /// <summary>
        /// 获取API基础URL
        /// </summary>
        string ApiBaseUrl { get; set; }

        /// <summary>
        /// 获取阿里云API Key
        /// </summary>
        string AliCloudApiKey { get; set; }

        /// <summary>
        /// 获取阿里云API基础URL
        /// </summary>
        string AliCloudApiBaseUrl { get; set; }

        /// <summary>
        /// 获取热键修饰符
        /// </summary>
        string HotkeyModifier { get; set; }

        /// <summary>
        /// 获取热键按键
        /// </summary>
        string HotkeyKey { get; set; }

        /// <summary>
        /// 是否自动保存截图
        /// </summary>
        bool AutoSaveScreenshots { get; set; }

        /// <summary>
        /// 截图保存路径
        /// </summary>
        string ScreenshotPath { get; set; }

        /// <summary>
        /// 加载配置
        /// </summary>
        Task LoadConfigAsync();

        /// <summary>
        /// 保存配置
        /// </summary>
        Task SaveConfigAsync();

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        void ResetToDefault();

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        bool ValidateConfig();

        /// <summary>
        /// 配置变更事件
        /// </summary>
        event EventHandler ConfigChanged;

        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        T GetConfigValue<T>(string key, T defaultValue = default(T));

        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        void SetConfigValue<T>(string key, T value);
    }
}
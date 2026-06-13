using System;
using System.Windows.Input;
using Newtonsoft.Json;
using Ocris.Utils;

namespace Ocris.Models
{
    /// <summary>
    /// 热键信息
    /// </summary>
    public class HotkeyInfo
    {
        /// <summary>
        /// 热键ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 热键名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 热键描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 修饰键
        /// </summary>
        public ModifierKeys Modifiers { get; set; }

        /// <summary>
        /// 按键
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// 是否已注册
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisterTime { get; set; }

        /// <summary>
        /// 最后触发时间
        /// </summary>
        public DateTime LastTriggered { get; set; }

        /// <summary>
        /// 触发次数
        /// </summary>
        public int TriggerCount { get; set; }

        /// <summary>
        /// 构造函数，设置默认值
        /// </summary>
        public HotkeyInfo()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// 热键字符串表示（如"Ctrl+Alt+Q"）
        /// </summary>
        [JsonIgnore]
        public string HotkeyString
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                
                if (Modifiers.HasFlag(ModifierKeys.Control))
                    parts.Add("Ctrl");
                if (Modifiers.HasFlag(ModifierKeys.Alt))
                    parts.Add("Alt");
                if (Modifiers.HasFlag(ModifierKeys.Shift))
                    parts.Add("Shift");
                if (Modifiers.HasFlag(ModifierKeys.Windows))
                    parts.Add("Win");
                
                parts.Add(Key.ToString());
                
                return string.Join("+", parts);
            }
        }

        /// <summary>
        /// 配置字符串表示（如"Alt|Q"）
        /// </summary>
        [JsonIgnore]
        public string ConfigString
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                
                if (Modifiers.HasFlag(ModifierKeys.Control))
                    parts.Add("Ctrl");
                if (Modifiers.HasFlag(ModifierKeys.Alt))
                    parts.Add("Alt");
                if (Modifiers.HasFlag(ModifierKeys.Shift))
                    parts.Add("Shift");
                if (Modifiers.HasFlag(ModifierKeys.Windows))
                    parts.Add("Win");
                
                parts.Add(Key.ToString());
                
                return string.Join("|", parts);
            }
        }

        /// <summary>
        /// 创建热键信息
        /// </summary>
        public static HotkeyInfo Create(string id, string name, ModifierKeys modifiers, Key key, string description = null)
        {
            return new HotkeyInfo
            {
                Id = id,
                Name = name,
                Description = description ?? name,
                Modifiers = modifiers,
                Key = key,
                RegisterTime = DateTime.Now
            };
        }

        /// <summary>
        /// 从配置字符串创建热键信息
        /// </summary>
        public static HotkeyInfo FromConfigString(string id, string name, string configString, string description = null)
        {
            ModifierKeys modifiers;
            Key key;
            if (Ocris.Utils.HotkeyParser.TryParse(configString, out modifiers, out key))
            {
                return Create(id, name, modifiers, key, description);
            }
            throw new ArgumentException(string.Format("无效的热键配置字符串: {0}", configString));
        }

        /// <summary>
        /// 验证热键是否有效
        /// </summary>
        public bool IsValid()
        {
            return Key != Key.None && !string.IsNullOrWhiteSpace(Id);
        }

        /// <summary>
        /// 记录触发
        /// </summary>
        public void RecordTrigger()
        {
            LastTriggered = DateTime.Now;
            TriggerCount++;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            TriggerCount = 0;
            LastTriggered = DateTime.MinValue;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, HotkeyString);
        }

        public override bool Equals(object obj)
        {
            var other = obj as HotkeyInfo;
            if (other != null)
            {
                return Modifiers == other.Modifiers && Key == other.Key;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Modifiers.GetHashCode();
                hash = hash * 23 + Key.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// 热键事件参数
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        private readonly HotkeyInfo _hotkeyInfo;
        private readonly DateTime _triggerTime;

        /// <summary>
        /// 热键信息
        /// </summary>
        public HotkeyInfo HotkeyInfo 
        { 
            get { return _hotkeyInfo; } 
        }

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggerTime 
        { 
            get { return _triggerTime; } 
        }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// 额外数据
        /// </summary>
        public object Tag { get; set; }

        public HotkeyEventArgs(HotkeyInfo hotkeyInfo)
        {
            if (hotkeyInfo == null) throw new ArgumentNullException("hotkeyInfo");
            _hotkeyInfo = hotkeyInfo;
            _triggerTime = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format("Hotkey '{0}' triggered at {1}", HotkeyInfo.Name, TriggerTime);
        }
    }

    /// <summary>
    /// 热键配置
    /// </summary>
    public class HotkeyConfig
    {
        /// <summary>
        /// 截图热键
        /// </summary>
        [JsonProperty("ScreenshotHotkey")]
        public string ScreenshotHotkey { get; set; }

        /// <summary>
        /// 显示/隐藏主窗口热键
        /// </summary>
        [JsonProperty("ToggleMainWindowHotkey")]
        public string ToggleMainWindowHotkey { get; set; }

        /// <summary>
        /// 退出程序热键
        /// </summary>
        [JsonProperty("ExitHotkey")]
        public string ExitHotkey { get; set; }

        /// <summary>
        /// 设置窗口热键
        /// </summary>
        [JsonProperty("SettingsHotkey")]
        public string SettingsHotkey { get; set; }

        /// <summary>
        /// 清除历史热键
        /// </summary>
        [JsonProperty("ClearHistoryHotkey")]
        public string ClearHistoryHotkey { get; set; }

        /// <summary>
        /// 是否启用热键
        /// </summary>
        [JsonProperty("EnableHotkeys")]
        public bool EnableHotkeys { get; set; }

        /// <summary>
        /// 是否显示热键冲突警告
        /// </summary>
        [JsonProperty("ShowConflictWarning")]
        public bool ShowConflictWarning { get; set; }

        /// <summary>
        /// 热键响应延迟（毫秒）
        /// </summary>
        [JsonProperty("HotkeyDelay")]
        public int HotkeyDelay { get; set; }

        /// <summary>
        /// 构造函数，设置默认值
        /// </summary>
        public HotkeyConfig()
        {
            ScreenshotHotkey = "Alt|Q";
            ToggleMainWindowHotkey = "Alt|W";
            ExitHotkey = "Alt|X";
            SettingsHotkey = "Alt|S";
            ClearHistoryHotkey = "Alt|C";
            EnableHotkeys = true;
            ShowConflictWarning = true;
            HotkeyDelay = 100;
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static HotkeyConfig GetDefault()
        {
            return new HotkeyConfig();
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            var configs = new[] { ScreenshotHotkey, ToggleMainWindowHotkey, ExitHotkey, SettingsHotkey, ClearHistoryHotkey };
            
            foreach (var config in configs)
            {
                if (string.IsNullOrWhiteSpace(config))
                    return false;
                    
                ModifierKeys modifiers;
                Key key;
                if (!Ocris.Utils.HotkeyParser.TryParse(config, out modifiers, out key))
                    return false;
            }
            
            return HotkeyDelay >= 0;
        }

        /// <summary>
        /// 检查是否有重复的热键配置
        /// </summary>
        public bool HasDuplicateHotkeys(out string[] duplicates)
        {
            var configs = new[]
            {
                new { Name = "Screenshot", Hotkey = ScreenshotHotkey },
                new { Name = "ToggleMainWindow", Hotkey = ToggleMainWindowHotkey },
                new { Name = "Exit", Hotkey = ExitHotkey },
                new { Name = "Settings", Hotkey = SettingsHotkey },
                new { Name = "ClearHistory", Hotkey = ClearHistoryHotkey }
            };

            var duplicateList = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < configs.Length; i++)
            {
                for (int j = i + 1; j < configs.Length; j++)
                {
                    if (configs[i].Hotkey == configs[j].Hotkey)
                    {
                        duplicateList.Add(string.Format("{0} 和 {1} 使用相同热键: {2}", configs[i].Name, configs[j].Name, configs[i].Hotkey));
                    }
                }
            }
            
            duplicates = duplicateList.ToArray();
            return duplicates.Length > 0;
        }
    }
}
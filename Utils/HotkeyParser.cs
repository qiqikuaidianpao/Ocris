using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Ocris.Utils
{
    /// <summary>
    /// 热键解析工具
    /// </summary>
    public static class HotkeyParser
    {
        /// <summary>
        /// 修饰键映射表
        /// </summary>
        private static readonly Dictionary<string, ModifierKeys> ModifierMap = new Dictionary<string, ModifierKeys>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ctrl", ModifierKeys.Control },
            { "Control", ModifierKeys.Control },
            { "Alt", ModifierKeys.Alt },
            { "Shift", ModifierKeys.Shift },
            { "Win", ModifierKeys.Windows },
            { "Windows", ModifierKeys.Windows },
            { "Cmd", ModifierKeys.Windows }, // Mac兼容
            { "Meta", ModifierKeys.Windows }  // 通用名称
        };

        /// <summary>
        /// 按键别名映射表
        /// </summary>
        private static readonly Dictionary<string, Key> KeyAliasMap = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
        {
            // 数字键
            { "0", Key.D0 }, { "1", Key.D1 }, { "2", Key.D2 }, { "3", Key.D3 }, { "4", Key.D4 },
            { "5", Key.D5 }, { "6", Key.D6 }, { "7", Key.D7 }, { "8", Key.D8 }, { "9", Key.D9 },
            
            // 功能键别名
            { "Esc", Key.Escape },
            { "Enter", Key.Return },
            { "Return", Key.Return },
            { "Space", Key.Space },
            { "Spacebar", Key.Space },
            { "Tab", Key.Tab },
            { "Backspace", Key.Back },
            { "Delete", Key.Delete },
            { "Del", Key.Delete },
            { "Insert", Key.Insert },
            { "Ins", Key.Insert },
            { "Home", Key.Home },
            { "End", Key.End },
            { "PageUp", Key.PageUp },
            { "PgUp", Key.PageUp },
            { "PageDown", Key.PageDown },
            { "PgDn", Key.PageDown },
            
            // 方向键
            { "Up", Key.Up },
            { "Down", Key.Down },
            { "Left", Key.Left },
            { "Right", Key.Right },
            { "ArrowUp", Key.Up },
            { "ArrowDown", Key.Down },
            { "ArrowLeft", Key.Left },
            { "ArrowRight", Key.Right },
            
            // 符号键
            { "Plus", Key.OemPlus },
            { "Minus", Key.OemMinus },
            { "Equal", Key.OemPlus },
            { "Comma", Key.OemComma },
            { "Period", Key.OemPeriod },
            { "Dot", Key.OemPeriod },
            { "Semicolon", Key.OemSemicolon },
            { "Quote", Key.OemQuotes },
            { "Backslash", Key.OemBackslash },
            { "Slash", Key.OemQuestion },
            { "Tilde", Key.OemTilde },
            { "LeftBracket", Key.OemOpenBrackets },
            { "RightBracket", Key.OemCloseBrackets },
            
            // 小键盘
            { "Num0", Key.NumPad0 }, { "Num1", Key.NumPad1 }, { "Num2", Key.NumPad2 },
            { "Num3", Key.NumPad3 }, { "Num4", Key.NumPad4 }, { "Num5", Key.NumPad5 },
            { "Num6", Key.NumPad6 }, { "Num7", Key.NumPad7 }, { "Num8", Key.NumPad8 },
            { "Num9", Key.NumPad9 },
            { "NumPlus", Key.Add },
            { "NumMinus", Key.Subtract },
            { "NumMultiply", Key.Multiply },
            { "NumDivide", Key.Divide },
            { "NumEnter", Key.Return },
            { "NumPeriod", Key.Decimal },
            
            // 媒体键
            { "VolumeUp", Key.VolumeUp },
            { "VolumeDown", Key.VolumeDown },
            { "VolumeMute", Key.VolumeMute },
            { "MediaNext", Key.MediaNextTrack },
            { "MediaPrev", Key.MediaPreviousTrack },
            { "MediaPlay", Key.MediaPlayPause },
            { "MediaStop", Key.MediaStop }
        };

        /// <summary>
        /// 尝试解析热键字符串
        /// </summary>
        /// <param name="configString">热键配置字符串</param>
        /// <param name="modifiers">解析出的修饰键</param>
        /// <param name="key">解析出的按键</param>
        /// <returns>解析是否成功</returns>
        public static bool TryParse(string configString, out ModifierKeys modifiers, out Key key)
        {
            modifiers = ModifierKeys.None;
            key = Key.None;
            
            if (string.IsNullOrEmpty(configString))
                return false;
                
            try
            {
                var parts = configString.Split('+');
                if (parts.Length == 0)
                    return false;
                    
                // 最后一个部分是按键，前面的都是修饰键
                var keyPart = parts[parts.Length - 1].Trim();
                
                // 解析修饰键
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var modifierPart = parts[i].Trim();
                    if (ModifierMap.ContainsKey(modifierPart))
                    {
                        modifiers |= ModifierMap[modifierPart];
                    }
                    else
                    {
                        return false;
                    }
                }
                
                // 解析按键
                if (KeyAliasMap.ContainsKey(keyPart))
                {
                    key = KeyAliasMap[keyPart];
                    return true;
                }
                
                // 尝试直接解析为Key枚举
                Key parsedKey;
                if (Enum.TryParse(keyPart, true, out parsedKey))
                {
                    key = parsedKey;
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
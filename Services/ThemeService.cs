using System;
using System.Windows;
using System.Windows.Media;

namespace Ocris.Services
{
    /// <summary>
    /// 主题服务：在 Application.Resources 设置主题画刷，供 {DynamicResource} 引用。
    /// 切换主题时只需调 ApplyTheme，所有 DynamicResource 即时刷新。
    /// </summary>
    public static class ThemeService
    {
        public const string Light = "Light";
        public const string Dark = "Dark";

        private static string _current = Light;

        /// <summary>
        /// 当前主题
        /// </summary>
        public static string Current
        {
            get { return _current; }
        }

        /// <summary>
        /// 应用主题（Light / Dark / Auto→Light）
        /// </summary>
        public static void ApplyTheme(string theme)
        {
            var app = Application.Current;
            if (app == null) return;

            if (string.IsNullOrEmpty(theme) || theme == "Auto")
            {
                theme = Light; // Auto 暂按浅色（后续可读系统主题）
            }
            _current = theme;
            bool dark = (theme == Dark);

            // 背景 / 表面
            Set(app, "WindowBackgroundBrush", dark ? "#1A1B1E" : "#F7F8FC");
            Set(app, "SurfaceBrush", dark ? "#242529" : "#FFFFFF");       // 卡片/顶栏/底栏
            Set(app, "SurfaceAltBrush", dark ? "#1F2024" : "#FAFBFC");     // 次级表面
            Set(app, "BorderColorBrush", dark ? "#374151" : "#EAECF2");
            Set(app, "BorderLightBrush", dark ? "#3F4147" : "#E5E7EB");

            // 主色（靛蓝，暗色稍亮）
            Set(app, "PrimaryBrush", dark ? "#6366F1" : "#4F46E5");
            Set(app, "PrimaryHoverBrush", dark ? "#818CF8" : "#4338CA");
            Set(app, "PrimaryLightBrush", dark ? "#312E81" : "#EEF2FF");

            // 文字
            Set(app, "TextPrimaryBrush", dark ? "#F3F4F6" : "#111827");
            Set(app, "TextSecondaryBrush", dark ? "#D1D5DB" : "#374151");
            Set(app, "TextMutedBrush", dark ? "#9CA3AF" : "#6B7280");
            Set(app, "TextFaintBrush", dark ? "#6B7280" : "#9CA3AF");

            // 语义色
            Set(app, "SuccessBrush", dark ? "#34D399" : "#10B981");
            Set(app, "WarningBrush", dark ? "#FBBF24" : "#D97706");
            Set(app, "DangerBrush", dark ? "#F87171" : "#DC2626");
        }

        /// <summary>
        /// 是否深色
        /// </summary>
        public static bool IsDark
        {
            get { return _current == Dark; }
        }

        private static void Set(Application app, string key, string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                app.Resources[key] = new SolidColorBrush(color);
            }
            catch
            {
                // 忽略无效颜色
            }
        }
    }
}

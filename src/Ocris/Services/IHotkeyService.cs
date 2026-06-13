using System;
using System.Windows.Input;

namespace Ocris.Services
{
    public interface IHotkeyService
    {
        /// <summary>
        /// 快捷键触发事件
        /// </summary>
        /// <param name="actionName">动作名称</param>
        event Action<string> HotkeyPressed;

        /// <summary>
        /// 快捷键注册失败事件
        /// </summary>
        /// <param name="reason">失败原因</param>
        event Action<string> HotkeyRegistrationFailed;

        /// <summary>
        /// 注册快捷键（简单版本）
        /// </summary>
        /// <param name="key">按键名称（如"F1"）</param>
        /// <param name="actionName">动作名称</param>
        void RegisterHotkey(string key, string actionName);

        /// <summary>
        /// 注册快捷键（完整版本）
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <param name="name">热键名称</param>
        /// <param name="modifiers">修饰键</param>
        /// <param name="key">按键</param>
        /// <param name="description">描述</param>
        /// <returns>是否注册成功</returns>
        bool RegisterHotkey(string id, string name, ModifierKeys modifiers, Key key, string description);

        /// <summary>
        /// 检查热键是否可用
        /// </summary>
        /// <param name="modifiers">修饰键</param>
        /// <param name="key">按键</param>
        /// <returns>是否可用</returns>
        bool IsHotkeyAvailable(ModifierKeys modifiers, Key key);

        /// <summary>
        /// 检查热键是否处于活动状态
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <returns>是否活动</returns>
        bool IsHotkeyActive(string id);

        /// <summary>
        /// 注销指定快捷键
        /// </summary>
        /// <param name="actionName">动作名称</param>
        void UnregisterHotkey(string actionName);

        /// <summary>
        /// 注销所有快捷键
        /// </summary>
        void UnregisterAll();
    }
}
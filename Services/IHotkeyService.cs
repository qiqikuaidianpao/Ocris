using System;
using System.Windows.Input;
using AIAnswerTool.Models;

namespace AIAnswerTool.Services
{


    /// <summary>
    /// 热键服务接口
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <param name="name">热键名称</param>
        /// <param name="modifiers">修饰键</param>
        /// <param name="key">按键</param>
        /// <param name="description">描述</param>
        /// <returns>注册是否成功</returns>
        bool RegisterHotkey(string id, string name, ModifierKeys modifiers, Key key, string description = "");

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="hotkey">热键信息</param>
        /// <returns>注册是否成功</returns>
        bool RegisterHotkey(HotkeyInfo hotkey);

        /// <summary>
        /// 取消注册热键
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <returns>取消注册是否成功</returns>
        bool UnregisterHotkey(string id);

        /// <summary>
        /// 取消注册所有热键
        /// </summary>
        void UnregisterAllHotkeys();

        /// <summary>
        /// 检查热键是否已注册
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <returns>是否已注册</returns>
        bool IsHotkeyRegistered(string id);

        /// <summary>
        /// 检查热键组合是否可用
        /// </summary>
        /// <param name="modifiers">修饰键</param>
        /// <param name="key">按键</param>
        /// <returns>是否可用</returns>
        bool IsHotkeyAvailable(ModifierKeys modifiers, Key key);

        /// <summary>
        /// 检查热键是否处于活动状态（已注册且在系统中有效）
        /// </summary>
        /// <param name="hotkeyName">热键名称或ID</param>
        /// <returns>热键是否活动</returns>
        bool IsHotkeyActive(string hotkeyName);

        /// <summary>
        /// 获取热键信息
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <returns>热键信息</returns>
        Models.HotkeyInfo GetHotkey(string id);

        /// <summary>
        /// 获取所有已注册的热键
        /// </summary>
        /// <returns>热键列表</returns>
        Models.HotkeyInfo[] GetAllHotkeys();

        /// <summary>
        /// 更新热键
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <param name="modifiers">新的修饰键</param>
        /// <param name="key">新的按键</param>
        /// <returns>更新是否成功</returns>
        bool UpdateHotkey(string id, ModifierKeys modifiers, Key key);

        /// <summary>
        /// 启用热键服务
        /// </summary>
        void Enable();

        /// <summary>
        /// 禁用热键服务
        /// </summary>
        void Disable();

        /// <summary>
        /// 热键服务是否已启用
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 热键触发事件
        /// </summary>
        event EventHandler<Models.HotkeyEventArgs> HotkeyPressed;

        /// <summary>
        /// 热键注册失败事件
        /// </summary>
        event EventHandler<string> HotkeyRegistrationFailed;
    }
}
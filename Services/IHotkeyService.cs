using System;
using System.Windows.Input;

namespace AIAnswerTool.Services
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
        /// 修饰键
        /// </summary>
        public ModifierKeys Modifiers { get; set; }

        /// <summary>
        /// 按键
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否已注册
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// 热键字符串表示
        /// </summary>
        public string DisplayText { get { return string.Format("{0}+{1}", Modifiers, Key); } }
    }

    /// <summary>
    /// 热键事件参数
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        /// <summary>
        /// 热键信息
        /// </summary>
        public HotkeyInfo Hotkey { get; set; }

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggerTime { get; set; }

        public HotkeyEventArgs(HotkeyInfo hotkey)
        {
            Hotkey = hotkey;
            TriggerTime = DateTime.Now;
        }
    }

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
        /// 获取热键信息
        /// </summary>
        /// <param name="id">热键ID</param>
        /// <returns>热键信息</returns>
        HotkeyInfo GetHotkey(string id);

        /// <summary>
        /// 获取所有已注册的热键
        /// </summary>
        /// <returns>热键列表</returns>
        HotkeyInfo[] GetAllHotkeys();

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
        event EventHandler<HotkeyEventArgs> HotkeyPressed;

        /// <summary>
        /// 热键注册失败事件
        /// </summary>
        event EventHandler<string> HotkeyRegistrationFailed;
    }
}
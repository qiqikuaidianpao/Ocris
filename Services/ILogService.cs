using System;

namespace Ocris.Services
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// 日志服务接口 - 重构版本，提供统一清晰的日志记录方法
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Debug(string message, params object[] args);

        /// <summary>
        /// 记录一般信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// 记录警告信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Warn(string message, params object[] args);

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Error(string message, params object[] args);

        /// <summary>
        /// 记录异常信息
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Error(Exception exception, string message, params object[] args);

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Fatal(string message, params object[] args);

        /// <summary>
        /// 记录致命错误
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        void Fatal(Exception exception, string message, params object[] args);

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="level">日志级别</param>
        void SetLogLevel(LogLevel level);

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        void CleanupOldLogs(int daysToKeep = 30);

        #region 向后兼容方法 - 已过时，请使用新的方法名

        /// <summary>
        /// 记录一般信息 - 已过时，请使用 Info 方法
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        [Obsolete("请使用 Info 方法替代", false)]
        void Information(string message, params object[] args);

        /// <summary>
        /// 记录警告信息 - 已过时，请使用 Warn 方法
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        [Obsolete("请使用 Warn 方法替代", false)]
        void Warning(string message, params object[] args);

        /// <summary>
        /// 记录异常信息 - 已过时，请使用 Error 方法
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常对象</param>
        [Obsolete("请使用 Error(Exception, string, params object[]) 方法替代", false)]
        void LogException(string message, Exception ex);

        /// <summary>
        /// 记录信息日志 - 已过时，请使用 Info 方法
        /// </summary>
        /// <param name="message">消息</param>
        [Obsolete("请使用 Info 方法替代", false)]
        void LogInformation(string message);

        /// <summary>
        /// 记录信息日志 - 已过时，请使用 Info 方法
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="args">参数</param>
        [Obsolete("请使用 Info 方法替代", false)]
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// 记录异常信息 - 已过时，请使用 Error 方法
        /// </summary>
        /// <param name="exception">异常</param>
        [Obsolete("请使用 Error(Exception, string, params object[]) 方法替代", false)]
        void LogException(Exception exception);

        #endregion
    }
}
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AIAnswerTool.Services;

namespace AIAnswerTool.Utils
{
    /// <summary>
    /// 日志记录扩展方法
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        /// 记录方法进入日志
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void LogMethodEntry(this ILogService logService,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            logService.Debug(string.Format("[ENTRY] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
        }

        /// <summary>
        /// 记录方法退出日志
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void LogMethodExit(this ILogService logService,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            logService.Debug(string.Format("[EXIT] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
        }

        /// <summary>
        /// 记录方法执行时间
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="action">要执行的操作</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void LogExecutionTime(this ILogService logService, Action action,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                logService.Debug(string.Format("[START] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
                action();
            }
            finally
            {
                stopwatch.Stop();
                logService.Debug(string.Format("[TIMING] {0}:{1} - {2}() completed in {3}ms", fileName, sourceLineNumber, memberName, stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 记录方法执行时间（异步版本）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="func">要执行的异步操作</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static async System.Threading.Tasks.Task LogExecutionTimeAsync(this ILogService logService, Func<System.Threading.Tasks.Task> func,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                logService.Debug(string.Format("[START] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
                await func();
            }
            finally
            {
                stopwatch.Stop();
                logService.Debug(string.Format("[TIMING] {0}:{1} - {2}() completed in {3}ms", fileName, sourceLineNumber, memberName, stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 记录带返回值的方法执行时间
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="logService">日志服务</param>
        /// <param name="func">要执行的操作</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        /// <returns>操作结果</returns>
        public static T LogExecutionTime<T>(this ILogService logService, Func<T> func,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                logService.Debug(string.Format("[START] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
                return func();
            }
            finally
            {
                stopwatch.Stop();
                logService.Debug(string.Format("[TIMING] {0}:{1} - {2}() completed in {3}ms", fileName, sourceLineNumber, memberName, stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 记录带返回值的异步方法执行时间
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="logService">日志服务</param>
        /// <param name="func">要执行的异步操作</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        /// <returns>操作结果</returns>
        public static async System.Threading.Tasks.Task<T> LogExecutionTimeAsync<T>(this ILogService logService, Func<System.Threading.Tasks.Task<T>> func,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                logService.Debug(string.Format("[START] {0}:{1} - {2}()", fileName, sourceLineNumber, memberName));
                return await func();
            }
            finally
            {
                stopwatch.Stop();
                logService.Debug(string.Format("[TIMING] {0}:{1} - {2}() completed in {3}ms", fileName, sourceLineNumber, memberName, stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 记录异常信息（扩展方法）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="exception">异常对象</param>
        /// <param name="message">附加消息</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void LogException(this ILogService logService, Exception exception, string message = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(sourceFilePath);
            var logMessage = string.IsNullOrEmpty(message) 
                ? string.Format("[EXCEPTION] {0}:{1} - {2}() - {3}: {4}", fileName, sourceLineNumber, memberName, exception.GetType().Name, exception.Message)
                : string.Format("[EXCEPTION] {0}:{1} - {2}() - {3}", fileName, sourceLineNumber, memberName, message);
            
            logService.Error(logMessage, exception);
        }

        /// <summary>
        /// 记录性能警告
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="elapsedMilliseconds">执行时间（毫秒）</param>
        /// <param name="threshold">阈值（毫秒）</param>
        /// <param name="operation">操作名称</param>
        /// <param name="memberName">方法名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void LogPerformanceWarning(this ILogService logService, long elapsedMilliseconds, long threshold, string operation,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (elapsedMilliseconds > threshold)
            {
                var fileName = System.IO.Path.GetFileName(sourceFilePath);
                logService.Warn(string.Format("[PERFORMANCE] {0}:{1} - {2}() - {3} took {4}ms (threshold: {5}ms)", fileName, sourceLineNumber, memberName, operation, elapsedMilliseconds, threshold));
            }
        }

        /// <summary>
        /// 记录用户操作
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="action">用户操作</param>
        /// <param name="details">操作详情</param>
        public static void LogUserAction(this ILogService logService, string action, string details = null)
        {
            var message = string.IsNullOrEmpty(details) 
                ? string.Format("[USER_ACTION] {0}", action)
                : string.Format("[USER_ACTION] {0} - {1}", action, details);
            
            logService.Info(message);
        }

        /// <summary>
        /// 记录API调用
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="endpoint">API端点</param>
        /// <param name="method">HTTP方法</param>
        /// <param name="statusCode">状态码</param>
        /// <param name="elapsedMilliseconds">响应时间</param>
        public static void LogApiCall(this ILogService logService, string endpoint, string method, int statusCode, long elapsedMilliseconds)
        {
            var level = statusCode >= 400 ? "ERROR" : "INFO";
            var message = string.Format("[API_CALL] {0} {1} - {2} ({3}ms)", method, endpoint, statusCode, elapsedMilliseconds);
            
            if (statusCode >= 400)
            {
                logService.Error(message);
            }
            else
            {
                logService.Info(message);
            }
        }

        /// <summary>
        /// 记录配置变更
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="configKey">配置键</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public static void LogConfigChange(this ILogService logService, string configKey, object oldValue, object newValue)
        {
            logService.Info(string.Format("[CONFIG_CHANGE] {0}: '{1}' -> '{2}'", configKey, oldValue, newValue));
        }

        /// <summary>
        /// 记录调试信息（简化调用）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="message">调试消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogDebug(this ILogService logService, string message, params object[] args)
        {
            logService.Debug(message, args);
        }

        /// <summary>
        /// 记录信息（简化调用）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="message">信息消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogInfo(this ILogService logService, string message, params object[] args)
        {
            logService.Info(message, args);
        }

        /// <summary>
        /// 记录警告信息（简化调用）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="message">警告消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogWarn(this ILogService logService, string message, params object[] args)
        {
            logService.Warn(message, args);
        }

        /// <summary>
        /// 记录错误信息（简化调用）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="message">错误消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogError(this ILogService logService, string message, params object[] args)
        {
            logService.Error(message, args);
        }

        /// <summary>
        /// 记录错误信息（带异常）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="exception">异常对象</param>
        /// <param name="message">错误消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogError(this ILogService logService, Exception exception, string message, params object[] args)
        {
            logService.Error(exception, message, args);
        }

        /// <summary>
        /// 记录致命错误（简化调用）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="message">致命错误消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogFatal(this ILogService logService, string message, params object[] args)
        {
            logService.Fatal(message, args);
        }

        /// <summary>
        /// 记录致命错误（带异常）
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="exception">异常对象</param>
        /// <param name="message">致命错误消息</param>
        /// <param name="args">格式化参数</param>
        public static void LogFatal(this ILogService logService, Exception exception, string message, params object[] args)
        {
            logService.Fatal(exception, message, args);
        }
    }
}
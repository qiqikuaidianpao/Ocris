using System;
using System.IO;
using System.Threading.Tasks;
using AIAnswerTool.Services;
using System.Diagnostics;
// using Serilog.Formatting.Compact; // 已移除Serilog依赖

namespace AIAnswerTool.Services
{
    /// <summary>
    /// 日志记录服务实现
    /// </summary>
    public class LogService : ILogService, IDisposable
    {
       private static TextWriter _logger;
        private static bool _isDebugEnabled = true;
        private string _logDirectory;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public LogService()
        {
            // 默认使用AppData目录，稍后可通过SetLogDirectory方法更改
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AIAnswerTool", "Logs");
            
            try
            {
                InitializeLogger();
            }
            catch (Exception ex)
            {
                // 如果日志初始化失败，使用控制台输出
                Console.WriteLine(string.Format("日志服务初始化失败: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 设置日志目录并重新初始化日志服务
        /// </summary>
        /// <param name="logPath">日志路径（相对路径或绝对路径）</param>
        public void SetLogDirectory(string logPath)
        {
            try
            {
                lock (_lockObject)
                {
                    // 关闭当前日志文件
                    if (_logger != null)
                    {
                        _logger.Dispose();
                        _logger = null;
                    }

                    // 设置新的日志目录
                    if (Path.IsPathRooted(logPath))
                    {
                        // 绝对路径
                        _logDirectory = logPath;
                    }
                    else
                    {
                        // 相对路径，相对于应用程序目录
                        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logPath);
                    }

                    // 重新初始化日志服务
                    InitializeLogger();
                    Info("日志目录已更新为: {0}", _logDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("设置日志目录失败: {0}", ex.Message));
            }
        }

        #region ILogService Implementation

        public void Debug(string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isDebugEnabled && _logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [DEBUG] {1}", DateTime.Now, formattedMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入调试日志失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        [Obsolete("请使用 Debug 方法替代")]
        public void LogDebug(string message)
        {
            Debug(message);
        }

        [Obsolete("请使用 Debug 方法替代")]
        public void LogDebug(string message, params object[] args)
        {
            Debug(message, args);
        }

        public void Info(string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [INFO] {1}", DateTime.Now, formattedMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入信息日志失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        [Obsolete("请使用 Info 方法替代")]
        public void Information(string message, params object[] args)
        {
            Info(message, args);
        }

        [Obsolete("请使用 Info 方法替代")]
        public void LogInformation(string message)
        {
            Info(message);
        }

        [Obsolete("请使用 Info 方法替代")]
        public void LogInformation(string message, params object[] args)
        {
            Info(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [WARN] {1}", DateTime.Now, formattedMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入警告日志失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        [Obsolete("请使用 Warn 方法替代")]
        public void Warning(string message, params object[] args)
        {
            Warn(message, args);
        }

        [Obsolete("请使用 Warn 方法替代")]
        public void LogWarning(string message)
        {
            Warn(message);
        }

        [Obsolete("请使用 Warn 方法替代")]
        public void LogWarning(string message, params object[] args)
        {
            Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] {1}", DateTime.Now, formattedMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入错误日志失败: {0}", ex.Message));
            }
        }

        public void Error(Exception exception, string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] {1}", DateTime.Now, formattedMessage));
                        if (exception != null)
                        {
                            _logger.WriteLine(string.Format("Exception: {0}", exception.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入错误日志失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        [Obsolete("请使用 Error 方法替代")]
        public void LogError(string message)
        {
            Error(message);
        }

        [Obsolete("请使用 Error 方法替代")]
        public void LogError(string message, Exception exception)
        {
            Error(exception, message);
        }

        [Obsolete("请使用 Error 方法替代")]
        public void LogError(string message, params object[] args)
        {
            Error(message, args);
        }

        [Obsolete("请使用 Error 方法替代")]
        public void LogException(string message, Exception exception)
        {
            Error(exception, message);
        }

        [Obsolete("请使用 Error 方法替代")]
        public void LogException(Exception exception)
        {
            Error(exception, "Exception occurred");
        }

        public void Fatal(string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [FATAL] {1}", DateTime.Now, formattedMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入致命错误日志失败: {0}", ex.Message));
            }
        }

        public void Fatal(Exception exception, string message, params object[] args)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_logger != null)
                    {
                        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                        _logger.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [FATAL] {1}", DateTime.Now, formattedMessage));
                        if (exception != null)
                        {
                            _logger.WriteLine(string.Format("Exception: {0}", exception.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("写入致命错误日志失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        [Obsolete("请使用 Fatal 方法替代")]
        public void LogFatal(string message)
        {
            Fatal(message);
        }

        [Obsolete("请使用 Fatal 方法替代")]
        public void LogFatal(string message, Exception exception)
        {
            Fatal(exception, message);
        }

        [Obsolete("请使用 Fatal 方法替代")]
        public void LogFatal(string message, params object[] args)
        {
            Fatal(message, args);
        }

        public void SetLogLevel(LogLevel level)
        {
            try
            {
                lock (_lockObject)
                {
                    _isDebugEnabled = level == LogLevel.Debug;
                    Info(string.Format("日志级别已设置为: {0}", level));
                }
            }
            catch (Exception ex)
            {
                // 避免在日志服务内部使用日志记录，直接输出到控制台
                Console.WriteLine(string.Format("设置日志级别失败: {0}", ex.Message));
            }
        }

        // 保留原有方法以兼容现有代码
        public void SetLogLevel(int level) // 0=Debug, 1=Info, 2=Warning, 3=Error
        {
            LogLevel logLevel = LogLevel.Information;
            switch (level)
            {
                case 0:
                    logLevel = LogLevel.Debug;
                    break;
                case 1:
                    logLevel = LogLevel.Information;
                    break;
                case 2:
                    logLevel = LogLevel.Warning;
                    break;
                case 3:
                    logLevel = LogLevel.Error;
                    break;
                case 4:
                    logLevel = LogLevel.Fatal;
                    break;
            }
            SetLogLevel(logLevel);
        }

        public int GetCurrentLogLevel()
        {
            return _isDebugEnabled ? 0 : 1;
        }

        public void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                int deletedCount = 0;

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("删除日志文件失败 [{0}]: {1}", file, ex.Message));
                    }
                }

                if (deletedCount > 0)
                {
                    Info(string.Format("清理了 {0} 个过期日志文件", deletedCount));
                }
            }
            catch (Exception ex)
            {
                Error(ex, string.Format("清理日志文件失败: {0}", ex.Message));
            }
        }

        public async Task CleanupOldLogsAsync(int daysToKeep = 30)
        {
            try
            {
                await Task.Run(() =>
                {
                    CleanupOldLogs(daysToKeep);
                });
            }
            catch (Exception ex)
            {
                Error(ex, string.Format("异步清理日志文件失败: {0}", ex.Message));
            }
        }

        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        public long GetLogDirectorySize()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                    return 0;

                var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                long totalSize = 0;

                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch
                    {
                        // 忽略无法访问的文件
                    }
                }

                return totalSize;
            }
            catch (Exception ex)
            {
                Error(string.Format("获取日志目录大小失败: {0}", ex.Message));
                return 0;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeLogger()
        {
            try
            {
                // 确保日志目录存在
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // 创建日志文件
                string logFileName = Path.Combine(_logDirectory, string.Format("log-{0:yyyy-MM-dd}.txt", DateTime.Now));
                _logger = new StreamWriter(logFileName, true) { AutoFlush = true };
                
                Info("日志服务初始化成功");
                Info(string.Format("日志目录: {0}", _logDirectory));
                Info(string.Format("当前日志级别: {0}", GetCurrentLogLevel()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("初始化日志服务失败: {0}", ex.Message));
                throw;
            }
        }

        private int ConvertToLogLevel(int level)
        {
            // 简单的日志级别转换
            return level;
        }

        private int ConvertFromLogLevel(int level)
        {
            // 简单的日志级别转换
            return level;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    lock (_lockObject)
                    {
                        Info("日志服务正在关闭");
                        if (_logger != null)
                        {
                            _logger.Close();
                            _logger.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("关闭日志服务时发生错误: {0}", ex.Message));
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        ~LogService()
        {
            Dispose(false);
        }

        #endregion
    }
}
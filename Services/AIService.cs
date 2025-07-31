using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using AIAnswerTool.Models;
using AIAnswerTool.Utils;

namespace AIAnswerTool.Services
{
    /// <summary>
    /// AI服务基类
    /// </summary>
    public abstract class AIService : IAIService
    {
        protected readonly ILogService _logService;
        protected readonly IConfigService _configService;
        protected readonly HttpClient _httpClient;
        protected string _apiToken;
        protected string _baseUrl;
        protected string _currentModel;
        protected string _systemPrompt;
        protected List<AIMessage> _conversationHistory;
        protected readonly int _maxRetries = 3;
        protected readonly int _timeoutSeconds = 30;

        public string CurrentModel { get { return _currentModel; } }
        public virtual bool IsConfigured { get { return !string.IsNullOrEmpty(_apiToken) && !string.IsNullOrEmpty(_baseUrl); } }

        public event EventHandler<AIResponse> ResponseReceived;
        public event EventHandler<string> ApiError;

        protected AIService(ILogService logService, IConfigService configService)
        {
            if (logService == null) throw new ArgumentNullException("logService");
            if (configService == null) throw new ArgumentNullException("configService");
            _logService = logService;
            _configService = configService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            _conversationHistory = new List<AIMessage>();
            
            // 设置默认系统提示词
            _systemPrompt = "你是一个专业的AI助手，请根据用户的问题提供准确、有用的回答。";
        }

        /// <summary>
        /// 回答问题
        /// </summary>
        public virtual async Task<AIResponse> AnswerQuestionAsync(string question, QuestionType questionType = QuestionType.Text)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("问题不能为空", "question");
            }

            if (!IsConfigured)
            {
                throw new InvalidOperationException("AI服务未配置");
            }

            try
            {
                _logService.Info("开始处理问题: {0}...", question.Substring(0, Math.Min(50, question.Length)));

                var response = await SendRequestWithRetryAsync(question);
                
                // 添加到对话历史
                _conversationHistory.Add(new AIMessage { Role = "user", Content = question });
                _conversationHistory.Add(new AIMessage { Role = "assistant", Content = response.Answer });

                // 触发响应事件
                if (ResponseReceived != null)
                {
                    ResponseReceived(this, response);
                }

                _logService.Info("问题处理完成，响应长度: {0}", response.Answer != null ? response.Answer.Length : 0);
                return response;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "回答问题失败: {0}", ex.Message);
                if (ApiError != null)
                {
                    ApiError(this, ex.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// 流式回答问题
        /// </summary>
        public virtual async Task<AIResponse> AnswerQuestionStreamAsync(string question, Action<string> onPartialResponse = null)
        {
            // 默认实现调用普通方法，子类可以重写实现真正的流式处理
            var response = await AnswerQuestionAsync(question);
            if (onPartialResponse != null)
            {
                onPartialResponse(response.Answer);
            }
            return response;
        }

        /// <summary>
        /// 分析问题类型
        /// </summary>
        public virtual async Task<QuestionType> AnalyzeQuestionTypeAsync(string question)
        {
            try
            {
                // 简单的问题类型分析逻辑
                if (question.Contains("选择") || question.Contains("A.") || question.Contains("B."))
                {
                    return QuestionType.MultipleChoice;
                }
                else if (question.Contains("判断") || question.Contains("对错") || question.Contains("正确"))
                {
                    return QuestionType.TrueFalse;
                }
                else if (question.Contains("填空") || question.Contains("_____"))
                {
                    return QuestionType.FillInBlank;
                }
                else
                {
                    return QuestionType.ShortAnswer;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "分析问题类型失败: {0}", ex.Message);
                return QuestionType.Unknown;
            }
        }

        /// <summary>
        /// 批量回答问题
        /// </summary>
        public virtual async Task<List<AIResponse>> AnswerQuestionsAsync(List<string> questions)
        {
            var responses = new List<AIResponse>();
            
            foreach (var question in questions)
            {
                try
                {
                    var response = await AnswerQuestionAsync(question);
                    responses.Add(response);
                    
                    // 添加延迟避免API限流
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "批量处理问题失败: {0}", ex.Message);
                    responses.Add(new AIResponse
                    {
                        Question = question,
                        Answer = string.Format("处理失败: {0}", ex.Message),
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            return responses;
        }

        /// <summary>
        /// 设置API配置
        /// </summary>
        public virtual void SetApiConfig(string apiToken, string baseUrl)
        {
            _apiToken = apiToken;
            _baseUrl = baseUrl;
            
            // 更新HttpClient的默认请求头
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_apiToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", _apiToken));
            }
            
            _logService.Info("AI服务配置已更新: {0}", baseUrl);
        }

        /// <summary>
        /// 测试API连接
        /// </summary>
        public abstract Task<bool> TestConnectionAsync();

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        public abstract Task<List<string>> GetAvailableModelsAsync();

        /// <summary>
        /// 设置使用的模型
        /// </summary>
        public virtual void SetModel(string model)
        {
            _currentModel = model;
            _logService.Info("AI模型已设置为: {0}", model);
        }

        /// <summary>
        /// 设置系统提示词
        /// </summary>
        public virtual void SetSystemPrompt(string systemPrompt)
        {
            _systemPrompt = systemPrompt ?? "你是一个专业的AI助手，请根据用户的问题提供准确、有用的回答。";
            _logService.Info("系统提示词已更新");
        }

        /// <summary>
        /// 清除对话历史
        /// </summary>
        public virtual void ClearHistory()
        {
            _conversationHistory.Clear();
            _logService.Info("对话历史已清除");
        }

        /// <summary>
        /// 发送请求并重试
        /// </summary>
        protected virtual async Task<AIResponse> SendRequestWithRetryAsync(string question)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                TimeSpan? delay = null;
                try
                {
                    _logService.Info("发送AI请求，第 {0} 次尝试", attempt);
                    return await SendRequestAsync(question);
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    if (attempt < _maxRetries)
                    {
                        delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 指数退避
                        _logService.Warn("请求失败，{0}秒后重试: {1}", delay.Value.TotalSeconds, ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    lastException = ex;
                    if (attempt < _maxRetries)
                    {
                        delay = TimeSpan.FromSeconds(1);
                        _logService.Warn("请求超时，重试第 {0} 次: {1}", attempt + 1, ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }

                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }
            }

            throw new Exception(string.Format("AI请求失败，已重试 {0} 次", _maxRetries), lastException);
        }

        /// <summary>
        /// 发送具体的API请求（由子类实现）
        /// </summary>
        protected abstract Task<AIResponse> SendRequestAsync(string question);

        /// <summary>
        /// 构建请求消息列表
        /// </summary>
        protected virtual List<AIMessage> BuildMessages(string question)
        {
            var messages = new List<AIMessage>();
            
            // 添加系统消息
            if (!string.IsNullOrEmpty(_systemPrompt))
            {
                messages.Add(new AIMessage { Role = "system", Content = _systemPrompt });
            }
            
            // 添加历史对话（保留最近的几轮对话）
            var recentHistory = _conversationHistory.Count > 10 
                ? _conversationHistory.GetRange(_conversationHistory.Count - 10, 10)
                : _conversationHistory;
            messages.AddRange(recentHistory);
            
            // 添加当前问题
            messages.Add(new AIMessage { Role = "user", Content = question });
            
            return messages;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _logService.Info("AI服务资源已释放");
            }
        }
    }

}
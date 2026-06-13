using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocris.Models;

namespace Ocris.Services
{


    /// <summary>
    /// AI服务接口
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// 回答问题
        /// </summary>
        /// <param name="question">问题文本</param>
        /// <param name="questionType">问题类型</param>
        /// <returns>AI响应</returns>
        Task<AIResponse> AnswerQuestionAsync(string question, QuestionType questionType = QuestionType.Text);

        /// <summary>
        /// 分析题目类型
        /// </summary>
        /// <param name="question">问题文本</param>
        /// <returns>题目类型</returns>
        Task<QuestionType> AnalyzeQuestionTypeAsync(string question);

        /// <summary>
        /// 批量回答问题
        /// </summary>
        /// <param name="questions">问题列表</param>
        /// <returns>回答列表</returns>
        Task<List<AIResponse>> AnswerQuestionsAsync(List<string> questions);

        /// <summary>
        /// 设置API配置
        /// </summary>
        /// <param name="apiToken">API Token</param>
        /// <param name="baseUrl">API基础URL</param>
        void SetApiConfig(string apiToken, string baseUrl);

        /// <summary>
        /// 测试API连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        /// <returns>模型列表</returns>
        Task<List<string>> GetAvailableModelsAsync();

        /// <summary>
        /// 设置使用的模型
        /// </summary>
        /// <param name="model">模型名称</param>
        void SetModel(string model);

        /// <summary>
        /// 获取当前模型
        /// </summary>
        string CurrentModel { get; }

        /// <summary>
        /// 是否已配置
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// 设置系统提示词
        /// </summary>
        /// <param name="systemPrompt">系统提示词</param>
        void SetSystemPrompt(string systemPrompt);

        /// <summary>
        /// 清除对话历史
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// AI响应事件
        /// </summary>
        event EventHandler<AIResponse> ResponseReceived;

        /// <summary>
        /// API错误事件
        /// </summary>
        event EventHandler<string> ApiError;

        /// <summary>
        /// 加载配置
        /// </summary>
        void LoadConfiguration();
    }
}
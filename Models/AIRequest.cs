using System;
using System.Collections.Generic;

namespace AIAnswerTool.Models
{
    /// <summary>
    /// AI请求模型
    /// </summary>
    public class AIRequest
    {
        /// <summary>
        /// 请求ID
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户问题
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// 使用的模型
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 系统提示词
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// 对话历史
        /// </summary>
        public List<AIMessage> Messages { get; set; } = new List<AIMessage>();

        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// 温度参数（控制随机性）
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Top-p参数
        /// </summary>
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public double FrequencyPenalty { get; set; } = 0.0;

        /// <summary>
        /// 存在惩罚
        /// </summary>
        public double PresencePenalty { get; set; } = 0.0;

        /// <summary>
        /// 是否流式输出
        /// </summary>
        public bool Stream { get; set; } = false;

        /// <summary>
        /// 请求时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 额外参数
        /// </summary>
        public Dictionary<string, object> ExtraParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 验证请求参数
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Question))
            {
                errorMessage = "问题不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Model))
            {
                errorMessage = "模型不能为空";
                return false;
            }

            if (MaxTokens <= 0)
            {
                errorMessage = "最大令牌数必须大于0";
                return false;
            }

            if (Temperature < 0 || Temperature > 2)
            {
                errorMessage = "温度参数必须在0-2之间";
                return false;
            }

            if (TopP < 0 || TopP > 1)
            {
                errorMessage = "Top-p参数必须在0-1之间";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 转换为JSON字符串（简化版本）
        /// </summary>
        public string ToJson()
        {
            return string.Format("{{\"RequestId\":\"{0}\",\"Question\":\"{1}\",\"Model\":\"{2}\"}}", RequestId, Question, Model);
        }

        /// <summary>
        /// 克隆请求对象
        /// </summary>
        public AIRequest Clone()
        {
            return new AIRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Question = this.Question,
                Model = this.Model,
                SystemPrompt = this.SystemPrompt,
                MaxTokens = this.MaxTokens,
                Temperature = this.Temperature,
                TopP = this.TopP,
                FrequencyPenalty = this.FrequencyPenalty,
                PresencePenalty = this.PresencePenalty,
                Stream = this.Stream,
                Timestamp = DateTime.Now,
                ExtraParameters = new Dictionary<string, object>(this.ExtraParameters)
            };
        }
    }

    /// <summary>
    /// AI消息模型
    /// </summary>
    public class AIMessage
    {
        /// <summary>
        /// 角色（system, user, assistant）
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息名称（可选）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 函数调用信息（可选）
        /// </summary>
        public object FunctionCall { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 创建系统消息
        /// </summary>
        public static AIMessage CreateSystemMessage(string content)
        {
            return new AIMessage
            {
                Role = "system",
                Content = content
            };
        }

        /// <summary>
        /// 创建用户消息
        /// </summary>
        public static AIMessage CreateUserMessage(string content)
        {
            return new AIMessage
            {
                Role = "user",
                Content = content
            };
        }

        /// <summary>
        /// 创建助手消息
        /// </summary>
        public static AIMessage CreateAssistantMessage(string content)
        {
            return new AIMessage
            {
                Role = "assistant",
                Content = content
            };
        }

        /// <summary>
        /// 验证消息
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Role) && 
                   !string.IsNullOrWhiteSpace(Content) &&
                   (Role == "system" || Role == "user" || Role == "assistant");
        }

        /// <summary>
        /// 获取消息摘要
        /// </summary>
        public string GetSummary(int maxLength = 50)
        {
            if (string.IsNullOrEmpty(Content))
                return "[空消息]";

            if (Content.Length <= maxLength)
                return Content;

            return Content.Substring(0, maxLength) + "...";
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Role, GetSummary());
        }
    }

    /// <summary>
    /// OpenAI格式的请求模型
    /// </summary>
    public class OpenAIRequest
    {
        public string Model { get; set; }

        public List<AIMessage> Messages { get; set; }

        public int MaxTokens { get; set; }

        public double Temperature { get; set; }

        public double TopP { get; set; }

        public double FrequencyPenalty { get; set; }

        public double PresencePenalty { get; set; }

        public bool Stream { get; set; }

        /// <summary>
        /// 从AIRequest转换
        /// </summary>
        public static OpenAIRequest FromAIRequest(AIRequest request)
        {
            return new OpenAIRequest
            {
                Model = request.Model,
                Messages = request.Messages,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stream = request.Stream
            };
        }
    }
}
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ocris.Models
{
    /// <summary>
    /// AI响应模型
    /// </summary>
    public class AIResponse
    {
        public AIResponse()
        {
            ResponseId = Guid.NewGuid().ToString();
            IsSuccess = true;
            Timestamp = DateTime.Now;
            Metadata = new Dictionary<string, object>();
            SuggestedQuestions = new List<string>();
            Sources = new List<string>();
        }

        /// <summary>
        /// 响应ID
        /// </summary>
        public string ResponseId { get; set; }

        /// <summary>
        /// 对应的请求ID
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// 原始问题
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// AI回答
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 使用的模型
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 响应时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 使用的令牌数
        /// </summary>
        public TokenUsage TokenUsage { get; set; }

        /// <summary>
        /// 置信度分数（0-1）
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// 问题类型
        /// </summary>
        public QuestionType QuestionType { get; set; }

        /// <summary>
        /// 回答质量评分（1-5）
        /// </summary>
        public int QualityScore { get; set; }

        /// <summary>
        /// 相关性评分（1-5）
        /// </summary>
        public int RelevanceScore { get; set; }

        /// <summary>
        /// 完整性评分（1-5）
        /// </summary>
        public int CompletenessScore { get; set; }

        /// <summary>
        /// 额外元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// 原始API响应
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// 建议的后续问题
        /// </summary>
        public List<string> SuggestedQuestions { get; set; }

        /// <summary>
        /// 引用来源
        /// </summary>
        public List<string> Sources { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static AIResponse CreateSuccess(string question, string answer, string model = null)
        {
            return new AIResponse
            {
                Question = question,
                Answer = answer,
                Model = model,
                IsSuccess = true
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static AIResponse CreateError(string question, string errorMessage, string errorCode = null)
        {
            return new AIResponse
            {
                Question = question,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                IsSuccess = false
            };
        }

        /// <summary>
        /// 设置处理时间
        /// </summary>
        public void SetProcessingTime(DateTime startTime)
        {
            ProcessingTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        /// <summary>
        /// 获取总体评分
        /// </summary>
        public double GetOverallScore()
        {
            if (QualityScore == 0 && RelevanceScore == 0 && CompletenessScore == 0)
                return 0;
            return (QualityScore + RelevanceScore + CompletenessScore) / 3.0;
        }

        /// <summary>
        /// 验证响应
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Question))
                return false;
            
            if (IsSuccess && string.IsNullOrWhiteSpace(Answer))
                return false;
                
            if (!IsSuccess && string.IsNullOrWhiteSpace(ErrorMessage))
                return false;
                
            return true;
        }

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// 从JSON字符串创建响应对象
        /// </summary>
        public static AIResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<AIResponse>(json);
        }

        /// <summary>
        /// 获取响应摘要
        /// </summary>
        public string GetSummary()
        {
            if (!IsSuccess)
                return string.Format("错误: {0}", ErrorMessage);

            var answerPreview = (Answer != null && Answer.Length > 100) ? Answer.Substring(0, 100) + "..." : Answer;
            return string.Format("回答: {0} (耗时: {1}ms)", answerPreview, ProcessingTimeMs);
        }

        /// <summary>
        /// 添加元数据
        /// </summary>
        public void AddMetadata(string key, object value)
        {
            Metadata[key] = value;
        }

        /// <summary>
        /// 获取元数据
        /// </summary>
        public T GetMetadata<T>(string key, T defaultValue)
        {
            object value;
            if (Metadata.TryGetValue(key, out value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public T GetMetadata<T>(string key)
        {
            return GetMetadata<T>(key, default(T));
        }

        public override string ToString()
        {
            return GetSummary();
        }
    }

    /// <summary>
    /// 令牌使用情况
    /// </summary>
    public class TokenUsage
    {
        /// <summary>
        /// 提示令牌数
        /// </summary>
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// 完成令牌数
        /// </summary>
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总令牌数
        /// </summary>
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// 估算成本（美元）
        /// </summary>
        public double EstimatedCost { get; set; }

        /// <summary>
        /// 计算总令牌数
        /// </summary>
        public void CalculateTotal()
        {
            TotalTokens = PromptTokens + CompletionTokens;
        }

        /// <summary>
        /// 估算成本（基于GPT-3.5-turbo价格）
        /// </summary>
        public void EstimateCost(double inputCostPer1K = 0.0015, double outputCostPer1K = 0.002)
        {
            EstimatedCost = (PromptTokens / 1000.0 * inputCostPer1K) + 
                           (CompletionTokens / 1000.0 * outputCostPer1K);
        }

        public override string ToString()
        {
            return string.Format("Tokens: {0} (提示: {1}, 完成: {2})", TotalTokens, PromptTokens, CompletionTokens);
        }
    }

    /// <summary>
    /// OpenAI格式的响应模型
    /// </summary>
    public class OpenAIResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }

        [JsonProperty("usage")]
        public TokenUsage Usage { get; set; }

        /// <summary>
        /// 转换为AIResponse
        /// </summary>
        public AIResponse ToAIResponse(string question = null)
        {
            var response = new AIResponse
            {
                ResponseId = Id,
                Question = question,
                Model = Model,
                TokenUsage = Usage,
                RawResponse = JsonConvert.SerializeObject(this)
            };

            if (Choices != null && Choices.Count > 0)
            {
                var choice = Choices[0];
                response.Answer = choice.Message != null ? choice.Message.Content : null;
                response.IsSuccess = choice.FinishReason != "error";
                
                if (choice.FinishReason == "error")
                {
                    response.ErrorMessage = "API返回错误";
                }
            }
            else
            {
                response.IsSuccess = false;
                response.ErrorMessage = "没有返回有效的选择";
            }

            return response;
        }
    }

    /// <summary>
    /// 选择项
    /// </summary>
    public class Choice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("message")]
        public AIMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class AIMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

}
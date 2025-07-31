using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AIAnswerTool.Models;
using System.Configuration;

namespace AIAnswerTool.Services
{
    public class AliCloudAIService : AIService
    {
        private string _aliCloudApiKey;
        private string _aliCloudBaseUrl;

        public AliCloudAIService(ILogService logService, IConfigService configService) : base(logService, configService)
        {
            LoadConfiguration();
        }

        public override bool IsConfigured 
        {
            get 
            {
                bool configured = !string.IsNullOrEmpty(_aliCloudApiKey) && !string.IsNullOrEmpty(_aliCloudBaseUrl);
                _logService.Info("AliCloudAIService.IsConfigured被调用: ApiKey={0}, BaseUrl={1}, Result={2}", 
                    string.IsNullOrEmpty(_aliCloudApiKey) ? "空" : "有值", 
                    string.IsNullOrEmpty(_aliCloudBaseUrl) ? "空" : "有值", 
                    configured);
                return configured;
            }
        }

        public void LoadConfiguration()
        {
            try
            {
                _aliCloudApiKey = _configService.AliCloudApiKey ?? "";
                _aliCloudBaseUrl = _configService.AliCloudApiBaseUrl ?? "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation";
                _logService.Info("阿里云配置加载: ApiKey={0}, BaseUrl={1}, IsConfigured={2}", 
                    string.IsNullOrEmpty(_aliCloudApiKey) ? "未设置" : "已设置", 
                    _aliCloudBaseUrl,
                    IsConfigured);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "加载阿里云配置失败");
                _aliCloudApiKey = "";
                _aliCloudBaseUrl = "";
            }
        }

        public override void SetApiConfig(string apiToken, string baseUrl)
        {
            _aliCloudApiKey = apiToken;
            _aliCloudBaseUrl = baseUrl;
        }

        public override async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testResponse = await AnswerQuestionAsync("你好", QuestionType.Text);
                return testResponse.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        public override async Task<List<string>> GetAvailableModelsAsync()
        {
            return new List<string>
            {
                "qwen-turbo",
                "qwen-plus",
                "qwen-max",
                "qwen-max-1201",
                "qwen-max-longcontext"
            };
        }

        protected override async Task<AIResponse> SendRequestAsync(string question)
        {
            _logService.Info("SendRequestAsync被调用: ApiKey={0}, BaseUrl={1}, IsConfigured={2}", 
                string.IsNullOrEmpty(_aliCloudApiKey) ? "空" : "有值", 
                string.IsNullOrEmpty(_aliCloudBaseUrl) ? "空" : "有值", 
                IsConfigured);
            
            if (!IsConfigured)
            {
                return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = "阿里云AI服务未配置",
                Answer = "",
                TokenUsage = new TokenUsage { CompletionTokens = 0, TotalTokens = 0 }
            };
            }

            try
            {
                var models = await GetAvailableModelsAsync();
                var selectedModel = models.Count > 0 ? models[0] : "qwen-turbo";
                
                var request = BuildAliCloudRequest(question, selectedModel, false);
                var jsonContent = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _aliCloudApiKey);

                var apiUrl = _aliCloudBaseUrl.TrimEnd('/') + "/chat/completions";
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var openAIResponse = JsonConvert.DeserializeObject<OpenAICompatibleResponse>(responseContent);
                    
                    if (openAIResponse != null && openAIResponse.Choices != null && openAIResponse.Choices.Length > 0)
                    {
                        var choice = openAIResponse.Choices[0];
                        return new AIResponse
                        {
                            IsSuccess = true,
                            Answer = choice.Message != null ? choice.Message.Content ?? "" : "",
                            ErrorMessage = "",
                            TokenUsage = new TokenUsage 
                            { 
                                CompletionTokens = openAIResponse.Usage != null ? openAIResponse.Usage.CompletionTokens : 0,
                                TotalTokens = openAIResponse.Usage != null ? openAIResponse.Usage.TotalTokens : 0
                            }
                        };
                    }
                    else
                    {
                        return new AIResponse
                        {
                            IsSuccess = false,
                            ErrorMessage = "API返回格式异常",
                            Answer = "",
                            TokenUsage = new TokenUsage { CompletionTokens = 0, TotalTokens = 0 }
                        };
                    }
                }
                else
                {
                    return new AIResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "API请求失败: " + response.StatusCode + " - " + responseContent,
                        Answer = "",
                        TokenUsage = new TokenUsage { CompletionTokens = 0, TotalTokens = 0 }
                    };
                }
            }
            catch (Exception ex)
            {
                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "请求异常: " + ex.Message,
                    Answer = "",
                    TokenUsage = new TokenUsage { CompletionTokens = 0, TotalTokens = 0 }
                };
            }
        }

        private object BuildAliCloudRequest(string question, string model, bool stream)
        {
            var request = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = question
                    }
                },
                max_tokens = 2000,
                temperature = 0.85,
                top_p = 0.8,
                stream = stream
            };

            return request;
        }
    }

    public class AliCloudRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("input")]
        public AliCloudInput Input { get; set; }

        [JsonProperty("parameters")]
        public AliCloudParameters Parameters { get; set; }
    }

    public class AliCloudInput
    {
        [JsonProperty("messages")]
        public List<AliCloudMessage> Messages { get; set; }
    }

    public class AliCloudMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class AliCloudParameters
    {
        [JsonProperty("result_format")]
        public string ResultFormat { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("top_p")]
        public double TopP { get; set; }

        [JsonProperty("top_k")]
        public int TopK { get; set; }

        [JsonProperty("repetition_penalty")]
        public double RepetitionPenalty { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("stop")]
        public string[] Stop { get; set; }

        [JsonProperty("enable_search")]
        public bool EnableSearch { get; set; }

        [JsonProperty("incremental_output")]
        public bool IncrementalOutput { get; set; }
    }

    public class AliCloudResponse
    {
        [JsonProperty("output")]
        public AliCloudOutput Output { get; set; }

        [JsonProperty("usage")]
        public AliCloudUsage Usage { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }

    public class AliCloudOutput
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class AliCloudUsage
    {
        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }

        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    // OpenAI Compatible Response Classes
    public class OpenAICompatibleResponse
    {
        [JsonProperty("choices")]
        public OpenAIChoice[] Choices { get; set; }

        [JsonProperty("usage")]
        public OpenAIUsage Usage { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonProperty("message")]
        public OpenAIMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
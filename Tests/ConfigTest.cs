using System;
using System.IO;
using System.Threading.Tasks;
using Ocris.Services;

namespace Ocris.Tests
{
    class ConfigTest
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("开始配置测试...");
                
                // 创建LogService
                var logService = new LogService();
                await logService.InitializeAsync();
                
                // 创建ConfigService
                var configService = new ConfigService(logService);
                await configService.InitializeAsync();
                
                Console.WriteLine("配置服务初始化完成");
                Console.WriteLine($"AliCloudApiKey: {configService.AliCloudApiKey}");
                Console.WriteLine($"AliCloudApiBaseUrl: {configService.AliCloudApiBaseUrl}");
                
                // 创建AliCloudAIService
                var aiService = new AliCloudAIService(logService, configService);
                Console.WriteLine($"AI服务配置状态: {aiService.IsConfigured}");
                
                // 测试AI服务
                if (aiService.IsConfigured)
                {
                    Console.WriteLine("开始测试AI服务连接...");
                    var testResult = await aiService.TestConnectionAsync();
                    Console.WriteLine($"AI服务连接测试结果: {testResult}");
                }
                else
                {
                    Console.WriteLine("AI服务未配置，无法进行连接测试");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
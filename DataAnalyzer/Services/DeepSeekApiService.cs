using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAnalyzer.Services
{
    public class DeepSeekApiService : BaseApiService
    {
        public DeepSeekApiService(ApiProviderConfig config) : base(config)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 初始化，模型: {config.Model}");
        }

        public override async Task<string> GenerateTextAsync(string prompt)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 开始生成文本，prompt长度: {prompt.Length}");
            
            if (!Config.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("[DeepSeekApiService] 配置无效");
                return "（AI分析未启用：请先配置API Key和URL）";
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Config.ApiUrl);
                request.Headers.Add("Authorization", $"Bearer {Config.ApiKey}");
                
                var body = new
                {
                    model = Config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = PromptTemplates.SystemPrompt },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 4096
                };

                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 发送请求到: {Config.ApiUrl}");
                
                var response = await GetHttpClient().SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 响应状态码: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 响应长度: {responseBody.Length}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] HTTP错误: {response.StatusCode} - {responseBody}");
                    return $"（AI请求失败: {response.StatusCode}）";
                }

                var content = ExtractContentFromResponse(responseBody);
                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 提取内容长度: {content.Length}");
                
                return content;
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[DeepSeekApiService] 请求超时");
                return "（AI请求超时，请检查网络连接）";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DeepSeekApiService] 堆栈: {ex.StackTrace}");
                return $"（AI分析异常：{ex.Message}）";
            }
        }
    }
}

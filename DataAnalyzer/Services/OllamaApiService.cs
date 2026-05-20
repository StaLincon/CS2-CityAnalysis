using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAnalyzer.Services
{
    public class OllamaApiService : BaseApiService
    {
        public OllamaApiService(ApiProviderConfig config) : base(config) { }

        public override async Task<string> GenerateTextAsync(string prompt)
        {
            if (string.IsNullOrEmpty(Config.ApiUrl))
                return "（AI分析未启用：请先配置API URL）";

            try
            {
                var url = $"{Config.ApiUrl.TrimEnd('/')}/api/chat";
                
                var body = new
                {
                    model = Config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = PromptTemplates.SystemPrompt },
                        new { role = "user", content = prompt }
                    },
                    stream = false
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

                var response = await GetHttpClient().SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"（AI请求失败：{response.StatusCode}）\n{responseBody}";

                return ExtractContentFromResponse(responseBody);
            }
            catch (TaskCanceledException)
            {
                return "（AI请求超时，请检查网络连接）";
            }
            catch (System.Exception ex)
            {
                return $"（AI分析异常：{ex.Message}）";
            }
        }
    }
}
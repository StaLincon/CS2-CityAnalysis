using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalyzer.Services
{
    public class AzureOpenAiApiService : BaseApiService
    {
        public AzureOpenAiApiService(ApiProviderConfig config) : base(config) { }

        public override async Task<string> GenerateTextAsync(string prompt)
        {
            if (!Config.IsValid)
                return "（AI分析未启用：请先配置API Key和URL）";

            try
            {
                var deploymentName = string.IsNullOrEmpty(Config.DeploymentName) ? Config.Model : Config.DeploymentName;
                var url = $"{Config.ApiUrl}/openai/deployments/{deploymentName}/chat/completions?api-version={Config.ApiVersion}";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("api-key", Config.ApiKey);
                request.Content = BuildRequestBody(prompt);

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
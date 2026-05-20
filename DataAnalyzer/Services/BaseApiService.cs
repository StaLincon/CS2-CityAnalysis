using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public abstract class BaseApiService : IApiService
    {
        protected readonly ApiProviderConfig Config;
        private HttpClient m_Http;
        private string m_LastProxyUrl;

        protected BaseApiService(ApiProviderConfig config)
        {
            Config = config;
            System.Diagnostics.Debug.WriteLine($"[BaseApiService] 初始化完成，提供商类型: {config.ProviderType}");
        }

        protected HttpClient GetHttpClient()
        {
            if (m_Http == null || m_LastProxyUrl != Config.ProxyUrl)
            {
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 创建HttpClient，代理URL: {(string.IsNullOrEmpty(Config.ProxyUrl) ? "无" : Config.ProxyUrl)}");
                
                var handler = new HttpClientHandler();
                
                System.Net.ServicePointManager.SecurityProtocol = 
                    System.Net.SecurityProtocolType.Tls12 | 
                    System.Net.SecurityProtocolType.Tls13;
                System.Diagnostics.Debug.WriteLine("[BaseApiService] 已设置TLS 1.2/1.3协议");
                
                if (!string.IsNullOrEmpty(Config.ProxyUrl))
                {
                    try
                    {
                        handler.Proxy = new System.Net.WebProxy(Config.ProxyUrl);
                        handler.UseProxy = true;
                        System.Diagnostics.Debug.WriteLine($"[BaseApiService] 已配置代理: {Config.ProxyUrl}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BaseApiService] 代理配置失败: {ex.Message}");
                    }
                }
                else
                {
                    handler.UseDefaultCredentials = true;
                    System.Diagnostics.Debug.WriteLine("[BaseApiService] 使用默认凭据");
                }
                
                m_Http?.Dispose();
                m_Http = new HttpClient(handler);
                m_Http.Timeout = TimeSpan.FromSeconds(180);
                m_LastProxyUrl = Config.ProxyUrl;
                
                System.Diagnostics.Debug.WriteLine("[BaseApiService] HttpClient创建完成");
            }
            return m_Http;
        }

        public abstract Task<string> GenerateTextAsync(string prompt);

        public virtual async Task<ConnectionTestResult> TestConnectionAsync()
        {
            var startTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"[BaseApiService] 开始测试连接，目标URL: {Config.ApiUrl}");
            System.Diagnostics.Debug.WriteLine($"[BaseApiService] 使用模型: {Config.Model}");
            
            try
            {
                var httpClient = GetHttpClient();
                System.Diagnostics.Debug.WriteLine("[BaseApiService] 获取HttpClient成功");
                
                var request = new HttpRequestMessage(HttpMethod.Post, Config.ApiUrl);
                request.Headers.Add("Authorization", $"Bearer {Config.ApiKey}");
                
                var body = new
                {
                    model = Config.Model,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    },
                    max_tokens = 5
                };

                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                
                System.Diagnostics.Debug.WriteLine("[BaseApiService] 发送测试请求...");
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 请求体: {json}");
                
                var response = await httpClient.SendAsync(request);
                var latency = (DateTime.Now - startTime).TotalMilliseconds;
                
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 响应状态码: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 请求延迟: {latency:F2}ms");
                
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 响应内容: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("[BaseApiService] 连接测试成功");
                    return new ConnectionTestResult
                    {
                        Success = true,
                        Latency = (int)latency,
                        ModelInfo = Config.Model
                    };
                }
                
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] HTTP错误: {response.StatusCode}");
                
                return new ConnectionTestResult
                {
                    Success = false,
                    Latency = (int)latency,
                    ErrorMessage = $"HTTP错误: {response.StatusCode} - {responseBody.Substring(0, Math.Min(200, responseBody.Length))}"
                };
            }
            catch (Exception ex)
            {
                var latency = (DateTime.Now - startTime).TotalMilliseconds;
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 连接测试异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 异常堆栈: {ex.StackTrace}");
                
                return new ConnectionTestResult
                {
                    Success = false,
                    Latency = (int)latency,
                    ErrorMessage = ex.Message
                };
            }
        }

        public virtual async Task<List<ModelInfo>> FetchModelsAsync()
        {
            System.Diagnostics.Debug.WriteLine("[BaseApiService] 开始获取模型列表");
            
            var result = new List<ModelInfo>();
            
            try
            {
                var baseUrl = Config.ApiUrl;
                var uri = new Uri(baseUrl);
                var modelsUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/v1/models";
                
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 模型列表URL: {modelsUrl}");
                
                var httpClient = GetHttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
                request.Headers.Add("Authorization", $"Bearer {Config.ApiKey}");
                
                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 模型列表响应状态码: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        var dataList = doc.RootElement.GetProperty("data");
                        foreach (var item in dataList.EnumerateArray())
                        {
                            result.Add(new ModelInfo
                            {
                                Id = item.GetProperty("id").GetString() ?? "",
                                OwnedBy = item.TryGetProperty("owned_by", out var own) ? own.GetString() ?? "" : "",
                                Created = item.TryGetProperty("created", out var cr) ? cr.GetInt64() : 0
                            });
                        }
                        System.Diagnostics.Debug.WriteLine($"[BaseApiService] 成功获取{result.Count}个模型");
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BaseApiService] 解析模型列表失败: {parseEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"[BaseApiService] 原始响应: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BaseApiService] 获取模型列表HTTP错误: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[BaseApiService] 响应: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BaseApiService] 获取模型列表异常: {ex.Message}");
            }

            if (result.Count == 0)
            {
                result.Add(new ModelInfo { Id = Config.Model, OwnedBy = "默认（当前配置）" });
            }

            return result;
        }

        public async Task<List<ReportChapter>> GenerateReportChaptersAsync(
            CityAnalysisReport analysis,
            StatisticSnapshot current,
            List<StatisticSnapshot> history,
            string cityName,
            IProgress<(string chapterId, string title, int current, int total)> progress,
            ReportTemplate template = ReportTemplate.Development)
        {
            var engine = new AnalysisEngine(current, history, analysis.Overview.KUpdatesPerDay);
            var devReport = engine.AnalyzeDevelopment();
            var ctx = DevelopmentPrompts.BuildDevelopmentContext(devReport, cityName);
            return await GenerateDevelopmentChapters(devReport, ctx, cityName, progress);
        }

        private async Task<List<ReportChapter>> GenerateDevelopmentChapters(
            DevelopmentReport devReport, string ctx, string cityName,
            IProgress<(string chapterId, string title, int current, int total)> progress)
        {
            var chapters = new List<ReportChapter>();
            var defs = new List<(string id, string title, Func<string, string> promptFn)>
            {
                ("opening", "开场白与建市以来总体回顾", c => DevelopmentPrompts.GetOpeningPrompt(cityName, c)),
                ("demographics", "人口发展与城镇化建设", DevelopmentPrompts.GetDemographicsPrompt),
                ("economy", "经济发展与财政运行", DevelopmentPrompts.GetEconomyPrompt),
                ("industry", "产业体系构建与优化升级", DevelopmentPrompts.GetIndustryPrompt),
                ("employment", "就业促进与社会保障", DevelopmentPrompts.GetEmploymentPrompt),
                ("transport", "基础设施建设与交通发展", DevelopmentPrompts.GetTransportPrompt),
                ("social", "民生福祉与社会事业", DevelopmentPrompts.GetSocialPrompt),
                ("fiscal", "财政管理与家庭经济", DevelopmentPrompts.GetFiscalPrompt),
                ("challenges", "面临的问题与挑战", DevelopmentPrompts.GetChallengesPrompt),
                ("outlook", "下一阶段发展目标与工作部署", DevelopmentPrompts.GetOutlookPrompt),
            };

            for (int i = 0; i < defs.Count; i++)
            {
                var (id, title, promptFn) = defs[i];
                progress?.Report((id, title, i + 1, defs.Count));
                var prompt = promptFn(ctx);
                var content = await GenerateTextAsync(prompt);
                chapters.Add(new ReportChapter { Id = id, Title = title, Content = content });
            }

            return chapters;
        }

        

        protected string ExtractContentFromResponse(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var msg = choices[0].GetProperty("message");
                    return msg.GetProperty("content").GetString() ?? "";
                }
                return "（AI未返回有效内容）";
            }
            catch
            {
                return "（AI返回格式异常）";
            }
        }

        protected StringContent BuildRequestBody(string userPrompt)
        {
            var body = new
            {
                model = Config.Model,
                messages = new[]
                {
                    new { role = "system", content = PromptTemplates.SystemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(body);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }

    public class ConnectionTestResult
    {
        public bool Success { get; set; }
        public int Latency { get; set; }
        public string ModelInfo { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}
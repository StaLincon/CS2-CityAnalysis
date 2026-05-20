using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public class ReportChapter
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }

    public class LlmService
    {
        private HttpClient m_Http;
        private string m_LastProxyUrl;

        public string ApiKey { get; set; }
        public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string Model { get; set; } = "gpt-4o";
        public string ProxyUrl { get; set; }

        public LlmService()
        {
        }

        private HttpClient GetHttpClient()
        {
            // 如果HttpClient不存在或代理URL发生变化，重新创建
            if (m_Http == null || m_LastProxyUrl != ProxyUrl)
            {
                var handler = new HttpClientHandler();
                
                if (!string.IsNullOrEmpty(ProxyUrl))
                {
                    try
                    {
                        handler.Proxy = new System.Net.WebProxy(ProxyUrl);
                        handler.UseProxy = true;
                    }
                    catch
                    {
                        // 忽略代理配置错误，继续使用无代理连接
                    }
                }
                
                m_Http?.Dispose();
                m_Http = new HttpClient(handler);
                m_Http.Timeout = TimeSpan.FromSeconds(180);
                m_LastProxyUrl = ProxyUrl;
            }
            return m_Http;
        }

        public async Task<List<ReportChapter>> GenerateAllChapters(
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
                var content = await SendAsync(DevelopmentPrompts.SystemPrompt, prompt);
                chapters.Add(new ReportChapter { Id = id, Title = title, Content = content });
            }

            return chapters;
        }

        public async Task<string> SendAsync(string userPrompt, string systemPromptOverride = null)
        {
            if (string.IsNullOrEmpty(ApiKey))
                return "（AI分析未启用：请先配置API Key）";

            var systemPrompt = systemPromptOverride ?? PromptTemplates.SystemPrompt;

            var body = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Add("Authorization", $"Bearer {ApiKey}");
                request.Content = content;

                var response = await GetHttpClient().SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"（AI请求失败：{response.StatusCode}）\n{responseBody}";

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
            catch (TaskCanceledException)
            {
                return "（AI请求超时，请检查网络连接或增加超时时间）";
            }
            catch (Exception ex)
            {
                return $"（AI分析异常：{ex.Message}）";
            }
        }
    }
}

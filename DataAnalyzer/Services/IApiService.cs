using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAnalyzer.Models;

namespace DataAnalyzer.Services
{
    public interface IApiService
    {
        Task<string> GenerateTextAsync(string prompt);
        
        Task<List<ReportChapter>> GenerateReportChaptersAsync(
            CityAnalysisReport analysis,
            StatisticSnapshot current,
            List<StatisticSnapshot> history,
            string cityName,
            IProgress<(string chapterId, string title, int current, int total)> progress,
            ReportTemplate template = ReportTemplate.Development);

        Task<ConnectionTestResult> TestConnectionAsync();

        Task<List<ModelInfo>> FetchModelsAsync();
    }

    public class ModelInfo
    {
        public string Id { get; set; } = "";
        public string OwnedBy { get; set; } = "";
        public long Created { get; set; }
    }
}
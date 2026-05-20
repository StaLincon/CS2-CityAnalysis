using System.IO;
using System.Text.Json;

namespace DataAnalyzer.Models
{
    public class AppConfig
    {
        public string DataPath { get; set; }
        public string CityName { get; set; } = "My City";
        public string SelectedSaveFolder { get; set; } = "";
        public LlmConfig Llm { get; set; } = new();

        private static readonly string DefaultDataPath =
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "Cities Skylines II", "analysis");

        public string GetEffectiveDataPath() =>
            string.IsNullOrEmpty(DataPath) ? DefaultDataPath : DataPath;

        public static AppConfig Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (cfg != null) return cfg;
                }
            }
            catch { }
            return new AppConfig();
        }

        public void Save(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public class LlmConfig
    {
        public string ProviderType { get; set; } = "OpenAI";
        public string ApiKey { get; set; } = "";
        public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string Model { get; set; } = "gpt-4o";
        public string ProxyUrl { get; set; } = "";
        public string ApiVersion { get; set; } = "2024-02-15-preview";
        public string DeploymentName { get; set; } = "";
        public bool Enabled => !string.IsNullOrEmpty(ApiKey);
    }
}

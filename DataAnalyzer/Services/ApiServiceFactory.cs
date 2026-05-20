using System;
using System.Collections.Generic;

namespace DataAnalyzer.Services
{
    public static class ApiServiceFactory
    {
        public static IApiService Create(ApiProviderConfig config)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiServiceFactory] 创建API服务: {config.ProviderType}");
            System.Diagnostics.Debug.WriteLine($"[ApiServiceFactory] API URL: {config.ApiUrl}");
            System.Diagnostics.Debug.WriteLine($"[ApiServiceFactory] 模型: {config.Model}");
            
            return config.ProviderType switch
            {
                ApiProviderType.OpenAI => new OpenAiApiService(config),
                ApiProviderType.AzureOpenAI => new AzureOpenAiApiService(config),
                ApiProviderType.Ollama => new OllamaApiService(config),
                ApiProviderType.DeepSeek => new DeepSeekApiService(config),
                ApiProviderType.SiliconFlow => new SiliconFlowApiService(config),
                ApiProviderType.Custom => new CustomApiService(config),
                _ => throw new NotSupportedException($"不支持的API提供商: {config.ProviderType}")
            };
        }

        public static IEnumerable<(string Name, ApiProviderType Type)> GetSupportedProviders()
        {
            yield return ("OpenAI", ApiProviderType.OpenAI);
            yield return ("Azure OpenAI", ApiProviderType.AzureOpenAI);
            yield return ("DeepSeek", ApiProviderType.DeepSeek);
            yield return ("SiliconFlow 硅基流动", ApiProviderType.SiliconFlow);
            yield return ("Ollama", ApiProviderType.Ollama);
            yield return ("自定义", ApiProviderType.Custom);
        }
    }

    public enum ApiProviderType
    {
        OpenAI,
        AzureOpenAI,
        DeepSeek,
        SiliconFlow,
        Ollama,
        Custom
    }

    public class ApiProviderConfig
    {
        public ApiProviderType ProviderType { get; set; } = ApiProviderType.OpenAI;
        
        public string ApiKey { get; set; } = "";
        
        public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
        
        public string Model { get; set; } = "gpt-4o";
        
        public string ProxyUrl { get; set; } = "";
        
        public string ApiVersion { get; set; } = "2024-02-15-preview";
        
        public string DeploymentName { get; set; } = "";

        public bool IsValid => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ApiUrl);
    }
}
namespace OPZManager.API.Services
{
    public class LlmSettingsDto
    {
        public string Provider { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
    }

    public class EmbeddingSettingsDto
    {
        public string Provider { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int Dimensions { get; set; }
    }

    public interface ISystemConfigService
    {
        Task<string> GetAsync(string key, string defaultValue = "");
        Task SetAsync(string key, string value, string category = "");
        Task<LlmSettingsDto> GetLlmSettingsAsync();
        Task<EmbeddingSettingsDto> GetEmbeddingSettingsAsync();
        Task UpdateLlmSettingsAsync(LlmSettingsDto dto);
        Task UpdateEmbeddingSettingsAsync(EmbeddingSettingsDto dto);
    }
}

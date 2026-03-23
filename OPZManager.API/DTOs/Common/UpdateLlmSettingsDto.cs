namespace OPZManager.API.DTOs.Common
{
    public class UpdateLlmSettingsDto
    {
        public string Provider { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? ModelName { get; set; }
    }
}

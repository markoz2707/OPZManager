namespace OPZManager.API.DTOs.Admin
{
    public class LlmLogSummaryDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string CallerMethod { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
    }

    public class LlmLogDetailDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string CallerMethod { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int MaxTokensRequested { get; set; }
        public double Temperature { get; set; }
    }
}

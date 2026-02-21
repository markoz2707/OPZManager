using System.Text;
using System.Text.Json;

namespace OPZManager.API.Services.LLM
{
    public class AnthropicProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AnthropicProvider> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;

        public string ProviderName => "Anthropic Claude";
        public string ModelName => _modelName;

        public AnthropicProvider(HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["LlmSettings:Anthropic:ApiKey"] ?? string.Empty;
            _modelName = configuration["LlmSettings:Anthropic:ModelName"] ?? "claude-sonnet-4-20250514";

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Anthropic API key is not configured in LlmSettings:Anthropic:ApiKey");
            }
        }

        public async Task<string> SendChatAsync(string systemPrompt, string userPrompt, int maxTokens = 2000, double temperature = 0.7)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = _modelName,
                max_tokens = maxTokens,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API request failed: {StatusCode} - {Error}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Anthropic API request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
            {
                var firstBlock = contentArray[0];
                if (firstBlock.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }

            throw new InvalidOperationException("Invalid response format from Anthropic API");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Cannot test Anthropic connection: API key not configured");
                    return false;
                }

                var response = await SendChatAsync(
                    "You are a test assistant.",
                    "Test. Respond OK.",
                    maxTokens: 10,
                    temperature: 0);
                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anthropic API connection test failed");
                return false;
            }
        }
    }
}

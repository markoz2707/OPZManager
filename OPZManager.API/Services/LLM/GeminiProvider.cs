using System.Text;
using System.Text.Json;

namespace OPZManager.API.Services.LLM
{
    public class GeminiProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiProvider> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;

        public string ProviderName => "Google Gemini";
        public string ModelName => _modelName;

        public GeminiProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["LlmSettings:Gemini:ApiKey"] ?? string.Empty;
            _modelName = configuration["LlmSettings:Gemini:ModelName"] ?? "gemini-2.0-flash";

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured in LlmSettings:Gemini:ApiKey");
            }
        }

        public async Task<string> SendChatAsync(string systemPrompt, string userPrompt, int maxTokens = 2000, double temperature = 0.7)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxTokens,
                    temperature
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API request failed: {StatusCode} - {Error}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Gemini API request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var candidateContent) &&
                    candidateContent.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }

            throw new InvalidOperationException("Invalid response format from Gemini API");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Cannot test Gemini connection: API key not configured");
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
                _logger.LogError(ex, "Gemini API connection test failed");
                return false;
            }
        }
    }
}

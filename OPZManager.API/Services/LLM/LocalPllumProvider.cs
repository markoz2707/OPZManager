using System.Text;
using System.Text.Json;

namespace OPZManager.API.Services.LLM
{
    public class LocalPllumProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalPllumProvider> _logger;
        private readonly string _modelName;

        public string ProviderName => "Local (Pllum)";
        public string ModelName => _modelName;

        public LocalPllumProvider(HttpClient httpClient, IConfiguration configuration, ILogger<LocalPllumProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _modelName = configuration["LlmSettings:Local:ModelName"] ?? "pllum";

            var apiKey = configuration["LlmSettings:Local:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        public async Task<string> SendChatAsync(string systemPrompt, string userPrompt, int maxTokens = 2000, double temperature = 0.7)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens,
                temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Local Pllum API request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var messageContent))
                {
                    return messageContent.GetString() ?? string.Empty;
                }
            }

            throw new InvalidOperationException("Invalid response format from Local Pllum API");
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await SendChatAsync(
                    "You are a test assistant.",
                    "Test. Respond OK.",
                    maxTokens: 10,
                    temperature: 0);
                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local Pllum API connection test failed");
                return false;
            }
        }
    }
}

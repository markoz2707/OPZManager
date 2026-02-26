using System.Text;
using System.Text.Json;

namespace OPZManager.API.Services.Embeddings
{
    public class OpenAICompatibleEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAICompatibleEmbeddingProvider> _logger;
        private readonly string _modelName;
        private readonly int _dimensions;
        private readonly string _providerName;

        public string ProviderName => _providerName;
        public string ModelName => _modelName;
        public int Dimensions => _dimensions;

        /// <summary>
        /// configSection: e.g. "OpenAICompatible" or "Mistral" — reads from EmbeddingSettings:{configSection}:*
        /// </summary>
        public OpenAICompatibleEmbeddingProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAICompatibleEmbeddingProvider> logger, string configSection = "OpenAICompatible")
        {
            _httpClient = httpClient;
            _logger = logger;

            var section = $"EmbeddingSettings:{configSection}";
            _providerName = configSection == "Mistral" ? "Mistral" : "OpenAI-Compatible";
            _modelName = configuration[$"{section}:ModelName"] ?? "text-embedding-3-small";
            _dimensions = int.Parse(configuration[$"{section}:Dimensions"] ?? "1536");

            var baseUrl = configuration[$"{section}:BaseUrl"] ?? "http://localhost:1234/v1/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            var apiKey = configuration[$"{section}:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var result = await GenerateEmbeddingsAsync(new[] { text });
            return result[0];
        }

        public async Task<float[][]> GenerateEmbeddingsAsync(IList<string> texts)
        {
            const int maxRetries = 5;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var requestBody = new
                {
                    model = _modelName,
                    input = texts
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("embeddings", content);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt == maxRetries)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Embedding API rate limit exceeded after {MaxRetries} retries: {Error}", maxRetries, errorBody);
                        throw new HttpRequestException($"Embedding API rate limit exceeded after {maxRetries} retries");
                    }

                    var delay = GetRetryDelay(response, attempt);
                    _logger.LogWarning("Embedding API rate limited (429), retry {Attempt}/{MaxRetries} after {Delay}ms", attempt + 1, maxRetries, delay.TotalMilliseconds);
                    await Task.Delay(delay);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI-Compatible Embedding API request failed: {StatusCode} - {Error}", response.StatusCode, errorBody);
                    throw new HttpRequestException($"Embedding API request failed: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (!responseObj.TryGetProperty("data", out var data))
                {
                    throw new InvalidOperationException("Invalid response format from Embedding API: missing 'data'");
                }

                var embeddings = new float[texts.Count][];
                foreach (var item in data.EnumerateArray())
                {
                    var index = item.GetProperty("index").GetInt32();
                    var embedding = item.GetProperty("embedding");
                    var values = new float[embedding.GetArrayLength()];
                    var i = 0;
                    foreach (var val in embedding.EnumerateArray())
                    {
                        values[i++] = val.GetSingle();
                    }
                    embeddings[index] = values;
                }

                return embeddings;
            }

            throw new HttpRequestException("Embedding API request failed after all retries");
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var retryAfter = values.FirstOrDefault();
                if (int.TryParse(retryAfter, out var seconds))
                    return TimeSpan.FromSeconds(seconds);
            }
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var result = await GenerateEmbeddingAsync("test");
                return result.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI-Compatible Embedding API connection test failed");
                return false;
            }
        }
    }
}

using System.Text;
using System.Text.Json;

namespace OPZManager.API.Services.Embeddings
{
    public class GeminiEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiEmbeddingProvider> _logger;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly int _dimensions;

        public string ProviderName => "Google Gemini";
        public string ModelName => _modelName;
        public int Dimensions => _dimensions;

        public GeminiEmbeddingProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiEmbeddingProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["EmbeddingSettings:Gemini:ApiKey"] ?? string.Empty;
            _modelName = configuration["EmbeddingSettings:Gemini:ModelName"] ?? "text-embedding-004";
            _dimensions = int.Parse(configuration["EmbeddingSettings:Gemini:Dimensions"] ?? "768");

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Gemini Embedding API key is not configured in EmbeddingSettings:Gemini:ApiKey");
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:embedContent?key={_apiKey}";

            var requestBody = new
            {
                model = $"models/{_modelName}",
                content = new
                {
                    parts = new[] { new { text } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini Embedding API request failed: {StatusCode} - {Error}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Gemini Embedding API request failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("embedding", out var embedding) &&
                embedding.TryGetProperty("values", out var values))
            {
                var result = new float[values.GetArrayLength()];
                var i = 0;
                foreach (var val in values.EnumerateArray())
                {
                    result[i++] = val.GetSingle();
                }
                return result;
            }

            throw new InvalidOperationException("Invalid response format from Gemini Embedding API");
        }

        public async Task<float[][]> GenerateEmbeddingsAsync(IList<string> texts)
        {
            // Gemini doesn't have a batch embedding endpoint, so we process sequentially
            var results = new float[texts.Count][];
            for (int i = 0; i < texts.Count; i++)
            {
                results[i] = await GenerateEmbeddingAsync(texts[i]);
            }
            return results;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Cannot test Gemini Embedding connection: API key not configured");
                    return false;
                }

                var result = await GenerateEmbeddingAsync("test");
                return result.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini Embedding API connection test failed");
                return false;
            }
        }
    }
}

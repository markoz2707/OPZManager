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

        public string ProviderName => "OpenAI-Compatible";
        public string ModelName => _modelName;
        public int Dimensions => _dimensions;

        public OpenAICompatibleEmbeddingProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAICompatibleEmbeddingProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _modelName = configuration["EmbeddingSettings:OpenAICompatible:ModelName"] ?? "text-embedding-3-small";
            _dimensions = int.Parse(configuration["EmbeddingSettings:OpenAICompatible:Dimensions"] ?? "1536");

            var baseUrl = configuration["EmbeddingSettings:OpenAICompatible:BaseUrl"] ?? "http://localhost:1234/v1/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            var apiKey = configuration["EmbeddingSettings:OpenAICompatible:ApiKey"];
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
            var requestBody = new
            {
                model = _modelName,
                input = texts
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("embeddings", content);

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

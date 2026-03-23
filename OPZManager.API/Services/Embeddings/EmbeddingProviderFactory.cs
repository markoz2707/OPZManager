namespace OPZManager.API.Services.Embeddings
{
    public interface IEmbeddingProviderFactory
    {
        IEmbeddingProvider GetProvider();
    }

    public class EmbeddingProviderFactory : IEmbeddingProviderFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISystemConfigService _configService;
        private readonly IConfiguration _configuration;

        public EmbeddingProviderFactory(
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            ISystemConfigService configService,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _configService = configService;
            _configuration = configuration;
        }

        public IEmbeddingProvider GetProvider()
        {
            var provider = _configService.GetAsync("EmbeddingSettings:Provider",
                _configuration["EmbeddingSettings:Provider"] ?? "openai-compatible").GetAwaiter().GetResult();

            var httpClient = _httpClientFactory.CreateClient("EmbeddingProvider");
            var config = BuildOverlayConfiguration();

            return provider.ToLower() switch
            {
                "gemini" => new GeminiEmbeddingProvider(
                    httpClient,
                    config,
                    _serviceProvider.GetRequiredService<ILogger<GeminiEmbeddingProvider>>()),

                "mistral" => new OpenAICompatibleEmbeddingProvider(
                    httpClient,
                    config,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleEmbeddingProvider>>(),
                    "Mistral"),

                _ => new OpenAICompatibleEmbeddingProvider(
                    httpClient,
                    config,
                    _serviceProvider.GetRequiredService<ILogger<OpenAICompatibleEmbeddingProvider>>())
            };
        }

        private IConfiguration BuildOverlayConfiguration()
        {
            var context = _serviceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var settings = context.SystemSettings.ToList();

            var overrides = new Dictionary<string, string?>();
            foreach (var s in settings)
            {
                overrides[s.Key] = s.Value;
            }

            if (overrides.Count == 0)
                return _configuration;

            var builder = new ConfigurationBuilder();
            builder.AddConfiguration(_configuration);
            builder.AddInMemoryCollection(overrides);
            return builder.Build();
        }
    }
}

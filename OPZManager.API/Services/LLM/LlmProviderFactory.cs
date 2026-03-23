namespace OPZManager.API.Services.LLM
{
    public interface ILlmProviderFactory
    {
        ILlmProvider GetProvider();
    }

    public class LlmProviderFactory : ILlmProviderFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISystemConfigService _configService;
        private readonly IConfiguration _configuration;

        public LlmProviderFactory(
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

        public ILlmProvider GetProvider()
        {
            // Read provider from DB (synchronous wrapper since DI resolution is sync)
            var provider = _configService.GetAsync("LlmSettings:Provider",
                _configuration["LlmSettings:Provider"] ?? "local").GetAwaiter().GetResult();

            var httpClient = _httpClientFactory.CreateClient("LlmProvider");

            return provider.ToLower() switch
            {
                "gemini" => new GeminiProvider(
                    httpClient,
                    BuildOverlayConfiguration(),
                    _serviceProvider.GetRequiredService<ILogger<GeminiProvider>>()),

                "anthropic" => new AnthropicProvider(
                    httpClient,
                    BuildOverlayConfiguration(),
                    _serviceProvider.GetRequiredService<ILogger<AnthropicProvider>>()),

                _ => CreateLocalProvider(httpClient)
            };
        }

        private LocalPllumProvider CreateLocalProvider(HttpClient httpClient)
        {
            var config = BuildOverlayConfiguration();
            var baseUrl = config["LlmSettings:Local:BaseUrl"] ?? "http://localhost:1234/v1/";
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

            return new LocalPllumProvider(
                httpClient,
                config,
                _serviceProvider.GetRequiredService<ILogger<LocalPllumProvider>>());
        }

        /// <summary>
        /// Builds a configuration that overlays DB settings on top of appsettings.json.
        /// </summary>
        private IConfiguration BuildOverlayConfiguration()
        {
            var dbSettings = _configService.GetType()
                .GetMethod("GetAsync")!; // We'll read the settings we need

            // Load all SystemSettings from DB into a dictionary
            var context = _serviceProvider.GetRequiredService<Data.ApplicationDbContext>();
            var settings = context.SystemSettings.ToList();

            var overrides = new Dictionary<string, string?>();
            foreach (var s in settings)
            {
                overrides[s.Key] = s.Value;
            }

            if (overrides.Count == 0)
                return _configuration;

            // Build layered config: appsettings.json + DB overrides
            var builder = new ConfigurationBuilder();
            builder.AddConfiguration(_configuration);
            builder.AddInMemoryCollection(overrides);
            return builder.Build();
        }
    }
}

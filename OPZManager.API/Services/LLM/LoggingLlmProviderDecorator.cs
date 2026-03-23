using System.Diagnostics;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services.LLM
{
    public class LoggingLlmProviderDecorator : ILlmProvider
    {
        private readonly ILlmProvider _inner;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoggingLlmProviderDecorator> _logger;

        public string ProviderName => _inner.ProviderName;
        public string ModelName => _inner.ModelName;

        public LoggingLlmProviderDecorator(
            ILlmProvider inner,
            ApplicationDbContext context,
            ILogger<LoggingLlmProviderDecorator> logger)
        {
            _inner = inner;
            _context = context;
            _logger = logger;
        }

        public async Task<string> SendChatAsync(
            string systemPrompt, string userPrompt,
            int maxTokens = 2000, double temperature = 0.7)
        {
            var log = new LlmLog
            {
                ProviderName = _inner.ProviderName,
                ModelName = _inner.ModelName,
                CallerMethod = InferCallerMethod(),
                SystemPrompt = Truncate(systemPrompt, 2000),
                UserPrompt = Truncate(userPrompt, 5000),
                MaxTokensRequested = maxTokens,
                Temperature = temperature,
                InputTokens = EstimateTokens(systemPrompt + userPrompt)
            };

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await _inner.SendChatAsync(systemPrompt, userPrompt, maxTokens, temperature);
                sw.Stop();

                log.DurationMs = sw.ElapsedMilliseconds;
                log.Response = Truncate(response, 5000);
                log.Success = true;
                log.OutputTokens = EstimateTokens(response);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                log.DurationMs = sw.ElapsedMilliseconds;
                log.Success = false;
                log.ErrorMessage = Truncate(ex.Message, 2000);
                throw;
            }
            finally
            {
                try
                {
                    _context.LlmLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to persist LLM log entry");
                }
            }
        }

        public Task<bool> TestConnectionAsync() => _inner.TestConnectionAsync();

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
        }

        private static int EstimateTokens(string text)
            => (int)Math.Ceiling(text.Length / 4.0);

        private static string InferCallerMethod()
        {
            var stackTrace = new StackTrace();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var method = stackTrace.GetFrame(i)?.GetMethod();
                var typeName = method?.DeclaringType?.Name ?? "";
                if (typeName == "PllumIntegrationService" ||
                    typeName == "EquipmentMatchingService" ||
                    typeName == "OPZVerificationService" ||
                    typeName == "OPZGenerationService")
                {
                    return $"{typeName}.{method!.Name}";
                }
            }
            return "Unknown";
        }
    }
}

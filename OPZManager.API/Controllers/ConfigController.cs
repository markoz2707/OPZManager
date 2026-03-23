using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Common;
using OPZManager.API.Services;
using OPZManager.API.Services.Embeddings;
using OPZManager.API.Services.LLM;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/config")]
    [Authorize(Roles = "Admin")]
    public class ConfigController : ControllerBase
    {
        private readonly IPllumIntegrationService _pllumService;
        private readonly ILlmProvider _llmProvider;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ISystemConfigService _configService;

        public ConfigController(
            IPllumIntegrationService pllumService,
            ILlmProvider llmProvider,
            IEmbeddingProvider embeddingProvider,
            ApplicationDbContext context,
            IConfiguration configuration,
            ISystemConfigService configService)
        {
            _pllumService = pllumService;
            _llmProvider = llmProvider;
            _embeddingProvider = embeddingProvider;
            _context = context;
            _configuration = configuration;
            _configService = configService;
        }

        [HttpGet("status")]
        public async Task<ActionResult<ConfigStatusDto>> GetStatus()
        {
            var llmConnected = await _pllumService.TestConnectionAsync();

            var status = new ConfigStatusDto
            {
                LlmConnected = llmConnected,
                LlmBaseUrl = _configuration["LlmSettings:Local:BaseUrl"] ?? "http://localhost:1234/v1/",
                LlmProvider = _llmProvider.ProviderName,
                LlmModelName = _llmProvider.ModelName,
                ManufacturersCount = await _context.Manufacturers.CountAsync(),
                EquipmentTypesCount = await _context.EquipmentTypes.CountAsync(),
                EquipmentModelsCount = await _context.EquipmentModels.CountAsync(),
                OPZDocumentsCount = await _context.OPZDocuments.CountAsync(),
                TrainingDataCount = await _context.TrainingData.CountAsync(),
                EmbeddingProvider = _embeddingProvider.ProviderName,
                EmbeddingModelName = _embeddingProvider.ModelName,
                KnowledgeDocumentsCount = await _context.KnowledgeDocuments.CountAsync(),
                KnowledgeChunksCount = await _context.KnowledgeChunks.CountAsync()
            };

            // Test embedding connection (non-blocking, catch errors)
            try
            {
                status.EmbeddingConnected = await _embeddingProvider.TestConnectionAsync();
            }
            catch
            {
                status.EmbeddingConnected = false;
            }

            return Ok(status);
        }

        [HttpGet("embedding/test")]
        public async Task<ActionResult<object>> TestEmbeddingConnection()
        {
            try
            {
                var isConnected = await _embeddingProvider.TestConnectionAsync();
                return Ok(new
                {
                    connected = isConnected,
                    provider = _embeddingProvider.ProviderName,
                    modelName = _embeddingProvider.ModelName,
                    dimensions = _embeddingProvider.Dimensions,
                    message = isConnected
                        ? $"Połączenie z modelem embeddingu ({_embeddingProvider.ProviderName}) działa prawidłowo."
                        : $"Nie można połączyć się z modelem embeddingu ({_embeddingProvider.ProviderName})."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    connected = false,
                    provider = _embeddingProvider.ProviderName,
                    modelName = _embeddingProvider.ModelName,
                    dimensions = _embeddingProvider.Dimensions,
                    message = $"Błąd połączenia z modelem embeddingu: {ex.Message}"
                });
            }
        }

        [HttpGet("llm/test")]
        public async Task<ActionResult<object>> TestLlmConnection()
        {
            var isConnected = await _pllumService.TestConnectionAsync();
            return Ok(new
            {
                connected = isConnected,
                provider = _llmProvider.ProviderName,
                modelName = _llmProvider.ModelName,
                message = isConnected
                    ? $"Połączenie z modelem LLM ({_llmProvider.ProviderName}) działa prawidłowo."
                    : $"Nie można połączyć się z modelem LLM ({_llmProvider.ProviderName})."
            });
        }

        [HttpGet("llm/settings")]
        public async Task<ActionResult<object>> GetLlmSettings()
        {
            var settings = await _configService.GetLlmSettingsAsync();
            return Ok(new
            {
                provider = settings.Provider,
                baseUrl = settings.BaseUrl,
                apiKey = MaskApiKey(settings.ApiKey),
                modelName = settings.ModelName
            });
        }

        [HttpPost("llm")]
        public async Task<ActionResult<object>> UpdateLlmSettings([FromBody] UpdateLlmSettingsDto dto)
        {
            await _configService.UpdateLlmSettingsAsync(new LlmSettingsDto
            {
                Provider = dto.Provider,
                BaseUrl = dto.BaseUrl ?? "",
                ApiKey = dto.ApiKey ?? "",
                ModelName = dto.ModelName ?? ""
            });

            // Test connection with new settings (provider will be resolved fresh on next request)
            // For now, return success and let the frontend test separately
            return Ok(new
            {
                success = true,
                message = "Ustawienia LLM zostały zapisane. Użyj 'Testuj połączenie' aby zweryfikować."
            });
        }

        [HttpGet("embedding/settings")]
        public async Task<ActionResult<object>> GetEmbeddingSettings()
        {
            var settings = await _configService.GetEmbeddingSettingsAsync();
            return Ok(new
            {
                provider = settings.Provider,
                baseUrl = settings.BaseUrl,
                apiKey = MaskApiKey(settings.ApiKey),
                modelName = settings.ModelName,
                dimensions = settings.Dimensions
            });
        }

        [HttpPost("embedding")]
        public async Task<ActionResult<object>> UpdateEmbeddingSettings([FromBody] UpdateEmbeddingSettingsDto dto)
        {
            await _configService.UpdateEmbeddingSettingsAsync(new EmbeddingSettingsDto
            {
                Provider = dto.Provider,
                BaseUrl = dto.BaseUrl ?? "",
                ApiKey = dto.ApiKey ?? "",
                ModelName = dto.ModelName ?? "",
                Dimensions = dto.Dimensions
            });

            return Ok(new
            {
                success = true,
                message = "Ustawienia embeddingu zostały zapisane. Użyj 'Testuj połączenie' aby zweryfikować."
            });
        }

        [HttpGet("llm-logs")]
        public async Task<ActionResult<object>> GetLlmLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? method = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _context.LlmLogs.AsQueryable();

            if (status == "success") query = query.Where(l => l.Success);
            if (status == "error") query = query.Where(l => !l.Success);
            if (!string.IsNullOrEmpty(method)) query = query.Where(l => l.CallerMethod == method);
            if (from.HasValue) query = query.Where(l => l.Timestamp >= from.Value.ToUniversalTime());
            if (to.HasValue) query = query.Where(l => l.Timestamp <= to.Value.ToUniversalTime());

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new DTOs.Admin.LlmLogSummaryDto
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp,
                    ProviderName = l.ProviderName,
                    ModelName = l.ModelName,
                    CallerMethod = l.CallerMethod,
                    DurationMs = l.DurationMs,
                    Success = l.Success,
                    InputTokens = l.InputTokens,
                    OutputTokens = l.OutputTokens
                })
                .ToListAsync();

            return Ok(new { items, totalCount, page, pageSize });
        }

        [HttpGet("llm-logs/{id}")]
        public async Task<ActionResult<DTOs.Admin.LlmLogDetailDto>> GetLlmLogDetail(int id)
        {
            var log = await _context.LlmLogs.FindAsync(id);
            if (log == null) return NotFound();

            return Ok(new DTOs.Admin.LlmLogDetailDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                ProviderName = log.ProviderName,
                ModelName = log.ModelName,
                CallerMethod = log.CallerMethod,
                SystemPrompt = log.SystemPrompt,
                UserPrompt = log.UserPrompt,
                Response = log.Response,
                InputTokens = log.InputTokens,
                OutputTokens = log.OutputTokens,
                DurationMs = log.DurationMs,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage,
                MaxTokensRequested = log.MaxTokensRequested,
                Temperature = log.Temperature
            });
        }

        [HttpGet("llm-logs/methods")]
        public async Task<ActionResult<List<string>>> GetLlmLogMethods()
        {
            var methods = await _context.LlmLogs
                .Select(l => l.CallerMethod)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
            return Ok(methods);
        }

        private static string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
                return string.IsNullOrEmpty(apiKey) ? "" : "****";

            return "****" + apiKey[^4..];
        }
    }
}

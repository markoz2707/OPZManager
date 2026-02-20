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

        public ConfigController(
            IPllumIntegrationService pllumService,
            ILlmProvider llmProvider,
            IEmbeddingProvider embeddingProvider,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _pllumService = pllumService;
            _llmProvider = llmProvider;
            _embeddingProvider = embeddingProvider;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("status")]
        public async Task<ActionResult<ConfigStatusDto>> GetStatus()
        {
            var llmConnected = await _pllumService.TestConnectionAsync();

            var status = new ConfigStatusDto
            {
                LlmConnected = llmConnected,
                LlmBaseUrl = _configuration["LlmSettings:Local:BaseUrl"] ?? _configuration["PllumAPI:BaseUrl"] ?? "http://localhost:1234/v1/",
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
                baseUrl = _configuration["LlmSettings:Local:BaseUrl"] ?? _configuration["PllumAPI:BaseUrl"] ?? "http://localhost:1234/v1/",
                message = isConnected
                    ? $"Połączenie z modelem LLM ({_llmProvider.ProviderName}) działa prawidłowo."
                    : $"Nie można połączyć się z modelem LLM ({_llmProvider.ProviderName})."
            });
        }
    }
}

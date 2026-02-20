using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Common;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/config")]
    [Authorize(Roles = "Admin")]
    public class ConfigController : ControllerBase
    {
        private readonly IPllumIntegrationService _pllumService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ConfigController(
            IPllumIntegrationService pllumService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _pllumService = pllumService;
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
                LlmBaseUrl = _configuration["PllumAPI:BaseUrl"] ?? "http://localhost:1234/v1/",
                ManufacturersCount = await _context.Manufacturers.CountAsync(),
                EquipmentTypesCount = await _context.EquipmentTypes.CountAsync(),
                EquipmentModelsCount = await _context.EquipmentModels.CountAsync(),
                OPZDocumentsCount = await _context.OPZDocuments.CountAsync(),
                TrainingDataCount = await _context.TrainingData.CountAsync()
            };

            return Ok(status);
        }

        [HttpGet("llm/test")]
        public async Task<ActionResult<object>> TestLlmConnection()
        {
            var isConnected = await _pllumService.TestConnectionAsync();
            return Ok(new
            {
                connected = isConnected,
                baseUrl = _configuration["PllumAPI:BaseUrl"] ?? "http://localhost:1234/v1/",
                message = isConnected ? "Połączenie z modelem LLM działa prawidłowo." : "Nie można połączyć się z modelem LLM."
            });
        }
    }
}

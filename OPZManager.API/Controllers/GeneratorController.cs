using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.OPZ;
using OPZManager.API.Exceptions;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/generator")]
    [Authorize]
    public class GeneratorController : ControllerBase
    {
        private readonly IOPZGenerationService _generationService;
        private readonly IDocxExportService _docxExportService;
        private readonly ApplicationDbContext _context;

        public GeneratorController(IOPZGenerationService generationService, IDocxExportService docxExportService, ApplicationDbContext context)
        {
            _generationService = generationService;
            _docxExportService = docxExportService;
            _context = context;
        }

        [HttpPost("content")]
        public async Task<ActionResult<object>> GenerateContent([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                throw new NotFoundException("EquipmentModel", string.Join(", ", request.EquipmentModelIds));

            var content = await _generationService.GenerateOPZContentAsync(equipmentModels, request.EquipmentType);

            return Ok(new { content });
        }

        [HttpPost("pdf")]
        public async Task<IActionResult> GeneratePdf([FromBody] GenerateOPZPdfRequestDto request)
        {
            var pdfBytes = await _generationService.GenerateOPZPdfAsync(request.Content, request.Title);
            return File(pdfBytes, "application/pdf", $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpPost("docx")]
        public async Task<IActionResult> GenerateDocx([FromBody] GenerateOPZPdfRequestDto request)
        {
            var docxBytes = await _docxExportService.GenerateDocxAsync(request.Content, request.Title);
            return File(docxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }

        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] DTOs.Common.ExportRequestDto request)
        {
            if (string.Equals(request.Format, "docx", StringComparison.OrdinalIgnoreCase))
            {
                var docxBytes = await _docxExportService.GenerateDocxAsync(request.Content, request.Title);
                return File(docxBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
            }

            var pdfBytes = await _generationService.GenerateOPZPdfAsync(request.Content, request.Title);
            return File(pdfBytes, "application/pdf", $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpPost("compliance")]
        public async Task<ActionResult<object>> GenerateCompliance([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                throw new NotFoundException("EquipmentModel", string.Join(", ", request.EquipmentModelIds));

            var compliance = await _generationService.GenerateComplianceRequirementsAsync(equipmentModels);

            return Ok(new { content = compliance });
        }

        [HttpPost("technical-specs")]
        public async Task<ActionResult<object>> GenerateTechnicalSpecs([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                throw new NotFoundException("EquipmentModel", string.Join(", ", request.EquipmentModelIds));

            var techSpecs = await _generationService.GenerateTechnicalSpecificationsAsync(equipmentModels);

            return Ok(new { content = techSpecs });
        }
    }
}

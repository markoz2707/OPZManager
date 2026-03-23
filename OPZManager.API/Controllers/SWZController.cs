using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/swz")]
    public class SWZController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISWZGenerationService _swzService;
        private readonly IDocxExportService _docxService;
        private readonly IPdfProcessingService _pdfService;

        public SWZController(
            ApplicationDbContext context,
            ISWZGenerationService swzService,
            IDocxExportService docxService,
            IPdfProcessingService pdfService)
        {
            _context = context;
            _swzService = swzService;
            _docxService = docxService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Generate SWZ from selected equipment models.
        /// Anonymous users get a preview (first 10 requirements), authenticated users get full content.
        /// </summary>
        [HttpPost("generate")]
        [EnableRateLimiting("anonymous")]
        public async Task<IActionResult> GenerateSWZ([FromBody] GenerateSWZRequestDto request)
        {
            if (request.EquipmentModelIds == null || request.EquipmentModelIds.Count < 1)
                return BadRequest(new { message = "Wybierz co najmniej jeden model sprzętu." });

            if (request.EquipmentModelIds.Count > 10)
                return BadRequest(new { message = "Maksymalnie 10 modeli na jedno zamówienie." });

            var models = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .Where(m => request.EquipmentModelIds.Contains(m.Id))
                .ToListAsync();

            if (!models.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var result = await _swzService.GenerateSWZAsync(models, request.EquipmentType);

            // Anonymous users get preview only
            if (User.Identity?.IsAuthenticated != true)
            {
                var preview = _swzService.GetPreviewForAnonymous(result);
                return Ok(new
                {
                    content = preview.PreviewContent,
                    isFullContent = false,
                    totalRequirements = preview.TotalRequirements,
                    visibleRequirements = preview.VisibleRequirements,
                    requirements = preview.VisibleItems,
                });
            }

            return Ok(new
            {
                content = result.Content,
                isFullContent = true,
                totalRequirements = result.ExtractedRequirements.Count,
                visibleRequirements = result.ExtractedRequirements.Count,
                requirements = result.ExtractedRequirements,
            });
        }

        /// <summary>
        /// Download SWZ as PDF. Requires authentication.
        /// </summary>
        [HttpPost("download/pdf")]
        [Authorize]
        public async Task<IActionResult> DownloadPdf([FromBody] GenerateSWZRequestDto request)
        {
            var models = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .Where(m => request.EquipmentModelIds.Contains(m.Id))
                .ToListAsync();

            if (!models.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var result = await _swzService.GenerateSWZAsync(models, request.EquipmentType);
            var title = $"SWZ - {request.EquipmentType}";
            var pdfBytes = await _pdfService.GenerateOPZPdfAsync(result.Content, title);

            return File(pdfBytes, "application/pdf", $"SWZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        /// <summary>
        /// Download SWZ as DOCX. Requires authentication.
        /// </summary>
        [HttpPost("download/docx")]
        [Authorize]
        public async Task<IActionResult> DownloadDocx([FromBody] GenerateSWZRequestDto request)
        {
            var models = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .Where(m => request.EquipmentModelIds.Contains(m.Id))
                .ToListAsync();

            if (!models.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var result = await _swzService.GenerateSWZAsync(models, request.EquipmentType);
            var title = $"SWZ - {request.EquipmentType}";
            var docxBytes = await _docxService.GenerateDocxAsync(result.Content, title);

            return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"SWZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }
    }

    public class GenerateSWZRequestDto
    {
        public List<int> EquipmentModelIds { get; set; } = new();
        public string EquipmentType { get; set; } = string.Empty;
    }
}

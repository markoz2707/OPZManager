using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Equipment;
using OPZManager.API.DTOs.OPZ;
using OPZManager.API.DTOs.Public;
using OPZManager.API.Models;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/public")]
    [EnableRateLimiting("anonymous")]
    public class PublicController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfProcessingService _pdfService;
        private readonly IOPZVerificationService _verificationService;
        private readonly IEquipmentMatchingService _equipmentMatchingService;
        private readonly IOPZGenerationService _generationService;
        private readonly ILeadCaptureService _leadCaptureService;
        private readonly IDocxExportService _docxExportService;
        private readonly IMapper _mapper;
        private readonly ILogger<PublicController> _logger;

        public PublicController(
            ApplicationDbContext context,
            IPdfProcessingService pdfService,
            IOPZVerificationService verificationService,
            IEquipmentMatchingService equipmentMatchingService,
            IOPZGenerationService generationService,
            ILeadCaptureService leadCaptureService,
            IDocxExportService docxExportService,
            IMapper mapper,
            ILogger<PublicController> logger)
        {
            _context = context;
            _pdfService = pdfService;
            _verificationService = verificationService;
            _equipmentMatchingService = equipmentMatchingService;
            _generationService = generationService;
            _leadCaptureService = leadCaptureService;
            _docxExportService = docxExportService;
            _mapper = mapper;
            _logger = logger;
        }

        private string? GetSessionId()
        {
            return Request.Headers["X-Session-Id"].FirstOrDefault();
        }

        private string? GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        // POST api/public/opz/upload
        [HttpPost("opz/upload")]
        public async Task<ActionResult<PublicOPZDocumentDto>> UploadOPZ(IFormFile file)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId) || sessionId.Length > 36)
                return BadRequest(new { message = "Brak lub nieprawidłowy identyfikator sesji (X-Session-Id)." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Plik nie został przesłany." });

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                return BadRequest(new { message = "Tylko pliki PDF są obsługiwane." });

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
                return BadRequest(new { message = "Plik jest zbyt duży (maksymalnie 50 MB)." });

            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "opz");
            if (!Directory.Exists(uploadsDirectory))
                Directory.CreateDirectory(uploadsDirectory);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsDirectory, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            List<OPZRequirement> requirements;
            try
            {
                var pdfText = await _pdfService.ExtractTextFromPdfAsync(filePath);
                requirements = await _pdfService.ExtractOPZRequirementsAsync(pdfText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract requirements from uploaded PDF");
                requirements = new List<OPZRequirement>();
            }

            var opzDocument = new OPZDocument
            {
                Filename = file.FileName,
                FilePath = filePath,
                AnonymousSessionId = sessionId,
                UploadDate = DateTime.UtcNow,
                AnalysisStatus = requirements.Count > 0 ? "Przetworzony" : "Uploaded"
            };

            _context.OPZDocuments.Add(opzDocument);
            await _context.SaveChangesAsync();

            foreach (var req in requirements)
            {
                req.OPZId = opzDocument.Id;
                _context.OPZRequirements.Add(req);
            }
            await _context.SaveChangesAsync();

            // Reload with includes for mapping
            var loaded = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .Include(d => d.EquipmentMatches)
                .FirstAsync(d => d.Id == opzDocument.Id);

            return Ok(_mapper.Map<PublicOPZDocumentDto>(loaded));
        }

        // GET api/public/opz/{id}
        [HttpGet("opz/{id}")]
        public async Task<ActionResult<PublicOPZDocumentDto>> GetOPZDocument(int id)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Brak identyfikatora sesji (X-Session-Id)." });

            var document = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .Include(d => d.EquipmentMatches)
                .FirstOrDefaultAsync(d => d.Id == id && d.AnonymousSessionId == sessionId);

            if (document == null)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            return Ok(_mapper.Map<PublicOPZDocumentDto>(document));
        }

        // POST api/public/opz/{id}/verify
        [HttpPost("opz/{id}/verify")]
        public async Task<ActionResult<VerificationResultDto>> VerifyOPZ(int id)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Brak identyfikatora sesji (X-Session-Id)." });

            var document = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .FirstOrDefaultAsync(d => d.Id == id && d.AnonymousSessionId == sessionId);

            if (document == null)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            var result = await _verificationService.VerifyDocumentAsync(document);
            return Ok(MapVerificationResult(result));
        }

        // GET api/public/opz/{id}/verification
        [HttpGet("opz/{id}/verification")]
        public async Task<ActionResult<VerificationResultDto>> GetVerification(int id)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Brak identyfikatora sesji (X-Session-Id)." });

            var document = await _context.OPZDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.AnonymousSessionId == sessionId);

            if (document == null)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            var result = await _verificationService.GetVerificationResultAsync(id);
            if (result == null)
                return NotFound(new { message = "Brak wyników weryfikacji. Najpierw uruchom weryfikację." });

            return Ok(MapVerificationResult(result));
        }

        // POST api/public/opz/{id}/analyze
        [HttpPost("opz/{id}/analyze")]
        public async Task<ActionResult<object>> AnalyzeOPZ(int id)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Brak identyfikatora sesji (X-Session-Id)." });

            var opzDocument = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .FirstOrDefaultAsync(d => d.Id == id && d.AnonymousSessionId == sessionId);

            if (opzDocument == null)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            opzDocument.AnalysisStatus = "Analizowanie";
            await _context.SaveChangesAsync();

            try
            {
                var matches = await _equipmentMatchingService.FindMatchingEquipmentAsync(opzDocument);

                opzDocument.AnalysisStatus = "Zakończono analizę";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Analiza zakończona pomyślnie.",
                    matchesCount = matches.Count,
                    matches = _mapper.Map<List<EquipmentMatchDto>>(matches)
                });
            }
            catch
            {
                opzDocument.AnalysisStatus = "Błąd analizy";
                await _context.SaveChangesAsync();
                throw;
            }
        }

        // POST api/public/generate/content
        [HttpPost("generate/content")]
        public async Task<ActionResult<object>> GenerateContent([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var fullContent = await _generationService.GenerateOPZContentAsync(equipmentModels, request.EquipmentType);

            // Authenticated users get full content
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new { content = fullContent, isFullContent = true });
            }

            // Anonymous users get preview only
            var preview = _generationService.SplitContentForPreview(fullContent);
            return Ok(new { content = preview.Preview, isFullContent = false });
        }

        // POST api/public/generate/pdf — requires JWT
        [Authorize]
        [HttpPost("generate/pdf")]
        public async Task<IActionResult> GenerateAuthorizedPdf([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var fullContent = await _generationService.GenerateOPZContentAsync(equipmentModels, request.EquipmentType);
            var title = $"OPZ - {request.EquipmentType}";
            var pdfBytes = await _generationService.GenerateOPZPdfAsync(fullContent, title);
            return File(pdfBytes, "application/pdf", $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        // POST api/public/lead-capture
        [HttpPost("lead-capture")]
        public async Task<ActionResult<LeadCaptureResponseDto>> CaptureLead([FromBody] LeadCaptureRequestDto request)
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(new { message = "Brak identyfikatora sesji (X-Session-Id)." });

            var lead = await _leadCaptureService.CaptureLeadAsync(
                request.Email,
                request.MarketingConsent,
                sessionId,
                request.OPZDocumentId,
                request.Source,
                GetClientIp());

            return Ok(new LeadCaptureResponseDto
            {
                DownloadToken = lead.DownloadToken!,
                ExpiresAt = lead.DownloadTokenExpiresAt!.Value,
                Message = "Dziękujemy! Token pobierania został wygenerowany."
            });
        }

        // POST api/public/download/pdf
        [HttpPost("download/pdf")]
        public async Task<IActionResult> DownloadPdf([FromBody] DownloadRequestDto request)
        {
            var lead = await _leadCaptureService.ValidateDownloadTokenAsync(request.DownloadToken);
            if (lead == null)
                return Unauthorized(new { message = "Nieprawidłowy lub wygasły token pobierania. Podaj adres email ponownie." });

            var pdfBytes = await _generationService.GenerateOPZPdfAsync(request.Content, request.Title);
            return File(pdfBytes, "application/pdf", $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }

        // POST api/public/generate/docx — requires JWT
        [Authorize]
        [HttpPost("generate/docx")]
        public async Task<IActionResult> GenerateAuthorizedDocx([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var fullContent = await _generationService.GenerateOPZContentAsync(equipmentModels, request.EquipmentType);
            var title = $"OPZ - {request.EquipmentType}";
            var docxBytes = await _docxExportService.GenerateDocxAsync(fullContent, title);
            return File(docxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }

        // POST api/public/download/docx — requires lead capture token
        [HttpPost("download/docx")]
        public async Task<IActionResult> DownloadDocx([FromBody] DownloadRequestDto request)
        {
            var lead = await _leadCaptureService.ValidateDownloadTokenAsync(request.DownloadToken);
            if (lead == null)
                return Unauthorized(new { message = "Nieprawidłowy lub wygasły token pobierania. Podaj adres email ponownie." });

            var docxBytes = await _docxExportService.GenerateDocxAsync(request.Content, request.Title);
            return File(docxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }

        // GET api/public/equipment/types
        [HttpGet("equipment/types")]
        public async Task<ActionResult<List<EquipmentTypeDto>>> GetEquipmentTypes()
        {
            var types = await _context.EquipmentTypes.OrderBy(t => t.Name).ToListAsync();
            return Ok(_mapper.Map<List<EquipmentTypeDto>>(types));
        }

        // GET api/public/equipment/models
        [HttpGet("equipment/models")]
        public async Task<ActionResult<List<EquipmentModelDto>>> GetEquipmentModels()
        {
            var models = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .OrderBy(m => m.Manufacturer.Name)
                .ThenBy(m => m.ModelName)
                .ToListAsync();
            return Ok(_mapper.Map<List<EquipmentModelDto>>(models));
        }

        // GET api/public/equipment/manufacturers
        [HttpGet("equipment/manufacturers")]
        public async Task<ActionResult<List<ManufacturerDto>>> GetManufacturers()
        {
            var manufacturers = await _context.Manufacturers.OrderBy(m => m.Name).ToListAsync();
            return Ok(_mapper.Map<List<ManufacturerDto>>(manufacturers));
        }

        // POST api/public/generate/docx — requires JWT
        [Authorize]
        [HttpPost("generate/docx")]
        public async Task<IActionResult> GenerateAuthorizedDocx([FromBody] GenerateOPZContentRequestDto request)
        {
            var equipmentModels = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => request.EquipmentModelIds.Contains(e.Id))
                .ToListAsync();

            if (!equipmentModels.Any())
                return NotFound(new { message = "Nie znaleziono wybranych modeli sprzętu." });

            var fullContent = await _generationService.GenerateOPZContentAsync(equipmentModels, request.EquipmentType);
            var title = $"OPZ - {request.EquipmentType}";
            var docxBytes = await _docxExportService.GenerateDocxAsync(fullContent, title);
            return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }

        // POST api/public/download/docx — with lead capture token
        [HttpPost("download/docx")]
        public async Task<IActionResult> DownloadDocx([FromBody] DownloadRequestDto request)
        {
            var lead = await _leadCaptureService.ValidateDownloadTokenAsync(request.DownloadToken);
            if (lead == null)
                return Unauthorized(new { message = "Nieprawidłowy lub wygasły token pobierania." });

            var docxBytes = await _docxExportService.GenerateDocxAsync(request.Content, request.Title);
            return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"OPZ_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx");
        }

        // GET api/public/equipment/models/search
        [HttpGet("equipment/models/search")]
        public async Task<ActionResult> SearchModels(
            [FromQuery] string? q = null,
            [FromQuery] int? typeId = null,
            [FromQuery] int? manufacturerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(m => m.ModelName.Contains(q) || m.Manufacturer.Name.Contains(q) || m.SpecificationsJson!.Contains(q));
            if (typeId.HasValue)
                query = query.Where(m => m.TypeId == typeId.Value);
            if (manufacturerId.HasValue)
                query = query.Where(m => m.ManufacturerId == manufacturerId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(m => m.Manufacturer.Name).ThenBy(m => m.ModelName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return Ok(new { items = _mapper.Map<List<EquipmentModelDto>>(items), totalCount = total, page, pageSize });
        }

        // GET api/public/equipment/models/{id}
        [HttpGet("equipment/models/{id}")]
        public async Task<ActionResult<EquipmentModelDto>> GetModelDetail(int id)
        {
            var model = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null)
                return NotFound(new { message = "Model nie został znaleziony." });

            return Ok(_mapper.Map<EquipmentModelDto>(model));
        }

        // POST api/public/equipment/models/compare
        [HttpPost("equipment/models/compare")]
        public async Task<ActionResult> CompareModels([FromBody] CompareModelsRequestDto request)
        {
            if (request.ModelIds == null || request.ModelIds.Count < 2 || request.ModelIds.Count > 5)
                return BadRequest(new { message = "Wybierz od 2 do 5 modeli do porównania." });

            var models = await _context.EquipmentModels
                .Include(m => m.Manufacturer)
                .Include(m => m.Type)
                .Where(m => request.ModelIds.Contains(m.Id))
                .ToListAsync();

            var result = models.Select(m => new
            {
                m.Id,
                m.ModelName,
                ManufacturerName = m.Manufacturer.Name,
                TypeName = m.Type.Name,
                Specifications = m.SpecificationsJson
            });

            return Ok(result);
        }

        private VerificationResultDto MapVerificationResult(OPZVerificationResult result)
        {
            return new VerificationResultDto
            {
                Id = result.Id,
                OPZDocumentId = result.OPZDocumentId,
                OverallScore = result.OverallScore,
                Grade = result.Grade,
                Completeness = result.CompletenessJson != null
                    ? JsonSerializer.Deserialize<CompletenessResultDto>(result.CompletenessJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null,
                Compliance = result.ComplianceJson != null
                    ? JsonSerializer.Deserialize<ComplianceResultDto>(result.ComplianceJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null,
                Technical = result.TechnicalJson != null
                    ? JsonSerializer.Deserialize<TechnicalResultDto>(result.TechnicalJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null,
                GapAnalysis = result.GapAnalysisJson != null
                    ? JsonSerializer.Deserialize<GapAnalysisResultDto>(result.GapAnalysisJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null,
                SummaryText = result.SummaryText,
                CreatedAt = result.CreatedAt
            };
        }
    }
}

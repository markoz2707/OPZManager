using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.OPZ;
using OPZManager.API.Exceptions;
using OPZManager.API.Models;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/opz")]
    [Authorize]
    public class OPZController : ControllerBase
    {
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly IEquipmentMatchingService _equipmentMatchingService;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAnalysisProgressService _progressService;

        public OPZController(
            IPdfProcessingService pdfProcessingService,
            IEquipmentMatchingService equipmentMatchingService,
            ApplicationDbContext context,
            IMapper mapper,
            IServiceScopeFactory scopeFactory,
            IAnalysisProgressService progressService)
        {
            _pdfProcessingService = pdfProcessingService;
            _equipmentMatchingService = equipmentMatchingService;
            _context = context;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
            _progressService = progressService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<OPZDocumentDto>> UploadOPZ(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Plik nie został przesłany." });

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                return BadRequest(new { message = "Tylko pliki PDF są obsługiwane." });

            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "opz");
            if (!Directory.Exists(uploadsDirectory))
                Directory.CreateDirectory(uploadsDirectory);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsDirectory, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var pdfText = await _pdfProcessingService.ExtractTextFromPdfAsync(filePath);
            var requirements = await _pdfProcessingService.ExtractOPZRequirementsAsync(pdfText);

            var opzDocument = new OPZDocument
            {
                Filename = file.FileName,
                FilePath = filePath,
                UploadDate = DateTime.UtcNow,
                AnalysisStatus = "Przetworzony"
            };

            _context.OPZDocuments.Add(opzDocument);
            await _context.SaveChangesAsync();

            foreach (var requirement in requirements)
            {
                requirement.OPZId = opzDocument.Id;
                _context.OPZRequirements.Add(requirement);
            }
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<OPZDocumentDto>(opzDocument));
        }

        [HttpGet]
        public async Task<ActionResult<List<OPZDocumentDto>>> GetOPZDocuments()
        {
            var documents = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .Include(d => d.EquipmentMatches)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return Ok(_mapper.Map<List<OPZDocumentDto>>(documents));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OPZDocumentDetailDto>> GetOPZDocument(int id)
        {
            var document = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .Include(d => d.EquipmentMatches)
                    .ThenInclude(m => m.RequirementCompliances)
                .Include(d => d.EquipmentMatches)
                    .ThenInclude(m => m.EquipmentModel)
                        .ThenInclude(e => e.Manufacturer)
                .Include(d => d.EquipmentMatches)
                    .ThenInclude(m => m.EquipmentModel)
                        .ThenInclude(e => e.Type)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                throw new NotFoundException("OPZDocument", id);

            return Ok(_mapper.Map<OPZDocumentDetailDto>(document));
        }

        [HttpPost("{id}/analyze")]
        public async Task<ActionResult<object>> AnalyzeOPZ(int id)
        {
            var opzDocument = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (opzDocument == null)
                throw new NotFoundException("OPZDocument", id);

            // Check if already running
            var existing = _progressService.GetProgress(id);
            if (existing?.Status == "running")
                return Ok(new { message = "Analiza już trwa.", status = "running" });

            opzDocument.AnalysisStatus = "Analizowanie";
            await _context.SaveChangesAsync();

            // Fire-and-forget in background with a new DI scope
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var matchingService = scope.ServiceProvider.GetRequiredService<IEquipmentMatchingService>();
                var progressService = scope.ServiceProvider.GetRequiredService<IAnalysisProgressService>();

                var doc = await ctx.OPZDocuments
                    .Include(d => d.OPZRequirements)
                    .FirstAsync(d => d.Id == id);

                try
                {
                    await matchingService.FindMatchingEquipmentAsync(doc, progressService);
                    progressService.Complete(id);
                    doc.AnalysisStatus = "Zakończono analizę";
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    progressService.Fail(id, ex.Message);
                    doc.AnalysisStatus = "Błąd analizy";
                    await ctx.SaveChangesAsync();
                }
            });

            return Accepted(new { message = "Analiza rozpoczęta.", status = "running" });
        }

        [HttpGet("{id}/analyze/progress")]
        public ActionResult<object> GetAnalysisProgress(int id)
        {
            var progress = _progressService.GetProgress(id);
            if (progress == null)
                return Ok(new { status = "idle", totalEquipment = 0, completedEquipment = 0, currentEquipmentName = "", percentage = 0 });

            var percentage = progress.TotalEquipment > 0
                ? (int)Math.Round(100.0 * progress.CompletedEquipment / progress.TotalEquipment)
                : 0;

            return Ok(new
            {
                status = progress.Status,
                totalEquipment = progress.TotalEquipment,
                completedEquipment = progress.CompletedEquipment,
                currentEquipmentName = progress.CurrentEquipmentName,
                percentage,
                errorMessage = progress.ErrorMessage
            });
        }

        [HttpPost("{id}/analyze/cancel")]
        public async Task<ActionResult<object>> CancelAnalysis(int id)
        {
            var progress = _progressService.GetProgress(id);
            if (progress == null || progress.Status != "running")
                return Ok(new { message = "Brak aktywnej analizy do anulowania." });

            _progressService.Cancel(id);

            // Update document status
            var doc = await _context.OPZDocuments.FindAsync(id);
            if (doc != null)
            {
                doc.AnalysisStatus = "Anulowano";
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Analiza została anulowana.", status = "cancelled" });
        }

        [HttpGet("{id}/matches")]
        public async Task<ActionResult<List<EquipmentMatchDto>>> GetOPZMatches(int id)
        {
            var matches = await _context.EquipmentMatches
                .Include(m => m.RequirementCompliances)
                .Include(m => m.EquipmentModel)
                    .ThenInclude(m => m.Manufacturer)
                .Include(m => m.EquipmentModel)
                    .ThenInclude(m => m.Type)
                .Where(m => m.OPZId == id)
                .OrderByDescending(m => m.MatchScore)
                .ToListAsync();

            return Ok(_mapper.Map<List<EquipmentMatchDto>>(matches));
        }

        [HttpPost("{id}/reprocess")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> ReprocessOPZ(int id)
        {
            var opzDocument = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (opzDocument == null)
                throw new NotFoundException("OPZDocument", id);

            if (!System.IO.File.Exists(opzDocument.FilePath))
                return BadRequest(new { message = "Plik PDF nie istnieje na dysku. Nie można ponownie przetworzyć." });

            // Remove old requirements and matches
            _context.OPZRequirements.RemoveRange(opzDocument.OPZRequirements);

            var oldMatches = await _context.EquipmentMatches
                .Include(m => m.RequirementCompliances)
                .Where(m => m.OPZId == id)
                .ToListAsync();
            foreach (var match in oldMatches)
            {
                _context.RequirementCompliances.RemoveRange(match.RequirementCompliances);
            }
            _context.EquipmentMatches.RemoveRange(oldMatches);
            await _context.SaveChangesAsync();

            // Re-extract requirements using current LLM
            var pdfText = await _pdfProcessingService.ExtractTextFromPdfAsync(opzDocument.FilePath);
            var requirements = await _pdfProcessingService.ExtractOPZRequirementsAsync(pdfText);

            foreach (var requirement in requirements)
            {
                requirement.OPZId = opzDocument.Id;
                _context.OPZRequirements.Add(requirement);
            }

            opzDocument.AnalysisStatus = "Przetworzony";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Ponowne przetworzenie zakończone. Wyodrębniono {requirements.Count} wymagań.",
                requirementsCount = requirements.Count
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> DeleteOPZ(int id)
        {
            var opzDocument = await _context.OPZDocuments
                .Include(d => d.OPZRequirements)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (opzDocument == null)
                throw new NotFoundException("OPZDocument", id);

            _context.OPZRequirements.RemoveRange(opzDocument.OPZRequirements);

            var matches = await _context.EquipmentMatches
                .Where(m => m.OPZId == id)
                .ToListAsync();
            _context.EquipmentMatches.RemoveRange(matches);

            _context.OPZDocuments.Remove(opzDocument);

            if (System.IO.File.Exists(opzDocument.FilePath))
            {
                System.IO.File.Delete(opzDocument.FilePath);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Dokument OPZ został usunięty." });
        }
    }
}

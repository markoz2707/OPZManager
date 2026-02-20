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

        public OPZController(
            IPdfProcessingService pdfProcessingService,
            IEquipmentMatchingService equipmentMatchingService,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _pdfProcessingService = pdfProcessingService;
            _equipmentMatchingService = equipmentMatchingService;
            _context = context;
            _mapper = mapper;
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

            var requirements = await _pdfProcessingService.ExtractOPZRequirementsAsync(filePath);

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

        [HttpGet("{id}/matches")]
        public async Task<ActionResult<List<EquipmentMatchDto>>> GetOPZMatches(int id)
        {
            var matches = await _context.EquipmentMatches
                .Include(m => m.EquipmentModel)
                    .ThenInclude(m => m.Manufacturer)
                .Include(m => m.EquipmentModel)
                    .ThenInclude(m => m.Type)
                .Where(m => m.OPZId == id)
                .OrderByDescending(m => m.MatchScore)
                .ToListAsync();

            return Ok(_mapper.Map<List<EquipmentMatchDto>>(matches));
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

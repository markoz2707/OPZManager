using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPZManager.API.DTOs.KnowledgeBase;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/equipment/models/{modelId}/knowledge")]
    [Authorize]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public KnowledgeBaseController(
            IKnowledgeBaseService knowledgeBaseService,
            IMapper mapper,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _mapper = mapper;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeDocumentDto>> Upload(int modelId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie przesłano pliku." });

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Dozwolone są tylko pliki PDF." });

            var maxSizeMB = int.Parse(_configuration["FileStorage:MaxFileSizeMB"] ?? "50");
            if (file.Length > maxSizeMB * 1024 * 1024)
                return BadRequest(new { message = $"Plik jest za duży. Maksymalny rozmiar: {maxSizeMB} MB." });

            using var stream = file.OpenReadStream();
            var document = await _knowledgeBaseService.UploadDocumentAsync(modelId, stream, file.FileName);
            var dto = _mapper.Map<KnowledgeDocumentDto>(document);

            return CreatedAtAction(nameof(GetDocuments), new { modelId }, dto);
        }

        [HttpGet]
        public async Task<ActionResult<List<KnowledgeDocumentDto>>> GetDocuments(int modelId)
        {
            var documents = await _knowledgeBaseService.GetDocumentsAsync(modelId);
            var dtos = _mapper.Map<List<KnowledgeDocumentDto>>(documents);
            return Ok(dtos);
        }

        [HttpDelete("{docId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int modelId, int docId)
        {
            var deleted = await _knowledgeBaseService.DeleteDocumentAsync(docId);
            if (!deleted)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            return NoContent();
        }

        [HttpPost("{docId}/reprocess")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Reprocess(int modelId, int docId)
        {
            var document = await _knowledgeBaseService.GetDocumentAsync(docId);
            if (document == null)
                return NotFound(new { message = "Dokument nie został znaleziony." });

            // Start reprocessing in background
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseService>();
                await service.ProcessDocumentAsync(docId);
            });

            return Ok(new { message = "Ponowne przetwarzanie dokumentu zostało uruchomione." });
        }

        [HttpPost("search")]
        public async Task<ActionResult<List<KnowledgeSearchResultDto>>> Search(int modelId, [FromBody] KnowledgeSearchRequestDto request)
        {
            var results = await _knowledgeBaseService.SearchAsync(modelId, request.Query, request.TopK);
            var dtos = _mapper.Map<List<KnowledgeSearchResultDto>>(results);
            return Ok(dtos);
        }
    }
}

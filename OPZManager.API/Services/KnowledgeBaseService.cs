using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;
using OPZManager.API.Services.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace OPZManager.API.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfProcessingService _pdfService;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KnowledgeBaseService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public KnowledgeBaseService(
            ApplicationDbContext context,
            IPdfProcessingService pdfService,
            IEmbeddingProvider embeddingProvider,
            IConfiguration configuration,
            ILogger<KnowledgeBaseService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _pdfService = pdfService;
            _embeddingProvider = embeddingProvider;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<KnowledgeDocument> UploadDocumentAsync(int equipmentModelId, Stream fileStream, string filename)
        {
            var knowledgeBasePath = _configuration["FileStorage:KnowledgeBasePath"] ?? "KnowledgeBase";
            var modelDir = Path.Combine(knowledgeBasePath, equipmentModelId.ToString());
            Directory.CreateDirectory(modelDir);

            var safeFilename = $"{Guid.NewGuid()}_{Path.GetFileName(filename)}";
            var filePath = Path.Combine(modelDir, safeFilename);

            await using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            var fileInfo = new FileInfo(filePath);

            var document = new KnowledgeDocument
            {
                EquipmentModelId = equipmentModelId,
                OriginalFilename = filename,
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                Status = "Oczekuje",
                UploadedAt = DateTime.UtcNow
            };

            _context.KnowledgeDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Start processing in background
            var documentId = document.Id;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseService>();
                await service.ProcessDocumentAsync(documentId);
            });

            return document;
        }

        public async Task<List<KnowledgeDocument>> GetDocumentsAsync(int equipmentModelId)
        {
            return await _context.KnowledgeDocuments
                .Where(d => d.EquipmentModelId == equipmentModelId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<KnowledgeDocument?> GetDocumentAsync(int documentId)
        {
            return await _context.KnowledgeDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            var document = await _context.KnowledgeDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return false;

            // Delete physical file
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            _context.KnowledgeDocuments.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ProcessDocumentAsync(int documentId)
        {
            var document = await _context.KnowledgeDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogError("KnowledgeDocument {Id} not found for processing", documentId);
                return;
            }

            try
            {
                document.Status = "Przetwarzanie";
                document.ErrorMessage = null;
                await _context.SaveChangesAsync();

                // Extract text from PDF
                var text = await _pdfService.ExtractTextFromPdfAsync(document.FilePath);

                if (string.IsNullOrWhiteSpace(text))
                {
                    document.Status = "Błąd";
                    document.ErrorMessage = "Nie udało się wyodrębnić tekstu z pliku PDF.";
                    await _context.SaveChangesAsync();
                    return;
                }

                // Chunk the text
                var chunkSize = int.Parse(_configuration["EmbeddingSettings:ChunkSize"] ?? "500");
                var chunkOverlap = int.Parse(_configuration["EmbeddingSettings:ChunkOverlap"] ?? "50");
                var chunks = TextChunker.ChunkText(text, chunkSize, chunkOverlap);

                if (chunks.Count == 0)
                {
                    document.Status = "Błąd";
                    document.ErrorMessage = "Tekst PDF nie zawiera treści do przetworzenia.";
                    await _context.SaveChangesAsync();
                    return;
                }

                // Remove old chunks if reprocessing
                if (document.Chunks.Any())
                {
                    _context.KnowledgeChunks.RemoveRange(document.Chunks);
                    await _context.SaveChangesAsync();
                }

                // Generate embeddings in batches
                const int batchSize = 20;
                var allChunkEntities = new List<KnowledgeChunk>();

                for (int i = 0; i < chunks.Count; i += batchSize)
                {
                    var batch = chunks.Skip(i).Take(batchSize).ToList();
                    var embeddings = await _embeddingProvider.GenerateEmbeddingsAsync(batch);

                    for (int j = 0; j < batch.Count; j++)
                    {
                        var chunkEntity = new KnowledgeChunk
                        {
                            KnowledgeDocumentId = document.Id,
                            ChunkIndex = i + j,
                            Content = batch[j],
                            TokenCount = TextChunker.EstimateTokenCount(batch[j]),
                            Embedding = new Vector(embeddings[j])
                        };
                        allChunkEntities.Add(chunkEntity);
                    }
                }

                _context.KnowledgeChunks.AddRange(allChunkEntities);

                document.ChunkCount = allChunkEntities.Count;
                document.Status = "Zindeksowany";
                document.IndexedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("KnowledgeDocument {Id} processed successfully: {ChunkCount} chunks", document.Id, document.ChunkCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing KnowledgeDocument {Id}", document.Id);
                document.Status = "Błąd";
                document.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<KnowledgeSearchResult>> SearchAsync(int equipmentModelId, string query, int topK = 5)
        {
            // Generate embedding for query
            var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(query);
            var queryVector = new Vector(queryEmbedding);

            // Search using pgvector cosine distance
            var results = await _context.KnowledgeChunks
                .Include(c => c.KnowledgeDocument)
                .Where(c => c.KnowledgeDocument.EquipmentModelId == equipmentModelId
                         && c.KnowledgeDocument.Status == "Zindeksowany"
                         && c.Embedding != null)
                .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
                .Take(topK)
                .Select(c => new KnowledgeSearchResult
                {
                    ChunkId = c.Id,
                    DocumentId = c.KnowledgeDocumentId,
                    DocumentFilename = c.KnowledgeDocument.OriginalFilename,
                    Content = c.Content,
                    Score = 1.0 - c.Embedding!.CosineDistance(queryVector),
                    ChunkIndex = c.ChunkIndex
                })
                .ToListAsync();

            return results;
        }
    }
}

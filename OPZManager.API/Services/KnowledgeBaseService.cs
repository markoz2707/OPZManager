using System.Text.Json;
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
        private readonly IPllumIntegrationService _pllumService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KnowledgeBaseService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public KnowledgeBaseService(
            ApplicationDbContext context,
            IPdfProcessingService pdfService,
            IEmbeddingProvider embeddingProvider,
            IPllumIntegrationService pllumService,
            IConfiguration configuration,
            ILogger<KnowledgeBaseService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _pdfService = pdfService;
            _embeddingProvider = embeddingProvider;
            _pllumService = pllumService;
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
                document.ProcessingProgress = 0;
                document.ProcessingStep = "Ekstrakcja tekstu z PDF...";
                await _context.SaveChangesAsync();

                // Step 1: Extract text from PDF (0% → 15%)
                var text = await _pdfService.ExtractTextFromPdfAsync(document.FilePath);

                if (string.IsNullOrWhiteSpace(text))
                {
                    document.Status = "Błąd";
                    document.ErrorMessage = "Nie udało się wyodrębnić tekstu z pliku PDF.";
                    document.ProcessingProgress = 0;
                    document.ProcessingStep = null;
                    await _context.SaveChangesAsync();
                    return;
                }

                document.ProcessingProgress = 15;
                document.ProcessingStep = "Dzielenie tekstu na fragmenty...";
                await _context.SaveChangesAsync();

                // Step 2: Chunk the text (15% → 20%)
                var chunkSize = int.Parse(_configuration["EmbeddingSettings:ChunkSize"] ?? "500");
                var chunkOverlap = int.Parse(_configuration["EmbeddingSettings:ChunkOverlap"] ?? "50");
                var chunks = TextChunker.ChunkText(text, chunkSize, chunkOverlap);

                if (chunks.Count == 0)
                {
                    document.Status = "Błąd";
                    document.ErrorMessage = "Tekst PDF nie zawiera treści do przetworzenia.";
                    document.ProcessingProgress = 0;
                    document.ProcessingStep = null;
                    await _context.SaveChangesAsync();
                    return;
                }

                document.ProcessingProgress = 20;
                document.ProcessingStep = $"Przygotowanie {chunks.Count} fragmentów...";
                await _context.SaveChangesAsync();

                // Remove old chunks if reprocessing
                if (document.Chunks.Any())
                {
                    _context.KnowledgeChunks.RemoveRange(document.Chunks);
                    await _context.SaveChangesAsync();
                }

                // Step 3: Generate embeddings in batches (20% → 95%)
                const int batchSize = 20;
                var allChunkEntities = new List<KnowledgeChunk>();
                var totalBatches = (int)Math.Ceiling(chunks.Count / (double)batchSize);

                for (int i = 0; i < chunks.Count; i += batchSize)
                {
                    var currentBatch = i / batchSize + 1;
                    var processedChunks = Math.Min(i + batchSize, chunks.Count);

                    document.ProcessingProgress = 20 + (int)(75.0 * processedChunks / chunks.Count);
                    document.ProcessingStep = $"Generowanie embeddingów: {processedChunks}/{chunks.Count} fragmentów (batch {currentBatch}/{totalBatches})";
                    await _context.SaveChangesAsync();

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

                // Step 4: Save to database (95% → 100%)
                document.ProcessingProgress = 95;
                document.ProcessingStep = "Zapisywanie do bazy danych...";
                await _context.SaveChangesAsync();

                _context.KnowledgeChunks.AddRange(allChunkEntities);

                document.ChunkCount = allChunkEntities.Count;
                document.Status = "Zindeksowany";
                document.ProcessingProgress = 100;
                document.ProcessingStep = null;
                document.IndexedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("KnowledgeDocument {Id} processed successfully: {ChunkCount} chunks", document.Id, document.ChunkCount);

                // Step 5: Extract equipment specs via LLM and merge into EquipmentModel
                try
                {
                    var extractedSpecs = await _pllumService.ExtractEquipmentSpecsAsync(text);
                    if (extractedSpecs.Count > 0)
                    {
                        var equipmentModel = await _context.EquipmentModels
                            .FirstOrDefaultAsync(m => m.Id == document.EquipmentModelId);

                        if (equipmentModel != null)
                        {
                            var existingSpecs = equipmentModel.Specifications ?? new Dictionary<string, object>();
                            foreach (var spec in extractedSpecs)
                            {
                                existingSpecs[spec.Key] = spec.Value;
                            }
                            equipmentModel.SpecificationsJson = JsonSerializer.Serialize(existingSpecs);
                            equipmentModel.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Merged {Count} extracted specs into EquipmentModel {ModelId}", extractedSpecs.Count, document.EquipmentModelId);
                        }
                    }
                }
                catch (Exception specEx)
                {
                    _logger.LogWarning(specEx, "Failed to extract equipment specs from KnowledgeDocument {Id}, embeddings are still valid", document.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing KnowledgeDocument {Id}", document.Id);
                document.Status = "Błąd";
                document.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                document.ProcessingProgress = 0;
                document.ProcessingStep = null;
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

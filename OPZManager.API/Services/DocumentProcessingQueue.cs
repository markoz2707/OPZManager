using System.Threading.Channels;

namespace OPZManager.API.Services
{
    public interface IDocumentProcessingQueue
    {
        void Enqueue(int documentId);
        int PendingCount { get; }
    }

    public class DocumentProcessingQueue : BackgroundService, IDocumentProcessingQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DocumentProcessingQueue> _logger;
        private readonly TimeSpan _delayBetweenDocuments;
        private int _pendingCount;

        public int PendingCount => _pendingCount;

        public DocumentProcessingQueue(
            IServiceScopeFactory scopeFactory,
            ILogger<DocumentProcessingQueue> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var delayMs = int.Parse(configuration["EmbeddingSettings:QueueDelayMs"] ?? "500");
            _delayBetweenDocuments = TimeSpan.FromMilliseconds(delayMs);
        }

        public void Enqueue(int documentId)
        {
            Interlocked.Increment(ref _pendingCount);
            if (!_channel.Writer.TryWrite(documentId))
            {
                Interlocked.Decrement(ref _pendingCount);
                _logger.LogError("Failed to enqueue document {DocumentId} for processing", documentId);
            }
            else
            {
                _logger.LogInformation("Document {DocumentId} enqueued for processing ({Pending} in queue)", documentId, _pendingCount);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DocumentProcessingQueue started");

            // Recovery: re-enqueue documents stuck in non-terminal states after restart
            await RecoverPendingDocumentsAsync(stoppingToken);

            await foreach (var documentId in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Processing document {DocumentId} from queue ({Pending} remaining)", documentId, _pendingCount);

                    using var scope = _scopeFactory.CreateScope();
                    var knowledgeBaseService = scope.ServiceProvider.GetRequiredService<IKnowledgeBaseService>();
                    await knowledgeBaseService.ProcessDocumentAsync(documentId);

                    _logger.LogInformation("Document {DocumentId} processing complete", documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing document {DocumentId} from queue", documentId);
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingCount);
                }

                // Delay between documents to avoid rate limiting
                if (_delayBetweenDocuments > TimeSpan.Zero)
                {
                    await Task.Delay(_delayBetweenDocuments, stoppingToken);
                }
            }

            _logger.LogInformation("DocumentProcessingQueue stopped");
        }

        private async Task RecoverPendingDocumentsAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var stuckDocuments = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .ToListAsync(
                        context.KnowledgeDocuments
                            .Where(d => d.Status == "Oczekuje" || d.Status == "Przetwarzanie")
                            .OrderBy(d => d.Id)
                            .Select(d => d.Id),
                        stoppingToken);

                if (stuckDocuments.Count > 0)
                {
                    _logger.LogInformation("Recovery: found {Count} documents in non-terminal state, re-enqueuing", stuckDocuments.Count);
                    foreach (var docId in stuckDocuments)
                    {
                        Enqueue(docId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery: failed to query pending documents");
            }
        }
    }
}

using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IKnowledgeBaseService
    {
        Task<KnowledgeDocument> UploadDocumentAsync(int equipmentModelId, Stream fileStream, string filename);
        Task<List<KnowledgeDocument>> GetDocumentsAsync(int equipmentModelId);
        Task<KnowledgeDocument?> GetDocumentAsync(int documentId);
        Task<bool> DeleteDocumentAsync(int documentId);
        Task ProcessDocumentAsync(int documentId);
        Task<List<KnowledgeSearchResult>> SearchAsync(int equipmentModelId, string query, int topK = 5);
    }

    public class KnowledgeSearchResult
    {
        public int ChunkId { get; set; }
        public int DocumentId { get; set; }
        public string DocumentFilename { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public int ChunkIndex { get; set; }
    }
}

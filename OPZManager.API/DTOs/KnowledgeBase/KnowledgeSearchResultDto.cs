namespace OPZManager.API.DTOs.KnowledgeBase
{
    public class KnowledgeSearchResultDto
    {
        public int ChunkId { get; set; }
        public int DocumentId { get; set; }
        public string DocumentFilename { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public int ChunkIndex { get; set; }
    }
}

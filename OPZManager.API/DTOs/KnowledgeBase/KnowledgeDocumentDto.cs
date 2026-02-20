namespace OPZManager.API.DTOs.KnowledgeBase
{
    public class KnowledgeDocumentDto
    {
        public int Id { get; set; }
        public int EquipmentModelId { get; set; }
        public string OriginalFilename { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? IndexedAt { get; set; }
    }
}

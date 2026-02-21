using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class KnowledgeDocument
    {
        public int Id { get; set; }

        [Required]
        public int EquipmentModelId { get; set; }

        [Required]
        [StringLength(500)]
        public string OriginalFilename { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Oczekuje"; // Oczekuje, Przetwarzanie, Zindeksowany, Błąd

        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        public int ChunkCount { get; set; }

        /// <summary>Processing progress 0-100%</summary>
        public int ProcessingProgress { get; set; }

        /// <summary>Current processing step description</summary>
        [StringLength(200)]
        public string? ProcessingStep { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? IndexedAt { get; set; }

        // Navigation properties
        public virtual EquipmentModel EquipmentModel { get; set; } = null!;
        public virtual ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
    }
}

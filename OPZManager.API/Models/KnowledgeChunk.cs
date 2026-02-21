using System.ComponentModel.DataAnnotations;
using Pgvector;

namespace OPZManager.API.Models
{
    public class KnowledgeChunk
    {
        public int Id { get; set; }

        [Required]
        public int KnowledgeDocumentId { get; set; }

        public int ChunkIndex { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int TokenCount { get; set; }

        public Vector? Embedding { get; set; }

        // Navigation properties
        public virtual KnowledgeDocument KnowledgeDocument { get; set; } = null!;
    }
}

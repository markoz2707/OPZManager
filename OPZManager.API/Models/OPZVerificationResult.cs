using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class OPZVerificationResult
    {
        public int Id { get; set; }

        [Required]
        public int OPZDocumentId { get; set; }

        public int OverallScore { get; set; } // 0-100

        [StringLength(1)]
        public string Grade { get; set; } = "F"; // A, B, C, D, F

        public string? CompletenessJson { get; set; }
        public string? ComplianceJson { get; set; }
        public string? TechnicalJson { get; set; }
        public string? GapAnalysisJson { get; set; }
        public string? SummaryText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual OPZDocument OPZDocument { get; set; } = null!;
    }
}

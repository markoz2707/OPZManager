using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class OPZDocument
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Filename { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        public int? UserId { get; set; }

        [StringLength(36)]
        public string? AnonymousSessionId { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string AnalysisStatus { get; set; } = "Uploaded"; // Uploaded, Analyzing, Completed, Failed

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<OPZRequirement> OPZRequirements { get; set; } = new List<OPZRequirement>();
        public virtual ICollection<EquipmentMatch> EquipmentMatches { get; set; } = new List<EquipmentMatch>();
        public virtual OPZVerificationResult? VerificationResult { get; set; }
        public virtual ICollection<LeadCapture> LeadCaptures { get; set; } = new List<LeadCapture>();
    }
}

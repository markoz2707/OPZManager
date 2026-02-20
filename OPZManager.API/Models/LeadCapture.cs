using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class LeadCapture
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        public bool MarketingConsent { get; set; }

        [Required]
        [StringLength(36)]
        public string AnonymousSessionId { get; set; } = string.Empty;

        public int? OPZDocumentId { get; set; }

        [StringLength(50)]
        public string Source { get; set; } = "verification"; // verification, generation

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(100)]
        public string? DownloadToken { get; set; }

        public DateTime? DownloadTokenExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual OPZDocument? OPZDocument { get; set; }
    }
}

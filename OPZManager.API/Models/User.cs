using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User"; // Admin, User, External

        // Contact fields
        [StringLength(200)]
        public string? FullName { get; set; }

        [StringLength(200)]
        public string? Company { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? NIP { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public bool MarketingConsent { get; set; } = false;
        public DateTime? MarketingConsentDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public virtual ICollection<OPZDocument> OPZDocuments { get; set; } = new List<OPZDocument>();
    }
}

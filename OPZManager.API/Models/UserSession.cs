using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class UserSession
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string JwtToken { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}

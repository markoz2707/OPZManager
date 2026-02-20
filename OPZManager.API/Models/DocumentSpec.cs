using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class DocumentSpec
    {
        public int Id { get; set; }
        
        [Required]
        public int DocumentId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SpecKey { get; set; } = string.Empty; // e.g., "RAM", "Storage", "RAID"
        
        [Required]
        public string SpecValue { get; set; } = string.Empty; // e.g., "32GB", "10TB", "RAID 6"
        
        [StringLength(50)]
        public string SpecType { get; set; } = "Text"; // Text, Number, Boolean
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Document Document { get; set; } = null!;
    }
}

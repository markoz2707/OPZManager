using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class EquipmentMatch
    {
        public int Id { get; set; }
        
        [Required]
        public int OPZId { get; set; }
        
        [Required]
        public int ModelId { get; set; }
        
        public decimal MatchScore { get; set; } = 0.0m; // 0.0 to 1.0
        
        [Required]
        public string ComplianceDescription { get; set; } = string.Empty; // How the equipment meets requirements
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual OPZDocument OPZDocument { get; set; } = null!;
        public virtual EquipmentModel EquipmentModel { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class Manufacturer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<EquipmentModel> EquipmentModels { get; set; } = new List<EquipmentModel>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}

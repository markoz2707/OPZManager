using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OPZManager.API.Models
{
    public class EquipmentModel
    {
        public int Id { get; set; }
        
        [Required]
        public int ManufacturerId { get; set; }
        
        [Required]
        public int TypeId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string ModelName { get; set; } = string.Empty; // e.g., "ME5012"
        
        public string SpecificationsJson { get; set; } = "{}"; // JSON with technical specs
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Manufacturer Manufacturer { get; set; } = null!;
        public virtual EquipmentType Type { get; set; } = null!;
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<EquipmentMatch> EquipmentMatches { get; set; } = new List<EquipmentMatch>();
        
        // Helper property to work with specifications as object
        public Dictionary<string, object>? Specifications
        {
            get => string.IsNullOrEmpty(SpecificationsJson) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(SpecificationsJson);
            set => SpecificationsJson = JsonSerializer.Serialize(value ?? new Dictionary<string, object>());
        }
    }
}

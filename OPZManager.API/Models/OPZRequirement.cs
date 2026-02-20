using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OPZManager.API.Models
{
    public class OPZRequirement
    {
        public int Id { get; set; }
        
        [Required]
        public int OPZId { get; set; }
        
        [Required]
        public string RequirementText { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string RequirementType { get; set; } = "General"; // General, Technical, Performance, Compliance
        
        public string ExtractedSpecsJson { get; set; } = "{}"; // JSON with extracted specifications
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual OPZDocument OPZDocument { get; set; } = null!;
        
        // Helper property to work with extracted specs as object
        public Dictionary<string, object>? ExtractedSpecs
        {
            get => string.IsNullOrEmpty(ExtractedSpecsJson) 
                ? new Dictionary<string, object>() 
                : JsonSerializer.Deserialize<Dictionary<string, object>>(ExtractedSpecsJson);
            set => ExtractedSpecsJson = JsonSerializer.Serialize(value ?? new Dictionary<string, object>());
        }
    }
}

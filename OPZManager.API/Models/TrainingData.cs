using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class TrainingData
    {
        public int Id { get; set; }
        
        [Required]
        public string Question { get; set; } = string.Empty;
        
        [Required]
        public string Answer { get; set; } = string.Empty;
        
        public string Context { get; set; } = string.Empty; // Additional context for the Q&A pair
        
        [StringLength(50)]
        public string DataType { get; set; } = "QA"; // QA, RequirementMatch, SpecExtraction
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

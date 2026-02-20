using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.DTOs.KnowledgeBase
{
    public class KnowledgeSearchRequestDto
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Query { get; set; } = string.Empty;

        public int TopK { get; set; } = 5;
    }
}

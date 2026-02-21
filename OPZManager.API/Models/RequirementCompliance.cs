using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class RequirementCompliance
    {
        public int Id { get; set; }

        [Required]
        public int EquipmentMatchId { get; set; }

        [Required]
        public int RequirementId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "not_applicable"; // met, partial, not_met, not_applicable

        public string? Explanation { get; set; }

        // Navigation properties
        public virtual EquipmentMatch EquipmentMatch { get; set; } = null!;
        public virtual OPZRequirement OPZRequirement { get; set; } = null!;
    }
}

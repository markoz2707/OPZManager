using OPZManager.API.DTOs.Equipment;

namespace OPZManager.API.DTOs.OPZ
{
    public class RequirementComplianceDto
    {
        public int RequirementId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Explanation { get; set; }
    }

    public class EquipmentMatchDto
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ManufacturerName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public decimal MatchScore { get; set; }
        public string ComplianceDescription { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<RequirementComplianceDto> RequirementCompliances { get; set; } = new();
    }
}

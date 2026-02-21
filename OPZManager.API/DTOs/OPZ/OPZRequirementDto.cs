namespace OPZManager.API.DTOs.OPZ
{
    public class OPZRequirementDto
    {
        public int Id { get; set; }
        public string RequirementText { get; set; } = string.Empty;
        public string RequirementType { get; set; } = string.Empty;
        public string ExtractedSpecsJson { get; set; } = "{}";
        public string DeviceCategory { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

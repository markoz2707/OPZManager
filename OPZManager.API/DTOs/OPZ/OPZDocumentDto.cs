namespace OPZManager.API.DTOs.OPZ
{
    public class OPZDocumentDto
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string AnalysisStatus { get; set; } = string.Empty;
        public int RequirementsCount { get; set; }
        public int MatchesCount { get; set; }
    }

    public class OPZDocumentDetailDto
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string AnalysisStatus { get; set; } = string.Empty;
        public List<OPZRequirementDto> Requirements { get; set; } = new();
        public List<EquipmentMatchDto> Matches { get; set; } = new();
    }
}

namespace OPZManager.API.DTOs.Public
{
    public class PublicOPZDocumentDto
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string AnalysisStatus { get; set; } = string.Empty;
        public int RequirementsCount { get; set; }
        public int MatchesCount { get; set; }
    }
}

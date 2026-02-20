namespace OPZManager.API.DTOs.Public
{
    public class VerificationResultDto
    {
        public int Id { get; set; }
        public int OPZDocumentId { get; set; }
        public int OverallScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public CompletenessResultDto? Completeness { get; set; }
        public ComplianceResultDto? Compliance { get; set; }
        public TechnicalResultDto? Technical { get; set; }
        public GapAnalysisResultDto? GapAnalysis { get; set; }
        public string? SummaryText { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CompletenessResultDto
    {
        public int Score { get; set; }
        public List<SectionCheckDto> Sections { get; set; } = new();
    }

    public class SectionCheckDto
    {
        public string Name { get; set; } = string.Empty;
        public bool Found { get; set; }
        public string? Details { get; set; }
    }

    public class ComplianceResultDto
    {
        public int Score { get; set; }
        public List<NormCheckDto> Norms { get; set; } = new();
    }

    public class NormCheckDto
    {
        public string Name { get; set; } = string.Empty;
        public bool Referenced { get; set; }
        public string? Details { get; set; }
    }

    public class TechnicalResultDto
    {
        public int Score { get; set; }
        public int MeasurableParams { get; set; }
        public int TotalParams { get; set; }
        public int QualifiersUsed { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    public class GapAnalysisResultDto
    {
        public int Score { get; set; }
        public List<string> MissingSections { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}

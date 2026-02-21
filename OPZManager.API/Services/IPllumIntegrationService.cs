using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class LlmExtractedRequirement
    {
        public string Category { get; set; } = "General";
        public string Requirement { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public Dictionary<string, string> Specs { get; set; } = new();
    }

    public class LlmEquipmentMatchScore
    {
        public int Score { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class LlmRequirementInput
    {
        public int RequirementId { get; set; }
        public string Device { get; set; } = string.Empty;
        public string RequirementText { get; set; } = string.Empty;
    }

    public class LlmRequirementCompliance
    {
        public int RequirementId { get; set; }
        public string Status { get; set; } = "not_applicable"; // met, partial, not_met, not_applicable
        public string Explanation { get; set; } = string.Empty;
    }

    public class LlmDetailedMatchResult
    {
        public int OverallScore { get; set; }
        public string OverallExplanation { get; set; } = string.Empty;
        public List<LlmRequirementCompliance> Requirements { get; set; } = new();
    }

    public interface IPllumIntegrationService
    {
        Task<string> AnalyzeOPZRequirementsAsync(string requirementText);
        Task<List<EquipmentModel>> GetEquipmentRecommendationsAsync(string requirements);
        Task<string> GenerateComplianceDescriptionAsync(EquipmentModel equipment, string requirements);
        Task<string> GenerateOPZContentAsync(List<EquipmentModel> selectedEquipment, string equipmentType);
        Task<string> VerifyOPZContentAsync(string pdfText);
        Task<bool> TestConnectionAsync();
        Task<List<LlmExtractedRequirement>> ExtractStructuredRequirementsAsync(string pdfText);
        Task<Dictionary<string, string>> ExtractEquipmentSpecsAsync(string documentText);
        Task<LlmEquipmentMatchScore> ScoreEquipmentMatchAsync(string requirements, string equipmentSpecs, string kbFragments);
        Task<LlmDetailedMatchResult> ScoreEquipmentMatchDetailedAsync(List<LlmRequirementInput> requirements, string equipmentSpecs, string kbFragments);
    }
}

using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class SWZRequirement
    {
        public string Category { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string MinValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCommon { get; set; } // true if ALL selected models meet this
    }

    public class SWZGenerationResult
    {
        public string Content { get; set; } = string.Empty;
        public List<SWZRequirement> ExtractedRequirements { get; set; } = new();
        public int MatchingModelsCount { get; set; }
    }

    public interface ISWZGenerationService
    {
        /// <summary>
        /// Extracts common technical parameters from selected equipment models
        /// to create requirements that ONLY those models satisfy.
        /// No manufacturer names or model-specific identifiers are included.
        /// </summary>
        Task<SWZGenerationResult> GenerateSWZAsync(List<EquipmentModel> selectedModels, string equipmentType);

        /// <summary>
        /// Uses LLM to generate a full SWZ document from extracted requirements.
        /// </summary>
        Task<string> GenerateSWZDocumentAsync(List<SWZRequirement> requirements, string equipmentType, int modelCount);

        /// <summary>
        /// Returns a preview for anonymous users: first 10 requirements visible, rest blurred.
        /// </summary>
        SWZPreviewResult GetPreviewForAnonymous(SWZGenerationResult fullResult);
    }

    public class SWZPreviewResult
    {
        public string PreviewContent { get; set; } = string.Empty;
        public int TotalRequirements { get; set; }
        public int VisibleRequirements { get; set; }
        public List<SWZRequirement> VisibleItems { get; set; } = new();
    }
}

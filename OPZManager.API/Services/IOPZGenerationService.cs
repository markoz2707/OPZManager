using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class ContentPreviewResult
    {
        public string Preview { get; set; } = string.Empty;
        public string FullContent { get; set; } = string.Empty;
    }

    public interface IOPZGenerationService
    {
        Task<string> GenerateOPZContentAsync(List<EquipmentModel> selectedEquipment, string equipmentType);
        Task<byte[]> GenerateOPZPdfAsync(string content, string title);
        Task<string> GenerateComplianceRequirementsAsync(List<EquipmentModel> equipment);
        Task<string> GenerateTechnicalSpecificationsAsync(List<EquipmentModel> equipment);
        ContentPreviewResult SplitContentForPreview(string fullContent);
    }
}

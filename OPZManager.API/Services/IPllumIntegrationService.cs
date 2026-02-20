using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IPllumIntegrationService
    {
        Task<string> AnalyzeOPZRequirementsAsync(string requirementText);
        Task<List<EquipmentModel>> GetEquipmentRecommendationsAsync(string requirements);
        Task<string> GenerateComplianceDescriptionAsync(EquipmentModel equipment, string requirements);
        Task<string> GenerateOPZContentAsync(List<EquipmentModel> selectedEquipment, string equipmentType);
        Task<string> VerifyOPZContentAsync(string pdfText);
        Task<bool> TestConnectionAsync();
    }
}

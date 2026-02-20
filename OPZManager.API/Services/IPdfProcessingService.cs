using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IPdfProcessingService
    {
        Task<string> ExtractTextFromPdfAsync(string filePath);
        Task<Dictionary<string, object>> ExtractSpecificationsAsync(string pdfText, string equipmentType);
        Task<List<OPZRequirement>> ExtractOPZRequirementsAsync(string pdfText);
        Task<bool> IndexDocumentAsync(Document document);
        Task<byte[]> GenerateOPZPdfAsync(string content, string title);
    }
}

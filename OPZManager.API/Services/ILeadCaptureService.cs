using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface ILeadCaptureService
    {
        Task<LeadCapture> CaptureLeadAsync(string email, bool marketingConsent, string sessionId, int? opzDocumentId, string source, string? ipAddress);
        Task<bool> HasValidLeadForSessionAsync(string sessionId);
        Task<LeadCapture?> ValidateDownloadTokenAsync(string downloadToken);
    }
}

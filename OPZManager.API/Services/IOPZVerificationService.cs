using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IOPZVerificationService
    {
        Task<OPZVerificationResult> VerifyDocumentAsync(OPZDocument document);
        Task<OPZVerificationResult?> GetVerificationResultAsync(int opzDocumentId);
    }
}

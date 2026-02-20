using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class LeadCaptureService : ILeadCaptureService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeadCaptureService> _logger;
        private static readonly TimeSpan TokenValidity = TimeSpan.FromMinutes(30);

        public LeadCaptureService(ApplicationDbContext context, ILogger<LeadCaptureService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LeadCapture> CaptureLeadAsync(
            string email, bool marketingConsent, string sessionId,
            int? opzDocumentId, string source, string? ipAddress)
        {
            // Check if session already has a lead with a valid token
            var existing = await _context.LeadCaptures
                .FirstOrDefaultAsync(l => l.AnonymousSessionId == sessionId
                    && l.DownloadTokenExpiresAt > DateTime.UtcNow);

            if (existing != null)
            {
                _logger.LogInformation("Returning existing lead for session {SessionId}", sessionId);
                return existing;
            }

            var token = Guid.NewGuid().ToString("N");
            var lead = new LeadCapture
            {
                Email = email,
                MarketingConsent = marketingConsent,
                AnonymousSessionId = sessionId,
                OPZDocumentId = opzDocumentId,
                Source = source,
                IpAddress = ipAddress,
                DownloadToken = token,
                DownloadTokenExpiresAt = DateTime.UtcNow.Add(TokenValidity)
            };

            _context.LeadCaptures.Add(lead);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Lead captured for {Email}, session {SessionId}", email, sessionId);
            return lead;
        }

        public async Task<bool> HasValidLeadForSessionAsync(string sessionId)
        {
            return await _context.LeadCaptures
                .AnyAsync(l => l.AnonymousSessionId == sessionId
                    && l.DownloadTokenExpiresAt > DateTime.UtcNow);
        }

        public async Task<LeadCapture?> ValidateDownloadTokenAsync(string downloadToken)
        {
            var lead = await _context.LeadCaptures
                .FirstOrDefaultAsync(l => l.DownloadToken == downloadToken
                    && l.DownloadTokenExpiresAt > DateTime.UtcNow);

            return lead;
        }
    }
}

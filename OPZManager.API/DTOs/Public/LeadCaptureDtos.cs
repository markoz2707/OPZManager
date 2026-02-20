namespace OPZManager.API.DTOs.Public
{
    public class LeadCaptureRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public bool MarketingConsent { get; set; }
        public int? OPZDocumentId { get; set; }
        public string Source { get; set; } = "verification";
    }

    public class LeadCaptureResponseDto
    {
        public string DownloadToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DownloadRequestDto
    {
        public string DownloadToken { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}

namespace OPZManager.API.DTOs.Admin
{
    public class DashboardStatsDto
    {
        public int TotalDocuments { get; set; }
        public int DocumentsLast30Days { get; set; }
        public int TotalVerifications { get; set; }
        public int VerificationsLast30Days { get; set; }
        public int TotalUsers { get; set; }
        public int UsersLast30Days { get; set; }
        public int TotalLeads { get; set; }
        public int LeadsLast30Days { get; set; }
        public double ConversionRate { get; set; }
        public int TotalEquipmentModels { get; set; }
        public int TotalKnowledgeDocs { get; set; }
    }

    public class ActivityDataDto
    {
        public string Date { get; set; } = string.Empty;
        public int Documents { get; set; }
        public int Verifications { get; set; }
        public int Registrations { get; set; }
        public int Leads { get; set; }
    }

    public class PopularEquipmentDto
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ManufacturerName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int MatchCount { get; set; }
    }

    public class LeadListDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool MarketingConsent { get; set; }
        public bool IsRegistered { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? IpAddress { get; set; }
    }
}

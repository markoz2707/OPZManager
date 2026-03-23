using System.Text;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Admin;

namespace OPZManager.API.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var totalDocs = await _context.OPZDocuments.CountAsync();
            var docsLast30 = await _context.OPZDocuments.CountAsync(d => d.UploadDate >= thirtyDaysAgo);
            var totalVerifications = await _context.OPZVerificationResults.CountAsync();
            var verificationsLast30 = await _context.OPZVerificationResults.CountAsync(v => v.CreatedAt >= thirtyDaysAgo);
            var totalUsers = await _context.Users.CountAsync();
            var usersLast30 = await _context.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);
            var totalLeads = await _context.LeadCaptures.CountAsync();
            var leadsLast30 = await _context.LeadCaptures.CountAsync(l => l.CreatedAt >= thirtyDaysAgo);
            var totalModels = await _context.EquipmentModels.CountAsync();
            var totalKBDocs = await _context.KnowledgeDocuments.CountAsync();

            // Conversion: leads whose email matches a registered user
            var leadEmails = await _context.LeadCaptures.Select(l => l.Email).Distinct().ToListAsync();
            var registeredLeads = totalLeads > 0
                ? await _context.Users.CountAsync(u => leadEmails.Contains(u.Email))
                : 0;

            return new DashboardStatsDto
            {
                TotalDocuments = totalDocs,
                DocumentsLast30Days = docsLast30,
                TotalVerifications = totalVerifications,
                VerificationsLast30Days = verificationsLast30,
                TotalUsers = totalUsers,
                UsersLast30Days = usersLast30,
                TotalLeads = totalLeads,
                LeadsLast30Days = leadsLast30,
                ConversionRate = totalLeads > 0 ? Math.Round((double)registeredLeads / totalLeads * 100, 1) : 0,
                TotalEquipmentModels = totalModels,
                TotalKnowledgeDocs = totalKBDocs,
            };
        }

        public async Task<List<ActivityDataDto>> GetActivityDataAsync(int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;
            var result = new List<ActivityDataDto>();

            for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
            {
                var nextDate = date.AddDays(1);
                result.Add(new ActivityDataDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Documents = await _context.OPZDocuments.CountAsync(d => d.UploadDate >= date && d.UploadDate < nextDate),
                    Verifications = await _context.OPZVerificationResults.CountAsync(v => v.CreatedAt >= date && v.CreatedAt < nextDate),
                    Registrations = await _context.Users.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate),
                    Leads = await _context.LeadCaptures.CountAsync(l => l.CreatedAt >= date && l.CreatedAt < nextDate),
                });
            }

            return result;
        }

        public async Task<List<PopularEquipmentDto>> GetPopularEquipmentAsync(int limit)
        {
            return await _context.EquipmentMatches
                .GroupBy(m => m.ModelId)
                .Select(g => new PopularEquipmentDto
                {
                    ModelId = g.Key,
                    MatchCount = g.Count(),
                    ModelName = g.First().EquipmentModel.ModelName,
                    ManufacturerName = g.First().EquipmentModel.Manufacturer.Name,
                    TypeName = g.First().EquipmentModel.Type.Name,
                })
                .OrderByDescending(x => x.MatchCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<(List<LeadListDto> Items, int TotalCount)> GetLeadsAsync(int page, int pageSize, string? search)
        {
            var query = _context.LeadCaptures.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.Email.Contains(search));

            var total = await query.CountAsync();
            var registeredEmails = await _context.Users.Select(u => u.Email).ToListAsync();

            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeadListDto
                {
                    Id = l.Id,
                    Email = l.Email,
                    Source = l.Source ?? "",
                    MarketingConsent = l.MarketingConsent,
                    CreatedAt = l.CreatedAt,
                    IpAddress = l.IpAddress,
                    IsRegistered = registeredEmails.Contains(l.Email),
                })
                .ToListAsync();

            return (items, total);
        }

        public async Task<byte[]> ExportLeadsCsvAsync()
        {
            var leads = await _context.LeadCaptures.OrderByDescending(l => l.CreatedAt).ToListAsync();
            var registeredEmails = await _context.Users.Select(u => u.Email).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Email;Data;Źródło;Zgoda marketingowa;Zarejestrowany;IP");

            foreach (var lead in leads)
            {
                var registered = registeredEmails.Contains(lead.Email) ? "Tak" : "Nie";
                var consent = lead.MarketingConsent ? "Tak" : "Nie";
                sb.AppendLine($"{lead.Email};{lead.CreatedAt:yyyy-MM-dd HH:mm};{lead.Source};{consent};{registered};{lead.IpAddress}");
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
    }
}

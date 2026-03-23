using OPZManager.API.DTOs.Admin;

namespace OPZManager.API.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<ActivityDataDto>> GetActivityDataAsync(int days);
        Task<List<PopularEquipmentDto>> GetPopularEquipmentAsync(int limit);
        Task<(List<LeadListDto> Items, int TotalCount)> GetLeadsAsync(int page, int pageSize, string? search);
        Task<byte[]> ExportLeadsCsvAsync();
    }
}

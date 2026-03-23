using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _analyticsService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity([FromQuery] int days = 30)
        {
            var data = await _analyticsService.GetActivityDataAsync(days);
            return Ok(data);
        }

        [HttpGet("popular-equipment")]
        public async Task<IActionResult> GetPopularEquipment([FromQuery] int limit = 10)
        {
            var data = await _analyticsService.GetPopularEquipmentAsync(limit);
            return Ok(data);
        }

        [HttpGet("leads")]
        public async Task<IActionResult> GetLeads([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var (items, total) = await _analyticsService.GetLeadsAsync(page, pageSize, search);
            return Ok(new { items, totalCount = total, page, pageSize });
        }

        [HttpGet("leads/export")]
        public async Task<IActionResult> ExportLeads()
        {
            var csv = await _analyticsService.ExportLeadsCsvAsync();
            return File(csv, "text/csv; charset=utf-8", $"leady_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}

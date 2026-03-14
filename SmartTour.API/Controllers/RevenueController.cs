using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // Giả sử chỉ Admin mới được xem
public class RevenueController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public RevenueController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<RevenueStatistics>> GetStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        return Ok(await _revenueService.GetStatisticsAsync(startDate, endDate));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int count = 100)
    {
        return Ok(await _revenueService.GetPaymentsAsync(startDate, endDate, count));
    }
}

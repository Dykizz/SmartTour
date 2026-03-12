using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<Subscription>> GetUserSubscription(int userId)
    {
        var subscription = await _subscriptionService.GetByUserIdAsync(userId);
        if (subscription == null) return NotFound("Chưa có gói dịch vụ nào.");
        return Ok(subscription);
    }

    [HttpPost("subscribe-default")]
    public async Task<IActionResult> SubscribeDefault([FromBody] SubscribeDefaultRequest request)
    {
        var result = await _subscriptionService.SubscribeDefaultPackageAsync(request.UserId);
        if (result)
        {
            return Ok(new { success = true, message = "Đã đăng ký gói mặc định thành công" });
        }
        return BadRequest(new { success = false, message = "Không thể đăng ký gói mặc định" });
    }
}

public class SubscribeDefaultRequest
{
    public int UserId { get; set; }
}

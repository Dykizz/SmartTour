using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using PayOS.Models.Webhooks;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
    {
        try
        {
            var paymentUrl = await _paymentService.CreatePaymentLinkAsync(request.UserId, request.PackageCode, request.Type ?? "New");
            return Ok(new { paymentUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayosWebhook([FromBody] Webhook webhook)
    {
        try
        {
            await _paymentService.ProcessWebhookAsync(webhook);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Webhook error", error = ex.Message });
        }
    }

    [HttpGet("verify-payment/{id}")]
    public async Task<IActionResult> VerifyPayment(int id)
    {
        var status = await _paymentService.GetPaymentStatusAsync(id);
        if (status == "NotFound") return NotFound();
        
        return Ok(new { status });
    }
}

public class PaymentRequest
{
    public int UserId { get; set; }
    public string PackageCode { get; set; }
    public string? Type { get; set; } 
}

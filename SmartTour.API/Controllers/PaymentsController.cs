using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PayOSClient _payOS;

    public PaymentsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        
        // Khởi tạo PayOS từ cấu hình
        _payOS = new PayOSClient(
            _configuration["PAYOS_CLIENT_ID"] ?? "",
            _configuration["PAYOS_API_KEY"] ?? "",
            _configuration["PAYOS_CHECKSUM_KEY"] ?? ""
        );
    }

    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == request.PackageCode && p.SoftDeleteAt == null);

        if (package == null) return BadRequest("Gói dịch vụ không tồn tại.");

        // 1. Tạo bản ghi Payment ở trạng thái Pending
        var payment = new Payment
        {
            UserId = request.UserId,
            PackageCode = request.PackageCode,
            Amount = package.Price,
            Status = "Pending",
            Type = request.Type ?? "New",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // 2. Tạo Link thanh toán PayOS
        try
        {
            string returnUrl = _configuration["PAYOS_RETURN_URL"] ?? "";
            string cancelUrl = _configuration["PAYOS_CANCEL_URL"] ?? "";

            // PayOS yêu cầu orderCode phải là số nguyên (long) và duy nhất
            long orderCode = payment.Id; 
            
            string description = $"TT {package.Name}";
            if (description.Length > 25) description = description.Substring(0, 25);

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)payment.Amount,
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = new List<PaymentLinkItem>
                {
                    new PaymentLinkItem
                    {
                        Name = package.Name,
                        Quantity = 1,
                        Price = (int)package.Price
                    }
                }
            };

            var result = await _payOS.PaymentRequests.CreateAsync(paymentRequest);
            
            // Lưu PaymentLinkId để đối soát sau này
            payment.ExternalTransactionNo = result.PaymentLinkId;
            await _context.SaveChangesAsync();

            return Ok(new { paymentUrl = result.CheckoutUrl, paymentId = payment.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi tạo link thanh toán PayOS", error = ex.Message });
        }
    }

    // Webhook: PayOS sẽ gọi vào đây
    [HttpPost("payos-webhook")]
    public async Task<IActionResult> PayosWebhook([FromBody] Webhook webhook)
    {
        try
        {
            // Xác thực dữ liệu từ PayOS
            var webhookData = await _payOS.Webhooks.VerifyAsync(webhook);

            var paymentId = (int)webhookData.OrderCode;
            var payment = await _context.Payments.FindAsync(paymentId);

            if (payment != null && payment.Status == "Pending")
            {
                // Trong thực tế, bạn nên kiểm tra số tiền và trạng thái giao dịch
                payment.Status = "Success";
                payment.ExternalTransactionNo = webhookData.Reference;
                
                await UpdateUserSubscription(payment);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Webhook error", error = ex.Message });
        }
    }

    private async Task UpdateUserSubscription(Payment payment)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == payment.PackageCode && p.SoftDeleteAt == null);
        
        if (package == null) return;

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == payment.UserId);
        
        if (subscription == null)
        {
            subscription = new Subscription
            {
                UserId = payment.UserId,
                PackageId = package.Id,
                LastPaymentId = payment.Id,
                PriceAtPurchase = payment.Amount,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays)
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.PackageId = package.Id;
            subscription.LastPaymentId = payment.Id;
            subscription.PriceAtPurchase = payment.Amount;
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddDays(package.DurationDays);
            _context.Subscriptions.Update(subscription);
        }
    }

    [HttpGet("verify-payment/{id}")]
    public async Task<IActionResult> VerifyPayment(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        
        if (payment.Status == "Pending" && !string.IsNullOrEmpty(payment.ExternalTransactionNo))
        {
            try
            {
                var paymentLink = await _payOS.PaymentRequests.GetAsync(payment.ExternalTransactionNo);
                if (paymentLink.Status == PaymentLinkStatus.Paid)
                {
                    payment.Status = "Success";
                    await UpdateUserSubscription(payment);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        return Ok(new { status = payment.Status });
    }
}

public class PaymentRequest
{
    public int UserId { get; set; }
    public string PackageCode { get; set; }
    public string? Type { get; set; } // 'New', 'Upgrade'
}

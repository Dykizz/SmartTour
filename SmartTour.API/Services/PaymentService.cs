using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using SmartTour.API.Interfaces;

namespace SmartTour.API.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PayOSClient _payOS;

    public PaymentService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        
        _payOS = new PayOSClient(
            _configuration["PAYOS_CLIENT_ID"] ?? "",
            _configuration["PAYOS_API_KEY"] ?? "",
            _configuration["PAYOS_CHECKSUM_KEY"] ?? ""
        );
    }

    public async Task<string> CreatePaymentLinkAsync(int userId, string packageCode, string type)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == packageCode && p.SoftDeleteAt == null);

        if (package == null) throw new Exception("Gói dịch vụ không tồn tại.");

        var payment = new Payment
        {
            UserId = userId,
            PackageCode = packageCode,
            Amount = package.Price,
            Status = "Pending",
            Type = type ?? "New",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        string returnUrl = _configuration["PAYOS_RETURN_URL"] ?? "";
        string cancelUrl = _configuration["PAYOS_CANCEL_URL"] ?? "";

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
        
        payment.ExternalTransactionNo = result.PaymentLinkId;
        await _context.SaveChangesAsync();

        return result.CheckoutUrl;
    }

    public async Task<bool> ProcessWebhookAsync(Webhook webhook)
    {
        var webhookData = await _payOS.Webhooks.VerifyAsync(webhook);

        var paymentId = (int)webhookData.OrderCode;
        var payment = await _context.Payments.FindAsync(paymentId);

        if (payment != null && payment.Status == "Pending")
        {
            payment.Status = "Success";
            payment.ExternalTransactionNo = webhookData.Reference;
            
            await UpdateUserSubscription(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<string> GetPaymentStatusAsync(int paymentId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return "NotFound";

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
                // Log error if needed
            }
        }

        return payment.Status;
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
}

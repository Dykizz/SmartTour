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

    public async Task<decimal> CalculateUpgradeAmountAsync(int userId, string newPackageCode)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.ServicePackage)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        var newPackage = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == newPackageCode && p.SoftDeleteAt == null);

        if (newPackage == null) return 0;
        if (subscription == null || subscription.ServicePackage == null || subscription.EndDate <= DateTime.UtcNow)
        {
            return newPackage.Price;
        }

        // Prorated Charge logic: 
        // Cost = (NewDailyRate - OldDailyRate) * RemainingDays
        double remainingDays = (subscription.EndDate - DateTime.UtcNow).TotalDays;
        if (remainingDays <= 0) return newPackage.Price;

        decimal oldDailyRate = subscription.PriceAtPurchase / subscription.ServicePackage.DurationDays;
        decimal newDailyRate = newPackage.Price / newPackage.DurationDays;

        if (newDailyRate <= oldDailyRate) return 0; // Or standard price if they really want to "downgrade" early? (Filtering should prevent this)

        decimal upgradeAmount = (decimal)remainingDays * (newDailyRate - oldDailyRate);
        
        // Ensure minimum amount (e.g., 2000 VND for PayOS or just round up)
        return Math.Max(2000, Math.Round(upgradeAmount));
    }

    public async Task<string> CreatePaymentLinkAsync(int userId, string packageCode, string type)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == packageCode && p.SoftDeleteAt == null);

        if (package == null) throw new Exception("Gói dịch vụ không tồn tại.");

        decimal amount = package.Price;
        if (type == "Upgrade")
        {
            amount = await CalculateUpgradeAmountAsync(userId, packageCode);
        }

        var payment = new Payment
        {
            UserId = userId,
            PackageCode = packageCode,
            Amount = amount,
            Status = "Pending",
            Type = type ?? "New",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        string returnUrl = _configuration["PAYOS_RETURN_URL"] ?? "";
        string cancelUrl = _configuration["PAYOS_CANCEL_URL"] ?? "";

        long orderCode = payment.Id; 
        
        string description = $"{(type == "Upgrade" ? "NC" : "TT")} {package.Name}";
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
                    Name = (type == "Upgrade" ? "Nâng cấp " : "") + package.Name,
                    Quantity = 1,
                    Price = (int)payment.Amount
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
                PriceAtPurchase = package.Price,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays)
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.PackageId = package.Id;
            subscription.LastPaymentId = payment.Id;
            
            // Lưu giá niêm yết của gói để các lần nâng cấp sau tính toán đúng DailyRate
            subscription.PriceAtPurchase = package.Price; 

            if (payment.Type != "Upgrade")
            {
                // Nếu là mua mới hoặc gia hạn, cộng thêm ngày
                subscription.EndDate = DateTime.UtcNow.AddDays(package.DurationDays);
            }
            // Nếu là Upgrade, EndDate được giữ nguyên như hiện tại (người dùng chỉ trả thêm tiền để có tính năng tốt hơn cho phần thời gian còn lại)
            
            _context.Subscriptions.Update(subscription);
        }

        // [SmartTour] Cập nhật Role của User thành SELLER (2) nếu họ đang là VISITOR (3)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == payment.UserId);
        if (user != null && user.RoleId == 3)
        {
            user.RoleId = 2; // Nâng cấp thành SELLER
            _context.Users.Update(user);
        }
    }
}

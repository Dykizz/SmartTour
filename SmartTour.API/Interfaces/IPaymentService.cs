using PayOS.Models.Webhooks;

namespace SmartTour.API.Interfaces;

public interface IPaymentService
{
    Task<string> CreatePaymentLinkAsync(int userId, string packageCode, string type);
    Task<bool> ProcessWebhookAsync(Webhook webhook);
    Task<string> GetPaymentStatusAsync(int paymentId);
}

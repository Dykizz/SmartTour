using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;

namespace SmartTour.API.Services;

public class PaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PaymentCleanupService> _logger;

    public PaymentCleanupService(IServiceProvider services, ILogger<PaymentCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Xóa các giao dịch Pending quá 24 giờ
                    var threshold = DateTime.UtcNow.AddHours(-24);
                    var oldPendingPayments = await context.Payments
                        .Where(p => p.Status == "Pending" && p.CreatedAt < threshold)
                        .ToListAsync(stoppingToken);

                    if (oldPendingPayments.Any())
                    {
                        _logger.LogInformation("Cleaning up {Count} expired pending payments.", oldPendingPayments.Count);
                        context.Payments.RemoveRange(oldPendingPayments);
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up payments.");
            }

            // Chạy 1 giờ một lần
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Payment Cleanup Service is stopping.");
    }
}

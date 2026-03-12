using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface ISubscriptionService
{
    Task<Subscription?> GetByUserIdAsync(int userId);
    Task<bool> SubscribeFreePackageAsync(int userId, string packageCode);
    Task<bool> SubscribeDefaultPackageAsync(int userId);
}

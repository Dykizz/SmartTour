using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;

    public SubscriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByUserIdAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.ServicePackage)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<bool> SubscribeFreePackageAsync(int userId, string packageCode)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.Code == packageCode && p.Price == 0 && p.SoftDeleteAt == null);

        if (package == null) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var existingSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (existingSubscription == null)
        {
            var subscription = new Subscription
            {
                UserId = userId,
                PackageId = package.Id,
                PriceAtPurchase = 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays)
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            existingSubscription.PackageId = package.Id;
            existingSubscription.PriceAtPurchase = 0;
            existingSubscription.EndDate = DateTime.UtcNow.AddDays(package.DurationDays);
            _context.Subscriptions.Update(existingSubscription);
        }

        // Cập nhật Role của User thành SELLER (2) nếu họ đang là VISITOR (3)
        if (user.RoleId == 3)
        {
            user.RoleId = 2;
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubscribeDefaultPackageAsync(int userId)
    {
        var package = await _context.ServicePackages
            .FirstOrDefaultAsync(p => p.IsDefault && p.SoftDeleteAt == null);

        if (package == null) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var existingSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        
        if (existingSubscription == null)
        {
            var subscription = new Subscription
            {
                UserId = userId,
                PackageId = package.Id,
                PriceAtPurchase = package.Price, // Even if it's default, record the price
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays)
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            existingSubscription.PackageId = package.Id;
            existingSubscription.PriceAtPurchase = package.Price;
            existingSubscription.EndDate = DateTime.UtcNow.AddDays(package.DurationDays);
            _context.Subscriptions.Update(existingSubscription);
        }

        // Cập nhật Role của User thành SELLER (2) nếu họ đang là VISITOR (3)
        if (user.RoleId == 3)
        {
            user.RoleId = 2;
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}

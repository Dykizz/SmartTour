using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;
using SmartTour.API.Interfaces;

namespace SmartTour.API.Services;

public class ServicePackageService : IServicePackageService
{
    private readonly AppDbContext _context;

    public ServicePackageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServicePackage>> GetAllActiveAsync()
    {
        return await _context.ServicePackages
            .Where(s => s.SoftDeleteAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServicePackage?> GetByIdAsync(int id)
    {
        return await _context.ServicePackages.FindAsync(id);
    }

    public async Task<ServicePackage> CreateAsync(ServicePackage package)
    {
        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Code == package.Code))
            throw new Exception("Mã gói (Code) đã tồn tại.");

        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Name == package.Name))
            throw new Exception("Tên gói đã tồn tại.");

        if (package.IsDefault)
        {
            await UnsetOtherDefaultsAsync();
        }

        package.CreatedAt = DateTime.UtcNow;
        _context.ServicePackages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    public async Task<ServicePackage?> UpdateAsync(int id, ServicePackage servicePackage)
    {
        var existingPackage = await _context.ServicePackages.FindAsync(id);
        if (existingPackage == null) return null;

        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Id != id && s.Code == servicePackage.Code))
            throw new Exception("Mã gói (Code) đã tồn tại.");

        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Id != id && s.Name == servicePackage.Name))
            throw new Exception("Tên gói đã tồn tại.");

        if (servicePackage.IsDefault)
        {
            await UnsetOtherDefaultsAsync();
        }

        existingPackage.SoftDeleteAt = DateTime.UtcNow;
        _context.ServicePackages.Update(existingPackage);

        var newPackage = new ServicePackage
        {
            Code = servicePackage.Code,
            Name = servicePackage.Name,
            Price = servicePackage.Price,
            DurationDays = servicePackage.DurationDays,
            Description = servicePackage.Description,
            MaxPoiAllowed = servicePackage.MaxPoiAllowed,
            IsActive = servicePackage.IsActive,
            IsDefault = servicePackage.IsDefault,
            CreatedAt = DateTime.UtcNow
        };
        _context.ServicePackages.Add(newPackage);

        await _context.SaveChangesAsync();
        return newPackage;
    }

    private async Task UnsetOtherDefaultsAsync()
    {
        var defaults = await _context.ServicePackages
            .Where(s => s.SoftDeleteAt == null && s.IsDefault)
            .ToListAsync();
        
        foreach (var d in defaults)
        {
            d.IsDefault = false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var package = await _context.ServicePackages.FindAsync(id);
        if (package == null) return false;

        package.SoftDeleteAt = DateTime.UtcNow;
        _context.ServicePackages.Update(package);
        await _context.SaveChangesAsync();
        return true;
    }
}

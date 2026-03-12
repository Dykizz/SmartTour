using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IServicePackageService
{
    Task<IEnumerable<ServicePackage>> GetAllActiveAsync();
    Task<ServicePackage?> GetByIdAsync(int id);
    Task<ServicePackage> CreateAsync(ServicePackage package);
    Task<ServicePackage?> UpdateAsync(int id, ServicePackage package);
    Task<bool> DeleteAsync(int id);
}

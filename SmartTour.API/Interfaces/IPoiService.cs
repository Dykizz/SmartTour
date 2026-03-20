using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IPoiService
{
    Task<IEnumerable<Poi>> GetPoisAsync(int? categoryId = null, double? lat = null, double? lng = null, double? radius = null, int? createdById = null, bool onlyActive = false, string? searchTerm = null, bool? onlyFeatured = null, bool? hasAudio = null, bool? onlyOpen = null);
    Task<PagedResponse<Poi>> GetPoisPagedAsync(int? categoryId = null, double? lat = null, double? lng = null, double? radius = null, int? createdById = null, bool onlyActive = false, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? onlyFeatured = null, bool? hasAudio = null, bool? onlyOpen = null);
    Task<IEnumerable<PoiGeofenceDto>> GetAllForGeofenceAsync();
    Task<Poi?> GetByIdAsync(int id);
    Task<Poi> CreateAsync(Poi poi, int userId);
    Task<bool> UpdateAsync(int id, Poi poi, int userId);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalCountAsync(int? createdById = null);
}

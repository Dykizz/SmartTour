using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IPoiRequestService
{
    Task<IEnumerable<PoiRequest>> GetAllAsync();
    Task<PagedResponse<PoiRequest>> GetAllPagedAsync(RequestStatus? status = null, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<PoiRequest>> GetByUserAsync(int userId);
    Task<PagedResponse<PoiRequest>> GetByUserPagedAsync(int userId, RequestStatus? status = null, int pageNumber = 1, int pageSize = 10);
    Task<PoiRequest?> GetByIdAsync(int id);
    Task<PoiRequest> SubmitRequestAsync(PoiRequest request, int userId);
    Task<bool> ApproveAsync(int requestId, int adminId, string? note);
    Task<bool> RejectAsync(int requestId, int adminId, string note);
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);
    Task<bool> UpdateRequestAsync(int id, string requestData, int userId);
    Task<Dictionary<string, int>> GetRequestCountsAsync(int? userId = null);
}

using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IPoiRequestService
{
    Task<IEnumerable<PoiRequest>> GetAllAsync();
    Task<IEnumerable<PoiRequest>> GetByUserAsync(int userId);
    Task<PoiRequest?> GetByIdAsync(int id);
    Task<PoiRequest> SubmitRequestAsync(PoiRequest request, int userId);
    Task<bool> ApproveAsync(int requestId, int adminId, string? note);
    Task<bool> RejectAsync(int requestId, int adminId, string note);
}

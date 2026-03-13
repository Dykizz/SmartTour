using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;
using System.Text.Json;

namespace SmartTour.API.Services;

public class PoiRequestService : IPoiRequestService
{
    private readonly AppDbContext _context;
    private readonly IPoiService _poiService;

    public PoiRequestService(AppDbContext context, IPoiService poiService)
    {
        _context = context;
        _poiService = poiService;
    }

    public async Task<IEnumerable<PoiRequest>> GetAllAsync()
    {
        return await _context.PoiRequests
            .Include(r => r.User)
            .Include(r => r.OriginalPoi)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PoiRequest>> GetByUserAsync(int userId)
    {
        return await _context.PoiRequests
            .Include(r => r.OriginalPoi)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<PoiRequest?> GetByIdAsync(int id)
    {
        return await _context.PoiRequests
            .Include(r => r.User)
            .Include(r => r.OriginalPoi)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<PoiRequest> SubmitRequestAsync(PoiRequest request, int userId)
    {
        request.UserId = userId;
        request.Status = RequestStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = null;

        _context.PoiRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<bool> ApproveAsync(int requestId, int adminId, string? note)
    {
        var request = await _context.PoiRequests.FindAsync(requestId);
        if (request == null) return false;

        request.Status = RequestStatus.Approved;
        request.AdminNote = note;
        request.UpdatedAt = DateTime.UtcNow;

        // Deserialize và thực sự tạo/cập nhật POI
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var poi = JsonSerializer.Deserialize<Poi>(request.RequestData, options);
        if (poi == null) return false;

        if (request.Type == RequestType.Create)
        {
            var created = await _poiService.CreateAsync(poi, request.UserId);
            // Lưu lại POI Id để Seller có thể xem/sửa POI của mình sau này
            request.POIId = created.Id;
        }
        else if (request.Type == RequestType.Update && request.POIId.HasValue)
        {
            poi.Id = request.POIId.Value;
            await _poiService.UpdateAsync(request.POIId.Value, poi, adminId);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int requestId, int adminId, string note)
    {
        var request = await _context.PoiRequests.FindAsync(requestId);
        if (request == null) return false;

        request.Status = RequestStatus.Rejected;
        request.AdminNote = note;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var request = await _context.PoiRequests.FindAsync(id);
        if (request == null) return false;

        // Nếu không phải admin, chỉ được xóa yêu cầu của chính mình
        if (!isAdmin && request.UserId != userId) return false;

        _context.PoiRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRequestAsync(int id, string requestData, int userId)
    {
        var request = await _context.PoiRequests.FindAsync(id);
        if (request == null || request.UserId != userId) return false;

        // Cho phép sửa nếu đang Pending hoặc Rejected
        if (request.Status == RequestStatus.Approved) return false;

        request.RequestData = requestData;
        request.Status = RequestStatus.Pending; // Gửi lại thì thành Pending
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}

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

    public async Task<PagedResponse<PoiRequest>> GetAllPagedAsync(RequestStatus? status = null, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.PoiRequests
            .Include(r => r.User)
            .Include(r => r.OriginalPoi)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<PoiRequest>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<PoiRequest>> GetByUserAsync(int userId)
    {
        return await _context.PoiRequests
            .Include(r => r.OriginalPoi)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResponse<PoiRequest>> GetByUserPagedAsync(int userId, RequestStatus? status = null, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.PoiRequests
            .Include(r => r.OriginalPoi)
            .Where(r => r.UserId == userId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<PoiRequest>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
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
            return true;
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

    public async Task<Dictionary<string, int>> GetRequestCountsAsync(int? userId = null)
    {
        var query = _context.PoiRequests.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);

        var groupedCounts = await query
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

        return new Dictionary<string, int>
        {
            { RequestStatus.Pending.ToString(), groupedCounts.GetValueOrDefault(RequestStatus.Pending.ToString(), 0) },
            { RequestStatus.Approved.ToString(), groupedCounts.GetValueOrDefault(RequestStatus.Approved.ToString(), 0) },
            { RequestStatus.Rejected.ToString(), groupedCounts.GetValueOrDefault(RequestStatus.Rejected.ToString(), 0) }
        };
    }
}

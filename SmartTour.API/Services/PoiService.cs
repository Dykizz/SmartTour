using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class PoiService : IPoiService
{
    private readonly AppDbContext _context;
    private readonly ICloudStorageService _storage;

    public PoiService(AppDbContext context, ICloudStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<IEnumerable<Poi>> GetPoisAsync(int? categoryId = null, double? lat = null, double? lng = null, double? radius = null, int? createdById = null, bool? isActive = null, string? search = null)
    {
        var query = _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .Include(p => p.AudioFiles)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (createdById.HasValue)
            query = query.Where(p => p.CreatedById == createdById.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) || (p.QrValue != null && p.QrValue.ToLower() == searchLower));
        }

        var pois = await query
            .OrderByDescending(p => p.IsFeature)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        if (lat.HasValue && lng.HasValue && radius.HasValue)
            pois = pois.Where(p => CalculateDistance(lat.Value, lng.Value, p.Latitude, p.Longitude) <= radius.Value).ToList();

        return pois;
    }

    public async Task<int> GetTotalCountAsync(int? createdById = null)
    {
        var query = _context.Pois.AsNoTracking();
        if (createdById.HasValue)
            query = query.Where(p => p.CreatedById == createdById.Value);

        return await query.CountAsync();
    }

    public async Task<PagedResponse<Poi>> GetPoisPagedAsync(int? categoryId = null, double? lat = null, double? lng = null, double? radius = null, int? createdById = null, bool? isActive = null, int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        var query = _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images) // Cần thiết cho Mobile Home
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (createdById.HasValue)
            query = query.Where(p => p.CreatedById == createdById.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) || (p.QrValue != null && p.QrValue.ToLower() == searchLower));
        }

        List<Poi> items;
        int totalCount;

        if (lat.HasValue && lng.HasValue && radius.HasValue)
        {
            // Lọc theo tọa độ (phải load về memory vì CalculateDistance là hàm C#)
            var allFiltered = await query
                .OrderByDescending(p => p.IsFeature)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            var geoFiltered = allFiltered
                .Where(p => CalculateDistance(lat.Value, lng.Value, p.Latitude, p.Longitude) <= radius.Value)
                .ToList();

            totalCount = geoFiltered.Count;
            items = geoFiltered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            // Phân trang chuẩn tại Database
            totalCount = await query.CountAsync();
            items = await query
                .OrderByDescending(p => p.IsFeature)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        return new PagedResponse<Poi>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Lấy danh sách nhẹ để dùng cho Geofence polling trên Mobile.
    /// Chỉ lấy các trường: Id, Name, Lat, Lng, GeofenceRadius, AudioFiles.
    /// </summary>
    public async Task<IEnumerable<PoiGeofenceDto>> GetAllForGeofenceAsync()
    {
        // QUAN TRỌNG: Phải ToListAsync() trước rồi mới project sang DTO,
        // vì EF Core không thể dịch p.AudioFiles.ToList() sang SQL.
        var pois = await _context.Pois
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.AudioFiles)
            .ToListAsync();

        return pois.Select(p => new PoiGeofenceDto
        {
            Id = p.Id,
            Name = p.Name,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            GeofenceRadius = p.GeofenceRadius,
            CategoryName = p.Category?.Name ?? "Place",
            AudioFiles = p.AudioFiles.ToList()
        });
    }

    public async Task<Poi?> GetByIdAsync(int id)
    {
        return await _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .Include(p => p.AudioFiles)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Poi> CreateAsync(Poi poi, int userId)
    {
        var nameLower = poi.Name.ToLower();
        var isDuplicate = await _context.Pois.AnyAsync(p => 
            p.Name.ToLower() == nameLower || 
            (p.Latitude == poi.Latitude && p.Longitude == poi.Longitude));

        if (isDuplicate)
        {
            throw new InvalidOperationException("Điểm đến này đã tồn tại (bị trùng tên hoặc trùng toạ độ).");
        }

        poi.CreatedAt = DateTime.UtcNow;
        poi.CreatedById = userId;
        poi.UpdatedById = userId;
        // Clear non-owned parent objects to prevent EF from inserting duplicate entries
        poi.Category = null;
        poi.CreatedBy = null;
        poi.UpdatedBy = null;

        // Tự sinh QrValue nếu trống
        if (string.IsNullOrWhiteSpace(poi.QrValue))
        {
            string newQr;
            bool qrExists;
            do
            {
                // Sinh tiền tố POI- kèm 8 ký tự ngẫu nhiên
                var randomStr = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                newQr = $"POI-{randomStr}";
                
                qrExists = await _context.Pois.AnyAsync(p => p.QrValue == newQr);
            } while (qrExists);
            
            poi.QrValue = newQr;
        }
        
        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();
        return poi;
    }

    public async Task<bool> UpdateAsync(int id, Poi poi, int userId)
    {
        if (id != poi.Id) return false;

        var nameLower = poi.Name.ToLower();
        var isDuplicate = await _context.Pois.AnyAsync(p => 
            p.Id != id && 
            (p.Name.ToLower() == nameLower || 
            (p.Latitude == poi.Latitude && p.Longitude == poi.Longitude)));

        if (isDuplicate)
        {
            throw new InvalidOperationException("Điểm đến này đã tồn tại (bị trùng tên hoặc trùng toạ độ).");
        }

        poi.Category = null;
        poi.CreatedBy = null;
        poi.UpdatedBy = null;

        var existingPoi = await _context.Pois.FindAsync(id);
        if (existingPoi == null) return false;

        var createdAt = existingPoi.CreatedAt;
        var createdById = existingPoi.CreatedById;

        _context.Entry(existingPoi).CurrentValues.SetValues(poi);

        existingPoi.CreatedAt = createdAt;
        existingPoi.CreatedById = createdById;
        
        existingPoi.UpdatedAt = DateTime.UtcNow;
        existingPoi.UpdatedById = userId;

        // Đồng bộ dữ liệu các bảng liên kết (dựa trên dữ liệu truyền vào)
        await SyncRelatedData(id, poi);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Pois.AnyAsync(e => e.Id == id)) return false;
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Load đầy đủ ảnh và audio để xóa file trên GCS
        var poi = await _context.Pois
            .Include(p => p.Images)
            .Include(p => p.AudioFiles)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null) return false;

        // 1. Xóa file thực trên Google Cloud Storage
        var deleteFileTasks = new List<Task>();
        foreach (var img in poi.Images)
            if (!string.IsNullOrEmpty(img.ImageUrl))
                deleteFileTasks.Add(_storage.DeleteFileAsync(img.ImageUrl));
        foreach (var audio in poi.AudioFiles)
            if (!string.IsNullOrEmpty(audio.FileUrl))
                deleteFileTasks.Add(_storage.DeleteFileAsync(audio.FileUrl));
        await Task.WhenAll(deleteFileTasks);

        // 2. Xóa các bảng con trong DB (EF cascade sẽ xử lý nếu đã cấu hình, nhưng xóa tường minh cho chắc)
        _context.PoiImages.RemoveRange(poi.Images);
        _context.PoiAudioFiles.RemoveRange(poi.AudioFiles);
        _context.PoiContents.RemoveRange(poi.Contents);
        _context.OperatingHours.RemoveRange(poi.OperatingHours);

        // 3. Xóa POI
        _context.Pois.Remove(poi);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task SyncRelatedData(int id, Poi poi)
    {
        // Operating Hours
        var existingHours = await _context.OperatingHours.Where(h => h.PoiId == id).ToListAsync();
        _context.OperatingHours.RemoveRange(existingHours);
        if (poi.OperatingHours != null)
        {
            foreach (var h in poi.OperatingHours) { h.Id = 0; h.PoiId = id; _context.OperatingHours.Add(h); }
        }

        // Images
        var existingImages = await _context.PoiImages.Where(i => i.PoiId == id).ToListAsync();
        _context.PoiImages.RemoveRange(existingImages);
        if (poi.Images != null)
        {
            foreach (var img in poi.Images) { img.Id = 0; img.PoiId = id; _context.PoiImages.Add(img); }
        }

        // Contents
        var existingContents = await _context.PoiContents.Where(c => c.PoiId == id).ToListAsync();
        _context.PoiContents.RemoveRange(existingContents);
        if (poi.Contents != null)
        {
            foreach (var content in poi.Contents) { content.Id = 0; content.PoiId = id; _context.PoiContents.Add(content); }
        }

        // Audio Files
        var existingAudios = await _context.PoiAudioFiles.Where(a => a.PoiId == id).ToListAsync();
        _context.PoiAudioFiles.RemoveRange(existingAudios);
        if (poi.AudioFiles != null)
        {
            foreach (var audio in poi.AudioFiles) { audio.Id = 0; audio.PoiId = id; _context.PoiAudioFiles.Add(audio); }
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3;
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}

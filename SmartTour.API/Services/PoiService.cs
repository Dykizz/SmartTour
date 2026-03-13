using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class PoiService : IPoiService
{
    private readonly AppDbContext _context;

    public PoiService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Poi>> GetPoisAsync(int? categoryId = null, double? lat = null, double? lng = null, double? radius = null, int? createdById = null, bool onlyActive = false)
    {
        var query = _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .Include(p => p.AudioFiles)
            .AsQueryable();

        if (onlyActive)
            query = query.Where(p => p.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (createdById.HasValue)
            query = query.Where(p => p.CreatedById == createdById.Value);

        var pois = await query
            .OrderByDescending(p => p.IsFeature)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        if (lat.HasValue && lng.HasValue && radius.HasValue)
            pois = pois.Where(p => CalculateDistance(lat.Value, lng.Value, p.Latitude, p.Longitude) <= radius.Value).ToList();

        return pois;
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
        poi.CreatedAt = DateTime.UtcNow;
        poi.CreatedById = userId;
        poi.UpdatedById = userId;
        
        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();
        return poi;
    }

    public async Task<bool> UpdateAsync(int id, Poi poi, int userId)
    {
        if (id != poi.Id) return false;

        var existingPoi = await _context.Pois.FindAsync(id);
        if (existingPoi == null) return false;

        // Update properties
        _context.Entry(poi).State = EntityState.Modified;
        _context.Entry(poi).Property(x => x.CreatedAt).IsModified = false;
        _context.Entry(poi).Property(x => x.CreatedById).IsModified = false;
        
        poi.UpdatedAt = DateTime.UtcNow;
        poi.UpdatedById = userId;

        // Sync related data
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
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null) return false;

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoisController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PoisController> _logger;

    public PoisController(AppDbContext context, ILogger<PoisController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ... (keep other methods as is, don't change them unless necessary, but I can't skip lines in replace_file_content like this easily if they are far apart. I'll target the Constructor and DeletePoi separately)

    // ...
    // DELETE: api/Pois/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePoi(int id)
    {
        _logger.LogInformation($"[DELETE POI] Request to delete POI ID: {id}. User: {User.Identity?.Name ?? "Anonymous"}");

        var poi = await _context.Pois.FindAsync(id);
        if (poi == null)
        {
            _logger.LogWarning($"[DELETE POI] POI {id} not found.");
            return NotFound();
        }

        try
        {
            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"[DELETE POI] Successfully deleted POI {id}.");
            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, $"[DELETE POI] Database update error for POI {id}.");
            return StatusCode(500, $"Lỗi cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[DELETE POI] General error deleting POI {id}.");
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated) return null;

        // Thử lấy ID từ claim NameIdentifier (phổ biến nhất)
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int id)) return id;

        // Nếu không có ID, thử lấy qua Email (thường dùng cho Google/Social login)
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                        ?? User.FindFirst("email")?.Value;
        
        if (!string.IsNullOrEmpty(emailClaim))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
            return user?.Id;
        }

        return null;
    }

    [HttpGet]
    [AllowAnonymous] // Cho phép khách xem danh sách POI
    public async Task<ActionResult<IEnumerable<Poi>>> GetPois()
    {
        return await _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .Include(p => p.AudioFiles)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Cho phép khách xem chi tiết POI
    public async Task<ActionResult<Poi>> GetPoi(int id)
    {
        var poi = await _context.Pois
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Contents)
            .Include(p => p.OperatingHours)
            .Include(p => p.AudioFiles)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        return poi;
    }

        // POST: api/Pois
    [HttpPost]
    [HttpPost]
    public async Task<ActionResult<Poi>> PostPoi(Poi poi)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Unauthorized("Vui lòng đăng nhập để thực hiện thao tác này");

        poi.CreatedAt = DateTime.UtcNow;
        poi.CreatedById = userId.Value;
        poi.UpdatedById = userId.Value; // Lần đầu tạo thì người tạo cũng là người cập nhật
        
        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPoi), new { id = poi.Id }, poi);
    }

    // PUT: api/Pois/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPoi(int id, Poi poi)
    {
        if (id != poi.Id)
        {
            return BadRequest();
        }

        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Unauthorized("Vui lòng đăng nhập để thực hiện thao tác này");

        // Update basic properties
        _context.Entry(poi).State = EntityState.Modified;
        _context.Entry(poi).Property(x => x.CreatedAt).IsModified = false;
        _context.Entry(poi).Property(x => x.CreatedById).IsModified = false;
        
        poi.UpdatedAt = DateTime.UtcNow;
        poi.UpdatedById = userId.Value;

        // Handle related data (OperatingHours and Images)
        // For simplicity in this example, we'll clear and re-add or you could implement a more complex sync
        
        // Update Operating Hours
        var existingHours = await _context.OperatingHours.Where(h => h.PoiId == id).ToListAsync();
        _context.OperatingHours.RemoveRange(existingHours);
        if (poi.OperatingHours != null)
        {
            foreach (var h in poi.OperatingHours)
            {
                h.Id = 0; // Ensure new IDs
                h.PoiId = id;
                _context.OperatingHours.Add(h);
            }
        }

        // Update Images
        var existingImages = await _context.PoiImages.Where(i => i.PoiId == id).ToListAsync();
        _context.PoiImages.RemoveRange(existingImages);
        if (poi.Images != null)
        {
            foreach (var img in poi.Images)
            {
                img.Id = 0;
                img.PoiId = id;
                _context.PoiImages.Add(img);
            }
        }

        // Update Contents (Name & Description per language)
        var existingContents = await _context.PoiContents.Where(c => c.PoiId == id).ToListAsync();
        _context.PoiContents.RemoveRange(existingContents);
        if (poi.Contents != null)
        {
            foreach (var content in poi.Contents)
            {
                content.Id = 0;
                content.PoiId = id;
                _context.PoiContents.Add(content);
            }
        }

        // Update Audio Files
        var existingAudios = await _context.PoiAudioFiles.Where(a => a.PoiId == id).ToListAsync();
        _context.PoiAudioFiles.RemoveRange(existingAudios);
        if (poi.AudioFiles != null)
        {
            foreach (var audio in poi.AudioFiles)
            {
                audio.Id = 0;
                audio.PoiId = id;
                _context.PoiAudioFiles.Add(audio);
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PoiExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }



    private bool PoiExists(int id)
    {
        return _context.Pois.Any(e => e.Id == id);
    }
}

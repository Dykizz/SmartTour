using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out int idFromClaim) && idFromClaim > 0)
            return idFromClaim;

        if (Request.Headers.TryGetValue("X-User-Id", out var headerVal)
            && int.TryParse(headerVal, out int idFromHeader)
            && idFromHeader > 0)
            return idFromHeader;

        return null;
    }

    // GET api/favorites — lấy danh sách yêu thích kèm đủ thông tin hiển thị
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Poi>>> GetFavorites()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var pois = await _context.Favorites
            .Where(f => f.UserId == userId.Value)
            .OrderByDescending(f => f.CreatedAt)
            .Include(f => f.Poi).ThenInclude(p => p!.Category)
            .Include(f => f.Poi).ThenInclude(p => p!.Images)
            .Select(f => f.Poi!)
            .ToListAsync();

        return Ok(pois);
    }

    // GET api/favorites/check/{poiId} — kiểm tra 1 POI có trong yêu thích không
    [HttpGet("check/{poiId}")]
    public async Task<ActionResult<bool>> CheckFavorite(int poiId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var exists = await _context.Favorites
            .AnyAsync(f => f.UserId == userId.Value && f.PoiId == poiId);

        return Ok(exists);
    }

    // POST api/favorites/toggle/{poiId} — thêm hoặc xóa khỏi yêu thích
    [HttpPost("toggle/{poiId}")]
    public async Task<IActionResult> ToggleFavorite(int poiId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.PoiId == poiId);

        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return Ok(new { isFavorite = false });
        }
        else
        {
            _context.Favorites.Add(new Favorite
            {
                UserId = userId.Value,
                PoiId = poiId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return Ok(new { isFavorite = true });
        }
    }

    // DELETE api/favorites/{poiId} — xóa trực tiếp (dùng từ trang Profile)
    [HttpDelete("{poiId}")]
    public async Task<IActionResult> RemoveFavorite(int poiId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.PoiId == poiId);

        if (favorite == null) return NotFound();

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

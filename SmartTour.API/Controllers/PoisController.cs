using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;
using Microsoft.AspNetCore.Authorization;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoisController : ControllerBase
{
    private readonly IPoiService _poiService;
    private readonly ILogger<PoisController> _logger;

    public PoisController(IPoiService poiService, ILogger<PoisController> logger)
    {
        _poiService = poiService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<Poi>>> GetPois(
        [FromQuery] int? categoryId = null,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        [FromQuery] double? radius = null,
        [FromQuery] int? createdById = null,
        [FromQuery] bool onlyActive = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _poiService.GetPoisPagedAsync(categoryId, lat, lng, radius, createdById, onlyActive, pageNumber, pageSize));
    }

    /// <summary>
    /// Trả về danh sách nhẹ (chỉ lat/lng/radius/audio) dùng cho Geofence polling trên Mobile.
    /// </summary>
    [HttpGet("geofence")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SmartTour.Shared.Models.PoiGeofenceDto>>> GetPoisForGeofence()
    {
        var pois = await _poiService.GetAllForGeofenceAsync();
        return Ok(pois);
    }

    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<ActionResult<int>> GetCount([FromQuery] int? createdById = null)
    {
        return Ok(await _poiService.GetTotalCountAsync(createdById));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Poi>> GetPoi(int id)
    {
        var poi = await _poiService.GetByIdAsync(id);
        if (poi == null) return NotFound();
        return poi;
    }

    [HttpPost]
    public async Task<ActionResult<Poi>> PostPoi(Poi poi)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Unauthorized("Vui lòng đăng nhập");

        var created = await _poiService.CreateAsync(poi, userId.Value);
        return CreatedAtAction(nameof(GetPoi), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPoi(int id, Poi poi)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null) return Unauthorized("Vui lòng đăng nhập");

        var result = await _poiService.UpdateAsync(id, poi, userId.Value);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePoi(int id)
    {
        var result = await _poiService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("map-config")]
    [AllowAnonymous]
    public IActionResult GetMapConfig([FromServices] IConfiguration config)
    {
        var token = config["MAPBOX_ACCESS_TOKEN"];
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("MAPBOX_ACCESS_TOKEN chưa được cấu hình");
            return Ok(new MapConfig { MapboxAccessToken = "" });
        }

        return Ok(new MapConfig { MapboxAccessToken = token });
    }

    public class MapConfig 
    { 
        [System.Text.Json.Serialization.JsonPropertyName("mapboxAccessToken")]
        public string MapboxAccessToken { get; set; } = string.Empty; 
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated) return null;

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int id)) return id;

        // Note: For real-world apps, you might need to find user by email from DB here
        return null;
    }
}

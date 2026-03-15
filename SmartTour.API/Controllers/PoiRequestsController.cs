using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/poi-requests")]
[Authorize]
public class PoiRequestsController : ControllerBase
{
    private readonly IPoiRequestService _requestService;
    private readonly IPoiService _poiService;

    public PoiRequestsController(IPoiRequestService requestService, IPoiService poiService)
    {
        _requestService = requestService;
        _poiService = poiService;
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var isAdmin = User.IsInRole("ADMIN");
        var counts = await _requestService.GetRequestCountsAsync(isAdmin ? null : userId.Value);
        
        return Ok(counts);
    }

    // GET api/poi-requests - Admin: tất cả; Seller: chỉ của mình (có phân trang)
    [HttpGet]
    public async Task<IActionResult> GetRequests([FromQuery] RequestStatus? status = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (User.IsInRole("ADMIN"))
        {
            var paged = await _requestService.GetAllPagedAsync(status, pageNumber, pageSize);
            return Ok(paged);
        }
        else
        {
            var paged = await _requestService.GetByUserPagedAsync(userId.Value, status, pageNumber, pageSize);
            return Ok(paged);
        }
    }

    // GET api/poi-requests/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _requestService.GetByIdAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    // POST api/poi-requests - Seller tạo yêu cầu
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] PoiRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var created = await _requestService.SubmitRequestAsync(request, userId.Value);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // POST api/poi-requests/{id}/approve - Admin duyệt
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Approve(int id, [FromBody] AdminActionDto dto)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null) return Unauthorized();

        var success = await _requestService.ApproveAsync(id, adminId.Value, dto.Note);
        if (!success) return NotFound();

        return NoContent();
    }

    // POST api/poi-requests/{id}/reject - Admin từ chối
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Reject(int id, [FromBody] AdminActionDto dto)
    {
        var adminId = GetCurrentUserId();
        if (adminId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Note))
            return BadRequest("Vui lòng nhập lý do từ chối.");

        var success = await _requestService.RejectAsync(id, adminId.Value, dto.Note!);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var success = await _requestService.DeleteAsync(id, userId.Value, User.IsInRole("ADMIN"));
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PoiRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var success = await _requestService.UpdateRequestAsync(id, request.RequestData, userId.Value);
        if (!success) return NotFound("Yêu cầu không tìm thấy hoặc đã được duyệt.");

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : null;
    }
}

public class AdminActionDto
{
    public string? Note { get; set; }
}

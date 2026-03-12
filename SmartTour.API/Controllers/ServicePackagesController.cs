using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicePackagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicePackagesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/ServicePackages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicePackage>>> GetServicePackages()
    {
        return await _context.ServicePackages
            .Where(s => s.SoftDeleteAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    // GET: api/ServicePackages/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ServicePackage>> GetServicePackage(int id)
    {
        var package = await _context.ServicePackages.FindAsync(id);

        if (package == null)
        {
            return NotFound();
        }

        return package;
    }

    // PUT: api/ServicePackages/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutServicePackage(int id, ServicePackage servicePackage)
    {
        var existingPackage = await _context.ServicePackages.FindAsync(id);
        if (existingPackage == null)
        {
            return NotFound();
        }

        // Kiểm tra trùng mã (Code) với các gói khác đang hoạt động
        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Id != id && s.Code == servicePackage.Code))
        {
            return BadRequest("Mã gói (Code) đã tồn tại.");
        }

        // Kiểm tra trùng tên (Name) với các gói khác đang hoạt động
        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Id != id && s.Name == servicePackage.Name))
        {
            return BadRequest("Tên gói đã tồn tại.");
        }

        // Xóa mềm bản cũ
        existingPackage.SoftDeleteAt = DateTime.UtcNow;
        _context.ServicePackages.Update(existingPackage);

        // Tạo bản mới dựa trên thông tin cập nhật
        var newPackage = new ServicePackage
        {
            Code = servicePackage.Code,
            Name = servicePackage.Name,
            Price = servicePackage.Price,
            DurationDays = servicePackage.DurationDays,
            Description = servicePackage.Description,
            MaxPoiAllowed = servicePackage.MaxPoiAllowed,
            IsActive = servicePackage.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.ServicePackages.Add(newPackage);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServicePackage), new { id = newPackage.Id }, newPackage);
    }

    // POST: api/ServicePackages
    [HttpPost]
    public async Task<ActionResult<ServicePackage>> PostServicePackage(ServicePackage servicePackage)
    {
        // Kiểm tra trùng mã (Code) trong các gói đang hoạt động
        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Code == servicePackage.Code))
        {
            return BadRequest("Mã gói (Code) đã tồn tại.");
        }

        // Kiểm tra trùng tên (Name) trong các gói đang hoạt động
        if (await _context.ServicePackages.AnyAsync(s => s.SoftDeleteAt == null && s.Name == servicePackage.Name))
        {
            return BadRequest("Tên gói đã tồn tại.");
        }

        servicePackage.CreatedAt = DateTime.UtcNow;
        _context.ServicePackages.Add(servicePackage);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServicePackage), new { id = servicePackage.Id }, servicePackage);
    }

    // DELETE: api/ServicePackages/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicePackage(int id)
    {
        var package = await _context.ServicePackages.FindAsync(id);
        if (package == null)
        {
            return NotFound();
        }

        // Thay đổi từ xóa cứng sang xóa mềm
        package.SoftDeleteAt = DateTime.UtcNow;
        _context.ServicePackages.Update(package);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServicePackageExists(int id)
    {
        return _context.ServicePackages.Any(e => e.Id == id);
    }
}

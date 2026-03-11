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
        return await _context.ServicePackages.OrderByDescending(s => s.CreatedAt).ToListAsync();
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
        if (id != servicePackage.Id)
        {
            return BadRequest();
        }

        _context.Entry(servicePackage).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServicePackageExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/ServicePackages
    [HttpPost]
    public async Task<ActionResult<ServicePackage>> PostServicePackage(ServicePackage servicePackage)
    {
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

        _context.ServicePackages.Remove(package);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServicePackageExists(int id)
    {
        return _context.ServicePackages.Any(e => e.Id == id);
    }
}

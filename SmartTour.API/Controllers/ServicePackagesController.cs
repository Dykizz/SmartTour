using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicePackagesController : ControllerBase
{
    private readonly IServicePackageService _packageService;

    public ServicePackagesController(IServicePackageService packageService)
    {
        _packageService = packageService;
    }

    // GET: api/ServicePackages
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicePackage>>> GetServicePackages()
    {
        return Ok(await _packageService.GetAllActiveAsync());
    }

    // GET: api/ServicePackages/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ServicePackage>> GetServicePackage(int id)
    {
        var package = await _packageService.GetByIdAsync(id);
        if (package == null) return NotFound();
        return package;
    }

    // PUT: api/ServicePackages/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutServicePackage(int id, ServicePackage servicePackage)
    {
        try
        {
            var updated = await _packageService.UpdateAsync(id, servicePackage);
            if (updated == null) return NotFound();
            return CreatedAtAction(nameof(GetServicePackage), new { id = updated.Id }, updated);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: api/ServicePackages
    [HttpPost]
    public async Task<ActionResult<ServicePackage>> PostServicePackage(ServicePackage servicePackage)
    {
        try
        {
            var created = await _packageService.CreateAsync(servicePackage);
            return CreatedAtAction(nameof(GetServicePackage), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE: api/ServicePackages/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicePackage(int id)
    {
        var result = await _packageService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Services;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private readonly ICloudStorageService _storageService;

    public UploadsController(ICloudStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var fileName = $"images/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = file.OpenReadStream();
            var fileUrl = await _storageService.UploadFileAsync(stream, fileName, file.ContentType);
            
            return Ok(new { url = fileUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Upload Error: {ex.Message}");
        }
    }
}

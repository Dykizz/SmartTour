using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly ITranslateService _translateService;

    public TranslateController(ITranslateService translateService)
    {
        _translateService = translateService;
    }

    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest("Text is required");

        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
            return BadRequest("Target language is required");

        try
        {
            var result = await _translateService.TranslateTextAsync(request.Text, request.TargetLanguage);
            
            return Ok(new 
            { 
                translatedText = result.TranslatedText,
                sourceLanguage = result.DetectedSourceLanguage,
                targetLanguage = request.TargetLanguage
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Translation Error: {ex.Message}");
        }
    }

    public class TranslateRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string? SourceLanguage { get; set; }
    }
}

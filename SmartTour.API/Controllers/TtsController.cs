using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private readonly ITtsService _ttsService;

    public TtsController(ITtsService ttsService)
    {
        _ttsService = ttsService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest("Text is required");

        try
        {
            var publicUrl = await _ttsService.GenerateSpeechAsync(request.Text, request.LanguageCode);
            return Ok(new { url = publicUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Google TTS/Storage Error: {ex.Message}");
        }
    }

    public class TtsRequest 
    { 
        public string Text { get; set; } = ""; 
        public string LanguageCode { get; set; } = "";
    }
}

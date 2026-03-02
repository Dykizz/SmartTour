using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Translation.V2;
using Google.Apis.Auth.OAuth2;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public TranslateController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest("Text is required");

        if (string.IsNullOrWhiteSpace(request.TargetLanguage))
            return BadRequest("Target language is required");

        var credentialPath = Path.Combine(_env.ContentRootPath, "google-credentials.json");
        
        if (!System.IO.File.Exists(credentialPath))
        {
            return StatusCode(500, "Google Credentials not found");
        }

        try
        {
            // Load credentials and create translation client
            using var stream = new System.IO.FileStream(credentialPath, FileMode.Open, FileAccess.Read);
            var googleCredential = GoogleCredential.FromStream(stream);
            var client = TranslationClient.Create(googleCredential);

            var response = await client.TranslateTextAsync(
                text: request.Text,
                targetLanguage: request.TargetLanguage
            );

            Console.WriteLine($"Translated text: {response.TranslatedText}");
            Console.WriteLine($"Source language: {response.DetectedSourceLanguage}");
            Console.WriteLine($"Target language: {response.TargetLanguage}");

            return Ok(new 
            { 
                translatedText = response.TranslatedText,
                sourceLanguage = response.DetectedSourceLanguage,
                targetLanguage = response.TargetLanguage
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

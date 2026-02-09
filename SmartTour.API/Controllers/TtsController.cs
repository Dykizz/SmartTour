using Microsoft.AspNetCore.Mvc;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using Google.Apis.Auth.OAuth2;
using SmartTour.API.Services;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ICloudStorageService _storageService;

    public TtsController(IWebHostEnvironment env, ICloudStorageService storageService)
    {
        _env = env;
        _storageService = storageService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest("Text is required");

        var credentialPath = Path.Combine(_env.ContentRootPath, "google-credentials.json");
        
        if (!System.IO.File.Exists(credentialPath))
        {
            return StatusCode(500, $"Credential file NOT found at: {credentialPath}");
        }

        try
        {
            using var stream = new System.IO.FileStream(credentialPath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var credential = GoogleCredential.FromJson(json);
            var clientBuilder = new TextToSpeechClientBuilder { GoogleCredential = credential };
            var client = await clientBuilder.BuildAsync();

            var input = new SynthesisInput { Text = request.Text };
            
            var (langTag, voiceName) = GetGoogleVoiceConfig(request.LanguageCode);

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = langTag,
                Name = voiceName
            };

            var audioConfig = new AudioConfig { AudioEncoding = AudioEncoding.Mp3 };

            var response = await client.SynthesizeSpeechAsync(input, voiceSelection, audioConfig);

            var audioBytes = response.AudioContent.ToByteArray();
            
            // Upload to Google Cloud Storage instead of local
            var fileName = $"audio/tts_{Guid.NewGuid():N}.mp3";
            var publicUrl = await _storageService.UploadBytesAsync(audioBytes, fileName, "audio/mpeg");
            Console.WriteLine($"[TTS] Audio uploaded to GCS: {publicUrl}");
            
            return Ok(new { url = publicUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Google TTS/Storage Error: {ex.Message}");
        }
    }

    private (string langTag, string voiceName) GetGoogleVoiceConfig(string? langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode)) langCode = "vi";
        var code = langCode.Split('-', '_')[0].ToLower().Trim();

        return code switch
        {
            "vi" => ("vi-VN", "vi-VN-Wavenet-A"),
            "en" => ("en-US", "en-US-Wavenet-D"),
            "zh" => ("cmn-CN", "cmn-CN-Wavenet-A"),
            "ja" => ("ja-JP", "ja-JP-Wavenet-A"),
            "ko" => ("ko-KR", "ko-KR-Wavenet-A"),
            "th" => ("th-TH", "th-TH-Standard-A"),
            "lo" => ("lo-LA", "lo-LA-Standard-A"),
            "km" => ("km-KH", "km-KH-Standard-A"),
            "ms" => ("ms-MY", "ms-MY-Wavenet-A"),
            "id" => ("id-ID", "id-ID-Wavenet-A"),
            "hi" => ("hi-IN", "hi-IN-Wavenet-A"),
            "bn" => ("bn-IN", "bn-IN-Wavenet-A"),
            "tl" or "fil" => ("fil-PH", "fil-PH-Wavenet-A"),
            "fr" => ("fr-FR", "fr-FR-Wavenet-A"),
            "de" => ("de-DE", "de-DE-Wavenet-A"),
            "ru" => ("ru-RU", "ru-RU-Wavenet-A"),
            "es" => ("es-ES", "es-ES-Wavenet-B"),
            "it" => ("it-IT", "it-IT-Wavenet-A"),
            "pt" => ("pt-PT", "pt-PT-Wavenet-A"),
            "nl" => ("nl-NL", "nl-NL-Wavenet-A"),
            "sv" => ("sv-SE", "sv-SE-Wavenet-A"),
            "no" or "nb" => ("nb-NO", "nb-NO-Wavenet-A"),
            "da" => ("da-DK", "da-DK-Wavenet-A"),
            "fi" => ("fi-FI", "fi-FI-Wavenet-A"),
            "pl" => ("pl-PL", "pl-PL-Wavenet-A"),
            "uk" => ("uk-UA", "uk-UA-Wavenet-A"),
            "cs" => ("cs-CZ", "cs-CZ-Wavenet-A"),
            "tr" => ("tr-TR", "tr-TR-Wavenet-A"),
            "el" => ("el-GR", "el-GR-Wavenet-A"),
            "ar" => ("ar-XA", "ar-XA-Wavenet-A"),
            "he" => ("he-IL", "he-IL-Wavenet-A"),
            "fa" => ("fa-IR", "fa-IR-Standard-A"),
            "sw" => ("sw-KE", "sw-KE-Standard-A"),
            "af" => ("af-ZA", "af-ZA-Standard-A"),
            _ => ("vi-VN", "vi-VN-Wavenet-A")
        };
    }

    public class TtsRequest 
    { 
        public string Text { get; set; } = ""; 
        public string LanguageCode { get; set; } = "";
    }
}

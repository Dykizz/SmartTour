using Google.Cloud.Translation.V2;
using Google.Apis.Auth.OAuth2;
using SmartTour.API.Interfaces;

namespace SmartTour.API.Services;

public class TranslateService : ITranslateService
{
    private readonly string _credentialPath;

    public TranslateService(IWebHostEnvironment env)
    {
        _credentialPath = Path.Combine(env.ContentRootPath, "google-credentials.json");
    }

    public async Task<(string TranslatedText, string DetectedSourceLanguage)> TranslateTextAsync(string text, string targetLanguage)
    {
        if (!File.Exists(_credentialPath)) throw new Exception("Google Credentials not found");

        using var stream = new FileStream(_credentialPath, FileMode.Open, FileAccess.Read);
        var googleCredential = GoogleCredential.FromStream(stream);
        var client = TranslationClient.Create(googleCredential);

        var response = await client.TranslateTextAsync(text: text, targetLanguage: targetLanguage);
        return (response.TranslatedText, response.DetectedSourceLanguage);
    }
}

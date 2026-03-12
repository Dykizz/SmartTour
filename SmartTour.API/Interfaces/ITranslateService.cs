namespace SmartTour.API.Interfaces;

public interface ITranslateService
{
    Task<(string TranslatedText, string DetectedSourceLanguage)> TranslateTextAsync(string text, string targetLanguage);
}

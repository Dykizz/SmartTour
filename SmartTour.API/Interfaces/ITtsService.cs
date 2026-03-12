namespace SmartTour.API.Interfaces;

public interface ITtsService
{
    Task<string> GenerateSpeechAsync(string text, string languageCode);
}

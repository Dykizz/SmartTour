using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface ILanguageService
{
    Task<IEnumerable<Language>> GetLanguagesAsync();
    Task<Language?> GetByIdAsync(int id);
    Task<Language> CreateOrUpdateLanguageAsync(Language language);
    Task<bool> ToggleActiveAsync(int id, bool isActive);
    Task<bool> SetDefaultAsync(int id);
}

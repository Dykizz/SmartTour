using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class LanguageService : ILanguageService
{
    private readonly AppDbContext _context;

    public LanguageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Language>> GetLanguagesAsync()
    {
        return await _context.Languages.OrderByDescending(l => l.IsDefault).ThenBy(l => l.Name).ToListAsync();
    }

    public async Task<Language?> GetByIdAsync(int id)
    {
        return await _context.Languages.FindAsync(id);
    }

    public async Task<Language> CreateOrUpdateLanguageAsync(Language language)
    {
        var existing = await _context.Languages.FirstOrDefaultAsync(l => l.Code == language.Code);
        if (existing != null)
        {
            existing.IsActive = true;
            existing.Name = language.Name;
            if (language.IsDefault)
            {
                await SetDefaultInternal(existing);
            }
            await _context.SaveChangesAsync();
            return existing;
        }

        if (language.IsDefault)
        {
            await SetDefaultInternal(language);
        }

        language.IsActive = true;
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();
        return language;
    }

    public async Task<bool> ToggleActiveAsync(int id, bool isActive)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null) return false;

        if (language.IsDefault && !isActive) return false; // Cannot disable default

        language.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultAsync(int id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null) return false;

        await SetDefaultInternal(language);
        language.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task SetDefaultInternal(Language language)
    {
        var otherDefaults = await _context.Languages.Where(l => l.IsDefault && l.Id != language.Id).ToListAsync();
        foreach (var lang in otherDefaults)
        {
            lang.IsDefault = false;
        }
        language.IsDefault = true;
    }
}

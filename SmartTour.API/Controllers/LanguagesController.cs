using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguagesController : ControllerBase
{
    private readonly ILanguageService _languageService;

    public LanguagesController(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Language>>> GetLanguages()
    {
        return Ok(await _languageService.GetLanguagesAsync());
    }

    [HttpGet("{id}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<Language>> GetLanguage(int id)
    {
        var language = await _languageService.GetByIdAsync(id);
        if (language == null) return NotFound();
        return language;
    }

    [HttpPost]
    public async Task<ActionResult<Language>> PostLanguage(Language language)
    {
        var result = await _languageService.CreateOrUpdateLanguageAsync(language);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        var result = await _languageService.ToggleActiveAsync(id, false);
        if (!result) return BadRequest("Không thể xóa hoặc ngôn ngữ mặc định.");
        return NoContent();
    }

    [HttpPatch("{id}/set-default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var result = await _languageService.SetDefaultAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}

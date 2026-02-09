using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public LanguagesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Languages
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Language>>> GetLanguages()
    {
        // Trả về cả ngôn ngữ không hoạt động để quản lý? 
        // Thường trang quản lý nên thấy hết, UI sẽ filter hoặc show status.
        return await _context.Languages.OrderByDescending(l => l.IsDefault).ThenBy(l => l.Name).ToListAsync();
    }

    // GET: api/Languages/5
    [HttpGet("{id}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<ActionResult<Language>> GetLanguage(int id)
    {
        var language = await _context.Languages.FindAsync(id);

        if (language == null)
        {
            return NotFound();
        }

        return language;
    }

    // POST: api/Languages
    [HttpPost]
    public async Task<ActionResult<Language>> PostLanguage(Language language)
    {
        // Kiểm tra xem ngôn ngữ này đã tồn tại chưa (kể cả đã bị hủy kích hoạt)
        var existing = await _context.Languages.FirstOrDefaultAsync(l => l.Code == language.Code);
        if (existing != null)
        {
            if (existing.IsActive)
            {
                return BadRequest("Ngôn ngữ này đã tồn tại và đang hoạt động.");
            }
            else
            {
                // Nếu đã tồn tại nhưng bị ẩn, ta kích hoạt lại và cập nhật thông tin
                existing.IsActive = true;
                existing.Name = language.Name;
                if (language.IsDefault)
                {
                    await SetDefaultInternal(existing);
                }
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
        }

        if (language.IsDefault)
        {
            await SetDefaultInternal(language);
        }

        language.IsActive = true;
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLanguage), new { id = language.Id }, language);
    }

    // DELETE: api/Languages/5 (Soft Delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
        {
            return NotFound();
        }

        if (language.IsDefault)
        {
            return BadRequest("Không thể hủy kích hoạt ngôn ngữ mặc định.");
        }

        // Soft delete: Chỉ chuyển trạng thái hoạt động
        language.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Patch endpoint để chuyển đổi mặc định (Nếu cần sau này)
    [HttpPatch("{id}/set-default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null) return NotFound();

        await SetDefaultInternal(language);
        language.IsActive = true;
        await _context.SaveChangesAsync();
        
        return NoContent();
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

    private bool LanguageExists(int id)
    {
        return _context.Languages.Any(e => e.Id == id);
    }
}

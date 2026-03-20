using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Login đơn giản cho Mobile app — trả về userId thật từ DB.
    /// Không cần JWT, mobile dùng X-User-Id header.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username không được để trống." });

        // Tìm theo username hoặc email, không phân biệt hoa thường
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == request.Username.ToLower() ||
                 u.Email.ToLower()    == request.Username.ToLower())
                && u.IsActive);

        if (user == null)
            return Unauthorized(new { message = "Tài khoản không tồn tại." });

        // Kiểm tra password nếu user có PasswordHash
        if (!string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(request.Password))
        {
            bool valid;
            try
            {
                // valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                valid = request.Password == user.PasswordHash;
            }
            catch
            {
                valid = request.Password == user.PasswordHash;
            }

            if (!valid)
                return Unauthorized(new { message = "Sai mật khẩu." });
        }
        else if (!string.IsNullOrEmpty(user.PasswordHash) && string.IsNullOrEmpty(request.Password))
        {
            // Có hash nhưng không nhập password → sai
            return Unauthorized(new { message = "Vui lòng nhập mật khẩu." });
        }

        return Ok(new
        {
            userId   = user.Id,
            username = user.Username,
            email    = user.Email,
            fullName = user.FullName,
            roleId   = user.RoleId
        });
    }
}

public class LoginRequest
{
    public string  Username { get; set; } = string.Empty;
    public string? Password { get; set; }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.Shared.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace SmartTour.Web.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle(string returnUrl = "/")
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", new { returnUrl }),
                Items = { { "returnUrl", returnUrl } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                return Redirect($"/login?error=google_auth_failed");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var identifier = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Redirect("/login?error=email_not_provided");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = name ?? email,
                    Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString().Substring(0, 4),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    AuthProvider = "Google",
                    ProviderId = identifier,
                    RoleId = 3 // Default Role: VISITOR
                };
                _context.Users.Add(user);
            }
            else
            {
                user.AuthProvider = "Google";
                user.ProviderId = identifier;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            var localClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()) 
            };

            var roles = await _context.Roles.FindAsync(user.RoleId);
            if (roles != null)
            {
                localClaims.Add(new Claim(ClaimTypes.Role, roles.Name));
            }

            var claimsIdentity = new ClaimsIdentity(localClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpPost("login-local")]
        public async Task<IActionResult> LoginLocal([FromForm] string username, [FromForm] string password, [FromForm] string returnUrl = "/")
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Redirect($"/login?error=invalid_credentials");
            }

            // Tìm user theo Username hoặc Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

            if (user == null)
            {
                return Redirect($"/login?error=invalid_credentials");
            }

            // Kiểm tra mật khẩu bằng PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Redirect($"/login?error=invalid_credentials");
            }

            if (!user.IsActive)
            {
                return Redirect($"/login?error=account_locked");
            }

            var localClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Id.ToString()),
            };

            var role = await _context.Roles.FindAsync(user.RoleId);
            if (role != null)
            {
                localClaims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var claimsIdentity = new ClaimsIdentity(localClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}

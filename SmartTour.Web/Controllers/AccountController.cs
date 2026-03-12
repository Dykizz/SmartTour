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
            // PHẢI dùng GoogleDefaults.AuthenticationScheme để lấy thông tin từ Google trả về
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded) {
                 result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            
            if (!result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("smarttour://"))
                    return Redirect($"{returnUrl}?error=auth_failed");
                return Redirect($"/login?error=google_auth_failed");
            }

            var claims = result.Principal?.Claims;
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

            // [SmartTour] Tự động gán gói mặc định nếu có và nếu user chưa có gói nào
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (subscription == null && user.RoleId != 1)
            {
                var defaultPackage = await _context.ServicePackages.FirstOrDefaultAsync(p => p.IsDefault && p.SoftDeleteAt == null);
                if (defaultPackage != null)
                {
                    subscription = new Subscription
                    {
                        UserId = user.Id,
                        PackageId = defaultPackage.Id,
                        PriceAtPurchase = defaultPackage.Price,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(defaultPackage.DurationDays)
                    };
                    _context.Subscriptions.Add(subscription);
                    
                    if (user.RoleId == 3)
                    {
                        user.RoleId = 2; // Auto upgrade to SELLER
                        _context.Users.Update(user);
                    }
                    await _context.SaveChangesAsync();
                }
            }
            var localClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // IMPORTANT
                new Claim(ClaimTypes.Role, user.RoleId.ToString()) 
            };

            var roles = await _context.Roles.FindAsync(user.RoleId);
            if (roles != null)
            {
                localClaims.Add(new Claim(ClaimTypes.Role, roles.Name));
            }

            var claimsIdentity = new ClaimsIdentity(localClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Chuyển hướng cho Mobile app
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("smarttour://"))
            {
                var finalUrl = $"{returnUrl}{(returnUrl.Contains("?") ? "&" : "?")}userId={user.Id}&email={user.Email}";
                return Redirect(finalUrl);
            }

            if (Url.IsLocalUrl(returnUrl) && returnUrl != "/")
            {
                return Redirect(returnUrl);
            }

            // [SmartTour] Phân tách Web và Mobile
            // - Mobile: Đã được xử lý bởi if (returnUrl.StartsWith("smarttour://")) ở trên. Nếu người dùng mobile đăng nhập thì về app luôn.
            // - Web: Bất kỳ ai vào web (không phải Admin - RoleId 1), nếu chưa có gói (SELLER) hoặc đang là VISITOR (RoleId 3) thì đều dẫn vào trang Bảng giá
            if (user.RoleId != 1)
            {
                var currentSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (currentSubscription == null)
                {
                    // Truyền thêm thông tin để trang Pricing biết và thực hiện nâng cấp role
                    return Redirect("/pricing?hideNav=true&upgrade=true");
                }
            }

            return Redirect("/");
        }


        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
    }
}

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartTour.Web.Services;

public class CookieHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            Console.WriteLine("[CookieHandler] HttpContext is NULL. Cannot forward cookies. (Blazor Server limitation?)");
        }
        else
        {
            Console.WriteLine($"[CookieHandler] HttpContext is present. User: {context.User?.Identity?.Name ?? "Anonymous"}. IsAuth: {context.User?.Identity?.IsAuthenticated}");
            
            // Lấy cookie xác thực từ request hiện tại của người dùng
            if (context.Request.Cookies.TryGetValue(CookieAuthenticationDefaults.AuthenticationScheme, out var cookieValue))
            {
                Console.WriteLine($"[CookieHandler] Found Auth Cookie! Length: {cookieValue.Length}. Forwarding to API...");
                // Đính kèm cookie vào request gửi đi API
                request.Headers.Add("Cookie", $"{CookieAuthenticationDefaults.AuthenticationScheme}={cookieValue}");
            }
            else
            {
                Console.WriteLine($"[CookieHandler] Auth Cookie '{CookieAuthenticationDefaults.AuthenticationScheme}' NOT found in request.");
                foreach (var c in context.Request.Cookies) Console.WriteLine($"[CookieHandler] Available Cookie: {c.Key}");
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

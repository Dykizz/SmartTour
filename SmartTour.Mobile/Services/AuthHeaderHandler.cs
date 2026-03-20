using System.Net.Http.Headers;
using SmartTour.Mobile.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace SmartTour.Mobile.Services;

/// <summary>
/// Interceptor tự động chèn X-User-Id vào mọi request HttpClient từ Mobile.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthHeaderHandler(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage? request, CancellationToken cancellationToken)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // Tìm UserId trong các Claims đã lưu
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                        ?? user.FindFirst("UserId")?.Value;

            if (!string.IsNullOrEmpty(userId) && request != null)
            {
                // Thêm Header nhận diện cho Backend
                if (request.Headers.Contains("X-User-Id"))
                    request.Headers.Remove("X-User-Id");
                    
                request.Headers.Add("X-User-Id", userId);
            }
        }

        return await base.SendAsync(request!, cancellationToken);
    }
}

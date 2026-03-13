using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace SmartTour.Web.Services;

public class ServerSideAuthHandler : DelegatingHandler
{
    private readonly UserSessionService _userSessionService;
    private readonly IConfiguration _config;

    public ServerSideAuthHandler(UserSessionService userSessionService, IConfiguration config)
    {
        _userSessionService = userSessionService;
        _config = config;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Thêm API Key (để chứng minh Web App hợp lệ)
        var apiKey = _config["SmartTourApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-SmartTour-Api-Key", apiKey);
        }

        // 2. Thêm User ID hiện tại (để API biết ai đang gọi) - Lấy từ Session Service
        try 
        {
            if (_userSessionService.IsAuthenticated)
            {
                var userId = _userSessionService.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    request.Headers.Add("X-SmartTour-User-Id", userId);
                }

                var role = _userSessionService.UserRole;
                if (!string.IsNullOrEmpty(role))
                {
                    request.Headers.Add("X-SmartTour-User-Role", role);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ServerSideAuthHandler] Error getting user session: {ex.Message}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

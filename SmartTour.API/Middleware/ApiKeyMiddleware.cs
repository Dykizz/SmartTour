using System.Security.Claims;

namespace SmartTour.API.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = _config["SmartTourApiKey"];
        
        // Chỉ xử lý nếu có cấu hình Key và Request có gửi Header Key
        if (!string.IsNullOrEmpty(apiKey) && context.Request.Headers.TryGetValue("X-SmartTour-Api-Key", out var extractedApiKey))
        {
            if (apiKey.Equals(extractedApiKey))
            {
                // Key hợp lệ - Kiểm tra xem có gửi kèm User ID để "mạo danh" hợp pháp không
                if (context.Request.Headers.TryGetValue("X-SmartTour-User-Id", out var userId))
                {
                    // _logger.LogInformation($"[ApiKeyMiddleware] Authenticated as Server-Side Proxy for User ID: {userId}");
                    
                    var claims = new[] 
                    { 
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, "Server Proxy"), 
                        new Claim("IsServerSideRequest", "true")
                    };
                    var identity = new ClaimsIdentity(claims, "ApiKey"); // Ensure AuthenticationType is set
                    context.User = new ClaimsPrincipal(identity);
                }
                else
                {
                    _logger.LogWarning("[ApiKeyMiddleware] Valid API Key but NO User ID header.");
                }
            }
            else
            {
                _logger.LogWarning($"[ApiKeyMiddleware] Invalid API Key. Received: {extractedApiKey}");
            }
        }

        await _next(context);
    }
}

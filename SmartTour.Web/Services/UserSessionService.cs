namespace SmartTour.Web.Services;

public class UserSessionService
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public void SetUser(string? userId, string? userName, string? role = null)
    {
        UserId = userId;
        UserName = userName;
        UserRole = role;
    }
}

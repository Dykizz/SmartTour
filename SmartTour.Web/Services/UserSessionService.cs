namespace SmartTour.Web.Services;

public class UserSessionService
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public void SetUser(string? userId, string? userName)
    {
        UserId = userId;
        UserName = userName;
    }
}

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SmartTour.Mobile.Services;

public class MobileAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    private bool _isInitialized = false;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            await LoadStateFromStorage();
            _isInitialized = true;
        }
        return new AuthenticationState(_currentUser);
    }

    private async Task LoadStateFromStorage()
    {
        try
        {
            var email = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_email");
            var userId = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_userid");
            var fullName = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_fullname");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(userId))
            {
                var identity = CreateIdentity(email, userId, fullName ?? "");
                _currentUser = new ClaimsPrincipal(identity);
            }
        }
        catch { /* Fallback to guest if storage fails */ }
    }

    private ClaimsIdentity CreateIdentity(string email, string userId, string fullName)
    {
        return new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("UserId", userId),
            new Claim("FullName", fullName),
            new Claim("MobileUser", "true")
        }, "MobileAuth");
    }

    public async void MarkUserAsAuthenticated(string email, string userId, string fullName = "")
    {
        var identity = CreateIdentity(email, userId, fullName);
        _currentUser = new ClaimsPrincipal(identity);
        
        try
        {
            await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_email", email);
            await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_userid", userId);
            await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_fullname", fullName);
        }
        catch { }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async void MarkUserAsLoggedOut()
    {
        _currentUser = new(new ClaimsIdentity());
        
        try
        {
            Microsoft.Maui.Storage.SecureStorage.Remove("auth_email");
            Microsoft.Maui.Storage.SecureStorage.Remove("auth_userid");
        }
        catch { }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

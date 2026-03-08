using Android.App;
using Android.Content;
using Android.Content.PM;

namespace SmartTour.Mobile
{
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "smarttour")]
    public class WebAuthenticatorActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
    }
}

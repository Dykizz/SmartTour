using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.Authorization;
using SmartTour.Mobile.Services;

namespace SmartTour.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

#if ANDROID
		// Cho phép Android WebView tự phát audio (bypass autoplay policy)
		BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("EnableMediaAutoplay", (handler, view) =>
		{
			if (handler.PlatformView is Android.Webkit.WebView webView)
			{
				webView.Settings.MediaPlaybackRequiresUserGesture = false;
			}
		});
#endif

		// Authentication & Authorization Services
		builder.Services.AddAuthorizationCore();
		builder.Services.AddScoped<AuthenticationStateProvider, MobileAuthStateProvider>();
		builder.Services.AddSingleton<LanguageService>();
		builder.Services.AddSingleton<GeofenceAudioService>();

		string baseUrl = "http://127.0.0.1:5164/";
#if ANDROID
		baseUrl = "http://10.0.2.2:5164/";
#endif
		builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });

		var app = builder.Build();

        // Cấu hình bắt lỗi toàn cục cho luồng Task (nếu cần)
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] {args.ExceptionObject}");
        };

        return app;
	}
}

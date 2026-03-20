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

		// Cho phép Blazor WebView cấp quyền WebRTC (Camera/Mic) mà không bị chặn đen thui
		BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("PermissionRequested", (handler, view) =>
		{
			if (handler.PlatformView is Android.Webkit.WebView webView)
			{
				webView.SetWebChromeClient(new PermissionManagingBlazorWebChromeClient());
			}
		});
#endif

		// Authentication & Authorization Services
		builder.Services.AddAuthorizationCore();
		builder.Services.AddScoped<AuthenticationStateProvider, MobileAuthStateProvider>();
		builder.Services.AddSingleton<LanguageService>();
		builder.Services.AddSingleton<GeofenceAudioService>();
		
		// Đăng ký AuthHeaderHandler (Interceptor)
		builder.Services.AddTransient<AuthHeaderHandler>();

		string baseUrl = "http://127.0.0.1:5164/";
#if ANDROID
		// Đổi IP về 127.0.0.1 trên Android dành cho cả Emulator lẫn Thiết bị thật (thông qua adb reverse)
		baseUrl = "http://127.0.0.1:5164/";
#endif

		// Cấu hình HttpClient dùng AuthHeaderHandler để chèn X-User-Id Header
		builder.Services.AddHttpClient("SmartTourApi", client => 
		{
			client.BaseAddress = new Uri(baseUrl);
		})
		.AddHttpMessageHandler<AuthHeaderHandler>();

		// Đăng ký HttpClient mặc định lấy từ Factory
		builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("SmartTourApi"));

		var app = builder.Build();

        // Cấu hình bắt lỗi toàn cục cho luồng Task (nếu cần)
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] {args.ExceptionObject}");
        };

        return app;
	}
}

#if ANDROID
internal class PermissionManagingBlazorWebChromeClient : Android.Webkit.WebChromeClient
{
    public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
    {
        try 
        {
            request?.Grant(request.GetResources());
        } 
        catch 
        {
            base.OnPermissionRequest(request);
        }
    }
}
#endif

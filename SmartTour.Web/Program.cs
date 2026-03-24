using SmartTour.Web.Components;
using SmartTour.Web.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => 
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });
builder.Services.AddMudServices();
builder.Services.AddScoped(sp => 
{
    var authHandler = sp.GetRequiredService<ServerSideAuthHandler>();
    authHandler.InnerHandler = new HttpClientHandler();
    
    var client = new HttpClient(authHandler)
    {
        BaseAddress = new Uri("http://localhost:5164/")
    };
    return client;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserSessionService>(); // Add UserSessionService
builder.Services.AddTransient<ServerSideAuthHandler>();
builder.Services.AddTransient<CookieHandler>(); // Keep for backward compat or remove if unused
builder.Services.AddDataProtection().SetApplicationName("SmartTourShared");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? "YOUR_GOOGLE_CLIENT_ID";
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    });

builder.Services.AddCascadingAuthenticationState();


var mapProvider = builder.Configuration["MAP_PROVIDER"] ?? "Mapbox";

if (mapProvider == "Mapbox")
{
    builder.Services.AddScoped<IMapService, MapboxService>();
}
else if (mapProvider == "Google")
{
    builder.Services.AddScoped<IMapService, GoogleMapService>();
}
else
{
    throw new InvalidOperationException($"Unknown MAP_PROVIDER: {mapProvider}. Valid values: Mapbox, Google");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapRazorComponents<SmartTour.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }

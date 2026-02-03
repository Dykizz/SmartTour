using SmartTour.Web.Components;
using SmartTour.Web.Services;
using MudBlazor.Services;
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
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? "YOUR_GOOGLE_CLIENT_ID";
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
    });

builder.Services.AddCascadingAuthenticationState();


var mapProvider = builder.Configuration["MAP_PROVIDER"] ?? "Vietmap";

if (mapProvider == "Vietmap")
{
    builder.Services.AddScoped<IMapService, VietmapService>();
}
else if (mapProvider == "Mapbox")
{
    builder.Services.AddScoped<IMapService, MapboxService>();
}
else
{
    throw new InvalidOperationException($"Unknown MAP_PROVIDER: {mapProvider}. Valid values: Vietmap, Mapbox");
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

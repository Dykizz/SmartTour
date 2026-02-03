using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using SmartTour.Shared.Models;

namespace SmartTour.Web.Services;

/// <summary>
/// Implementation của Map Service sử dụng Vietmap
/// </summary>
public class VietmapService : IMapService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private string? _apiKey;

    public string ProviderName => "Vietmap";

    public VietmapService(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    private string GetApiKey()
    {
        if (_apiKey == null)
        {
            _apiKey = _configuration["VIETMAP_API_KEY"] 
                      ?? throw new InvalidOperationException("VIETMAP_API_KEY not found in configuration");
        }
        return _apiKey;
    }

    public async Task InitMapAsync<T>(double lat, double lng, string elementId, DotNetObjectReference<T> dotNetHelper) where T : class
    {
        await _jsRuntime.InvokeVoidAsync("vietmapInterop.initMap", lat, lng, elementId, dotNetHelper, GetApiKey());
    }

    public async Task SetMarkerAsync(double lat, double lng, string elementId)
    {
        await _jsRuntime.InvokeVoidAsync("vietmapInterop.setMarker", lat, lng, elementId);
    }

    public async Task InvalidateSizeAsync(string elementId)
    {
        await _jsRuntime.InvokeVoidAsync("vietmapInterop.invalidateSize", elementId);
    }

    public async Task<List<MapPrediction>> GetPredictionsAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 3)
            return new List<MapPrediction>();

        try
        {
            var predictions = await _jsRuntime.InvokeAsync<List<MapPrediction>>(
                "vietmapInterop.getPredictions", 
                cancellationToken, 
                input, 
                GetApiKey()
            );
            return predictions ?? new List<MapPrediction>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Vietmap search error: {ex.Message}");
            return new List<MapPrediction>();
        }
    }

    public async Task<MapPlaceDetails?> GetPlaceDetailsAsync(string placeId)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<MapPlaceDetails>(
                "vietmapInterop.getPlaceDetails", 
                placeId, 
                GetApiKey()
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Vietmap place details error: {ex.Message}");
            return null;
        }
    }
}

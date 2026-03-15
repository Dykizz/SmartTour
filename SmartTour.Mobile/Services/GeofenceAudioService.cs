using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices.Sensors;
using SmartTour.Shared.Models;
using System.Net.Http.Json;

namespace SmartTour.Mobile.Services;

/// <summary>
/// Service Singleton theo dõi GPS và kích hoạt audio tự động khi user vào vùng Geofence của POI.
/// Dùng IServiceScopeFactory thay vì inject HttpClient trực tiếp để tránh Captive Dependency.
/// </summary>
public class GeofenceAudioService : IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LanguageService _languageService;

    // Event được kích hoạt khi user bước vào vùng geofence chưa được phát
    public event Func<PoiGeofenceDto, PoiAudioFile, Task>? AudioTriggered;

    // Trạng thái
    public bool IsRunning { get; private set; } = false;
    public double? LastKnownLatitude { get; private set; }
    public double? LastKnownLongitude { get; private set; }

    // ── Banner state (Singleton ─ tồn tại qua mọi trang) ──────────────────────
    public bool BannerVisible { get; private set; } = false;
    public bool IsAudioPlaying { get; private set; } = false;
    public string BannerPoiName { get; private set; } = string.Empty;
    public PoiGeofenceDto? CurrentPoi { get; private set; }

    /// Được raise bất cứ khi banner state thay đổi để các trang gọi StateHasChanged().
    public event Action? BannerStateChanged;

    // Nội bộ
    private CancellationTokenSource? _cts;
    private List<PoiGeofenceDto> _allPois = new();
    private const double DefaultGeofenceMeters = 50.0;
    private const int PollingIntervalSeconds = 8;

    public GeofenceAudioService(IServiceScopeFactory scopeFactory, LanguageService languageService)
    {
        _scopeFactory = scopeFactory;
        _languageService = languageService;
    }

    /// <summary>
    /// Bắt đầu vòng lặp GPS. Gọi method này khi trang Home được load.
    /// </summary>
    public async Task StartAsync()
    {
        if (IsRunning) return;

        // --- Kiểm tra và xin quyền GPS runtime (Android 6+ bắt buộc) ---
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            System.Diagnostics.Debug.WriteLine("[GeofenceService] Đang xin quyền GPS...");
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status != PermissionStatus.Granted)
        {
            System.Diagnostics.Debug.WriteLine("[GeofenceService] ❌ User từ chối quyền GPS. Service không khởi động.");
            return;
        }

        System.Diagnostics.Debug.WriteLine("[GeofenceService] ✅ Quyền GPS được cấp.");

        IsRunning = true;
        _cts = new CancellationTokenSource();

        // Tải danh sách POI 1 lần khi bắt đầu
        await LoadPoisAsync();

        System.Diagnostics.Debug.WriteLine($"[GeofenceService] Đã tải {_allPois.Count} POI. Bắt đầu vòng lặp GPS...");

        // Chạy vòng lặp trên background thread
        _ = Task.Run(() => PollingLoopAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Dừng vòng lặp GPS. Gọi khi trang Home bị Dispose.
    /// </summary>
    public Task StopAsync()
    {
        if (!IsRunning) return Task.CompletedTask;
        IsRunning = false;
        _cts?.Cancel();
        System.Diagnostics.Debug.WriteLine("[GeofenceService] Đã dừng.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tải lại danh sách POI từ API (dùng khi cần refresh).
    /// </summary>
    public async Task ReloadPoisAsync() => await LoadPoisAsync();

    /// Reset trạng thái phát để có thể trigger lại.
    public void ResetPlayedHistory()
    {
        CurrentPoi = null;
        BannerVisible = false;
        IsAudioPlaying = false;
        BannerStateChanged?.Invoke();
    }

    /// Cập nhật trạng thái chơi / dừng từ bên ngoài (component bấm Pause).
    public void SetAudioPlaying(bool playing)
    {
        IsAudioPlaying = playing;
        BannerStateChanged?.Invoke();
    }

    /// Đóng banner và dừng audio (bấm X).
    public void CloseBanner()
    {
        BannerVisible = false;
        IsAudioPlaying = false;
        BannerStateChanged?.Invoke();
    }

    // ─── Private Helpers ────────────────────────────────────────────────────

    private async Task LoadPoisAsync()
    {
        try
        {
            // Tạo scope mới để lấy HttpClient (tránh Captive Dependency)
            using var scope = _scopeFactory.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

            var pois = await http.GetFromJsonAsync<List<PoiGeofenceDto>>("api/pois/geofence");
            if (pois != null)
            {
                _allPois = pois;
                System.Diagnostics.Debug.WriteLine($"[GeofenceService] Loaded {pois.Count} POIs for geofencing.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GeofenceService] Lỗi tải POI: {ex.Message}");
        }
    }

    private async Task PollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await CheckLocationAsync();
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeofenceService] Lỗi vòng lặp: {ex.Message}");
                try { await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), token); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task CheckLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location == null)
            {
                System.Diagnostics.Debug.WriteLine("[GeofenceService] Không lấy được vị trí GPS.");
                return;
            }

            LastKnownLatitude = location.Latitude;
            LastKnownLongitude = location.Longitude;

            System.Diagnostics.Debug.WriteLine(
                $"[GeofenceService] 📍 GPS: {location.Latitude:F5}, {location.Longitude:F5} | Đang kiểm tra {_allPois.Count} POI...");

            string langCode = _languageService.SelectedLanguage;

            PoiGeofenceDto? closestPoi = null;
            double minDistance = double.MaxValue;

            foreach (var poi in _allPois)
            {
                var poiLocation = new Location(poi.Latitude, poi.Longitude);
                double distanceKm = Location.CalculateDistance(location, poiLocation, DistanceUnits.Kilometers);
                double distanceMeters = distanceKm * 1000;
                double radius = poi.GeofenceRadius > 0 ? poi.GeofenceRadius : DefaultGeofenceMeters;

                System.Diagnostics.Debug.WriteLine(
                    $"[GeofenceService]   POI #{poi.Id} \"{poi.Name}\": {distanceMeters:F1}m (radius: {radius}m)");

                if (distanceMeters <= radius && distanceMeters < minDistance)
                {
                    minDistance = distanceMeters;
                    closestPoi = poi;
                }
            }

            if (closestPoi == null)
            {
                // User không ở trong bất kỳ vùng nào
                if (CurrentPoi != null)
                {
                    System.Diagnostics.Debug.WriteLine("[GeofenceService] 🚪 User đã ra khỏi tất cả các vùng POI.");
                    CurrentPoi = null;
                    BannerVisible = false;
                    IsAudioPlaying = false;
                    BannerStateChanged?.Invoke();
                }
            }
            else
            {
                // 1. Logic CƯỚP QUYỀN (Preemption): 
                // Nếu POI gần nhất khác với POI đang "active" hiện tại -> Chuyển audio ngay
                if (CurrentPoi?.Id != closestPoi.Id)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeofenceService] 🔄 Đổi sang POI gần hơn: #{closestPoi.Id} \"{closestPoi.Name}\" ({minDistance:F1}m)");
                    await TriggerNewAudio(closestPoi, langCode);
                }
                // 2. Logic PHÁT LẠI KHI VÀO TÂM (Re-trigger):
                // Nếu khách tiến sát tâm < 10m và audio cũ đã phát xong/dừng -> Phát lại
                else if (minDistance < 10.0 && !IsAudioPlaying)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeofenceService] 🎯 User quay lại tâm POI #{closestPoi.Id} ({minDistance:F1}m) -> Phát lại audio.");
                    await TriggerNewAudio(closestPoi, langCode);
                }
            }
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("[GeofenceService] GPS không được hỗ trợ.");
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[GeofenceService] Không có quyền GPS.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GeofenceService] Lỗi: {ex.Message}");
        }
    }

    private async Task TriggerNewAudio(PoiGeofenceDto poi, string langCode)
    {
        var audio = poi.AudioFiles.FirstOrDefault(a => a.LanguageCode == langCode)
                 ?? poi.AudioFiles.FirstOrDefault(a => a.LanguageCode == "vi")
                 ?? poi.AudioFiles.FirstOrDefault();

        if (audio == null)
        {
            System.Diagnostics.Debug.WriteLine($"[GeofenceService]   ⚠️ Không có audio cho POI #{poi.Id}");
            return;
        }

        CurrentPoi = poi;
        BannerPoiName = poi.Name;
        BannerVisible = true;
        IsAudioPlaying = true;
        BannerStateChanged?.Invoke();

        System.Diagnostics.Debug.WriteLine(
            $"[GeofenceService] ✅ TRIGGER POI #{poi.Id} \"{poi.Name}\" -> {audio.FileUrl}");

        if (AudioTriggered != null)
            await AudioTriggered.Invoke(poi, audio);
    }

    /// <summary>
    /// Được gọi từ JavaScript (audioHelper.js) khi file audio kết thúc.
    /// </summary>
    [Microsoft.JSInterop.JSInvokable]
    public void OnAudioEnded()
    {
        System.Diagnostics.Debug.WriteLine($"[GeofenceService] ⏹️ Audio POI #{CurrentPoi?.Id} đã kết thúc.");
        IsAudioPlaying = false;
        BannerStateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}

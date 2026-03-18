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

    // --- Thêm biến trạng thái công tắc Auto-Play ---
    public bool IsAutoPlayEnabled { get; private set; } = true; // Mặc định bật theo yêu cầu thông thường, hoặc false nếu muốn user tự chọn

    public void ToggleAutoPlay(bool isEnabled)
    {
        IsAutoPlayEnabled = isEnabled;

        if (!isEnabled && IsAudioPlaying)
        {
            // Chỉ tắt trạng thái phát nhạc, KHÔNG tắt BannerVisible
            IsAudioPlaying = false;
            System.Diagnostics.Debug.WriteLine("[Geofence] 🛑 User tắt AutoPlay -> Dừng nhạc, giữ banner chờ.");
        }

        System.Diagnostics.Debug.WriteLine($"[GeofenceService] Auto-Play is now {(isEnabled ? "ENABLED" : "DISABLED")}");
        BannerStateChanged?.Invoke();
    }

    // ── Banner state (Singleton ─ tồn tại qua mọi trang) ──────────────────────
    public bool BannerVisible { get; private set; } = false;
    public bool IsAudioPlaying { get; private set; } = false;
    public string BannerPoiName { get; private set; } = string.Empty;
    public PoiGeofenceDto? CurrentPoi { get; private set; }

    public IReadOnlyList<PoiGeofenceDto> AllPois => _allPois;

    /// Được raise bất cứ khi banner state thay đổi để các trang gọi StateHasChanged().
    public event Action? BannerStateChanged;

    // Nội bộ
    private CancellationTokenSource? _cts;
    private List<PoiGeofenceDto> _allPois = new();
    private const double DefaultGeofenceMeters = 50.0;
    private const int PollingIntervalSeconds = 8;

    // Theo dõi các POI đang ở trong vùng và đã phát
    private HashSet<int> _insidePoiIds = new();
    private HashSet<int> _playedPoiIds = new();

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
        _playedPoiIds.Clear();
        _insidePoiIds.Clear();
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

    /// <summary>
    /// Hàm để người dùng bấm nút "Nghe lại" trên giao diện.
    /// Bỏ qua kiểm tra công tắc tự động và bỏ qua danh sách đã phát.
    /// </summary>
    public async Task PlayManualAsync(PoiGeofenceDto poi, string langCode)
    {
        var audio = poi.AudioFiles.FirstOrDefault(a => a.LanguageCode == langCode)
                    ?? poi.AudioFiles.FirstOrDefault(a => a.LanguageCode == "vi")
                    ?? poi.AudioFiles.FirstOrDefault();

        if (audio != null)
        {
            CurrentPoi = poi;
            BannerPoiName = poi.Name;
            BannerVisible = true;
            IsAudioPlaying = true;
            BannerStateChanged?.Invoke();

            if (AudioTriggered != null)
                await AudioTriggered.Invoke(poi, audio);
        }
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

            HashSet<int> currentlyInsideIds = new();
            PoiGeofenceDto? closestPoi = null;
            double minDistance = double.MaxValue;

            foreach (var poi in _allPois)
            {
                var poiLocation = new Location(poi.Latitude, poi.Longitude);
                double distanceKm = Location.CalculateDistance(location, poiLocation, DistanceUnits.Kilometers);
                double distanceMeters = distanceKm * 1000;
                double radius = poi.GeofenceRadius > 0 ? poi.GeofenceRadius : DefaultGeofenceMeters;

                if (distanceMeters <= radius)
                {
                    currentlyInsideIds.Add(poi.Id);
                    if (distanceMeters < minDistance)
                    {
                        minDistance = distanceMeters;
                        closestPoi = poi;
                    }
                }
            }

            // --- Pass 2: Xử lý POI gần nhất ---
            if (closestPoi != null)
            {
                // Cập nhật Banner để người dùng biết họ đang ở đâu
                if (CurrentPoi?.Id != closestPoi.Id)
                {
                    CurrentPoi = closestPoi;
                    BannerPoiName = closestPoi.Name;
                    BannerVisible = true;
                    BannerStateChanged?.Invoke();

                    System.Diagnostics.Debug.WriteLine($"[Geofence] Đổi vùng ưu tiên sang: {closestPoi.Name} ({minDistance:F1}m)");
                }

                // LOGIC TỰ ĐỘNG PHÁT: Phải BẬT công tắc VÀ chưa phát trong lần vào vùng này
                if (IsAutoPlayEnabled && !_playedPoiIds.Contains(closestPoi.Id))
                {
                    _playedPoiIds.Add(closestPoi.Id);
                    _insidePoiIds.Add(closestPoi.Id);

                    System.Diagnostics.Debug.WriteLine($"[Geofence] 🔊 Tự động phát audio cho: {closestPoi.Name}");
                    await TriggerNewAudio(closestPoi, langCode);
                }
                else if (!_insidePoiIds.Contains(closestPoi.Id))
                {
                    // Trường hợp user tắt AutoPlay nhưng mới bước vào vùng -> Mark là đã ở trong vùng để không tự phát khi bật lại giữa chừng (tùy chọn UI)
                    _insidePoiIds.Add(closestPoi.Id);
                }
            }

            // --- Pass 3: Rời khỏi vùng (Reset để lần sau vào lại sẽ phát như mới) ---
            var exitedIds = new HashSet<int>(_insidePoiIds);
            exitedIds.ExceptWith(currentlyInsideIds);
            foreach (var exitedId in exitedIds)
            {
                _insidePoiIds.Remove(exitedId);
                _playedPoiIds.Remove(exitedId); // QUAN TRỌNG: Xóa khỏi danh sách đã phát để lần sau vào lại sẽ trigger
                System.Diagnostics.Debug.WriteLine($"[Geofence] 🚪 Đã ra khỏi POI #{exitedId}, sẵn sàng phát lại nếu quay vào.");

                // Nếu POI vừa ra khỏi chính là POI đang hiển thị trên Banner thì đóng Banner lại
                if (CurrentPoi?.Id == exitedId)
                {
                    CurrentPoi = null; // QUAN TRỌNG: Clear để re-entry hoạt động
                    BannerVisible = false;
                    IsAudioPlaying = false;
                    BannerStateChanged?.Invoke();
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

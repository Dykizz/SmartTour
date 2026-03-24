using Microsoft.EntityFrameworkCore;
using SmartTour.Mobile.Data;
using SmartTour.Shared.Models;
using System.Net.Http.Json;

namespace SmartTour.Mobile.Services;

/// <summary>
/// Service chuyên xử lý logic Offline-First (Cache Data) chuẩn Best Practice cho MAUI Blazor kết hợp EF Core SQLite
/// </summary>
public class OfflineSyncService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MediaCacheService _mediaCache;

    // --- QUẢN LÝ TRẠNG THÁI BIỂU TƯỢNG SYNC NGẦM ---
    private int _syncTasksCount = 0;
    public bool IsSyncing => _syncTasksCount > 0;
    public event Action? OnSyncStateChanged;

    // Khóa đụng độ đa luồng
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private class SyncJobFormat
    {
        public string Endpoint { get; set; } = string.Empty;
        public System.Text.Json.JsonElement? Body { get; set; }
    }

    private void BeginSync()
    {
        Interlocked.Increment(ref _syncTasksCount);
        OnSyncStateChanged?.Invoke();
    }

    private void EndSync()
    {
        Interlocked.Decrement(ref _syncTasksCount);
        OnSyncStateChanged?.Invoke();
    }

    // Cache TTL chống Spam API
    private bool ShouldSyncApi(string endpointKey)
    {
        // [YÊU CẦU MỚI] - BỎ GIỚI HẠN 5 PHÚT (REAL-TIME CẬP NHẬT LIÊN TỤC LUỒNG NGẦM)
        // Chỉ cần điện thoại Đang CÓ MẠNG là ứng dụng sẽ liên tục móc API tự động lấy cái Mới Nhất
        return Microsoft.Maui.Networking.Connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;
    }

    public OfflineSyncService(IDbContextFactory<AppDbContext> dbContextFactory, IHttpClientFactory httpClientFactory, MediaCacheService mediaCache)
    {
        _dbContextFactory = dbContextFactory;
        _mediaCache = mediaCache;
        _httpClientFactory = httpClientFactory; // Chỉ lưu Factory để tránh lỗi Socket Android
    }

    /// <summary>
    /// Mẫu phương thức "Cache-Then-Network"
    /// 1. Tải và đọc dữ liệu từ Local SQLite ngay lập tức. Cập nhật UI ngay nếu có Local Data.
    /// 2. Gọi API để tải Data mới không gây gián đoạn UI.
    /// 3. So khớp/Cập nhật CSDL SQLite nếu API có Data mới. 
    /// 4. Gọi Callback để UI tự Bind lại Data mới lấy được.
    /// </summary>
    public async Task<List<Category>> GetCategoriesAsync(Action<List<Category>>? onDataUpdated = null)
    {
        List<Category> localCategories = new();

        // --- 1. LẤY DỮ LIỆU LOCAL (OFFLINE) ---
        try
        {
            // Mở Scope riêng của DbContext (Sử dụng using để tự hủy Context an toàn, không làm lock DB của người khác)
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            
            // AsNoTracking giúp truy vấn CSDL nhanh hơn bằng cách không đính kèm kết quả vào Tracker
            localCategories = await localDb.Categories.AsNoTracking().ToListAsync();

            // Notify UI nếu có Data (UI show data liền)
            if (localCategories.Any() && onDataUpdated != null)
            {
                onDataUpdated.Invoke(localCategories);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalDb] Lỗi lấy Category Offline: {ex.Message}");
        }

        // CHỐNG SPAM: Nếu Local đã có Data VÀ chưa vượt quá 5 phút thì Mặc kệ Server (Không đồng bộ ngầm gây lag)
        if (!localCategories.Any() || ShouldSyncApi("categories"))
        {
            // --- 2. GỌI FETCH TỪ API BACKGROUND ---
            _ = Task.Run(async () =>
            {
                BeginSync();
                try
                {
                    var http = _httpClientFactory.CreateClient("SmartTourApi");
                    var networkCategories = await http.GetFromJsonAsync<List<Category>>("api/categories");

                    if (networkCategories != null && networkCategories.Any())
                    {
                        using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                        await updateDb.Categories.ExecuteDeleteAsync();
                        await updateDb.Categories.AddRangeAsync(networkCategories);
                        await updateDb.SaveChangesAsync();
                        onDataUpdated?.Invoke(networkCategories);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NetworkDb] Lỗi gọi API Categories: {ex.Message}");
                }
                finally { EndSync(); }
            });
        }

        return localCategories ?? new();
    }

    /// <summary>
    /// Fetch danh sách POI chuẩn Offline-First hỗ trợ PagedResponse.
    /// Khác với Category, POI được fetch theo phân trang trên từng điều kiện, nên ta dùng cơ chế UPSERT (Cập nhật hoặc Thêm mới) 
    /// thay vì xóa sạch (Truncate) tránh làm mất các POI ở trang khác/điều kiện khác.
    /// </summary>
    public async Task<PagedResponse<Poi>> GetPoisAsync(string apiQueryString, Action<PagedResponse<Poi>>? onDataUpdated = null)
    {
        PagedResponse<Poi> localResponse = new() { Items = new List<Poi>(), TotalCount = 0, PageNumber = 1, PageSize = 10 };

        // --- 1. LẤY DATA LOCAL (CACHE) Cơ bản ---
        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            // Nhả kèm thông tin Hình ảnh và Danh mục để hiển thị trên Card UI Home
            var pois = await localDb.Pois
                .Include(p => p.Images)
                .Include(p => p.Category)
                .AsNoTracking()
                .Take(20)
                .ToListAsync(); // Load nhanh 20 item có sẵn
            if (pois.Any())
            {
                localResponse = new PagedResponse<Poi>() { Items = pois, TotalCount = pois.Count, PageNumber = 1, PageSize = pois.Count };
                onDataUpdated?.Invoke(localResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocalDb] Lỗi POI Offline: {ex.Message}");
        }

        // CHỐNG SPAM: Chỉ Fetch khi Local trống, HOẶC đã quá 5 phút kể từ lần lấy trang này
        if (!localResponse.Items.Any() || ShouldSyncApi($"pois?{apiQueryString}"))
        {
            // --- 2. GỌI FETCH TỪ API TRONG BACKGROUND ---
            _ = Task.Run(async () =>
            {
                BeginSync();
                try
                {
                    var http = _httpClientFactory.CreateClient("SmartTourApi");
                    var networkResponse = await http.GetFromJsonAsync<PagedResponse<Poi>>($"api/pois?{apiQueryString}");
                    if (networkResponse != null && networkResponse.Items.Any())
                    {
                        onDataUpdated?.Invoke(networkResponse);

                        try 
                        {
                            using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                            foreach (var remotePoi in networkResponse.Items)
                            {
                                remotePoi.Category = null;

                                var existingFavs = await updateDb.Favorites.Where(f => f.PoiId == remotePoi.Id).ToListAsync();

                                var existPoi = await updateDb.Pois.FirstOrDefaultAsync(p => p.Id == remotePoi.Id);
                                if (existPoi != null)
                                {
                                    updateDb.Pois.Remove(existPoi);
                                    await updateDb.SaveChangesAsync(); 
                                }
                                
                                updateDb.Pois.Add(remotePoi);
                                await updateDb.SaveChangesAsync();

                                if (existingFavs.Any())
                                {
                                    foreach (var fav in existingFavs)
                                        updateDb.Favorites.Add(new Favorite { PoiId = fav.PoiId, UserId = fav.UserId });
                                    await updateDb.SaveChangesAsync();
                                }
                            }
                        }
                        catch (Exception dbEx) { Console.WriteLine($"[BgCache] Lỗi tiến trình SQLite: {dbEx.Message}"); }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[NetworkDb] Lỗi API POI: {ex.Message}"); }
                finally { EndSync(); }
            });
        }

        return localResponse;
    }

    /// <summary>
    /// Cho trang Chi tiết (Detail) xem trực tiếp từ CSDL thiết bị rồi tải ngầm.
    /// Bao bọc trọn gói Cả Ảnh, Audio, Thông tin Khung giờ v.v...
    /// </summary>
    public async Task<Poi> GetPoiByIdAsync(int id, Action<Poi>? onDataUpdated = null)
    {
        Poi? localPoi = null;

        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            localPoi = await localDb.Pois
                .Include(p => p.Images)
                .Include(p => p.AudioFiles)
                .Include(p => p.Contents)
                .Include(p => p.Category)
                .Include(p => p.OperatingHours)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (localPoi != null) onDataUpdated?.Invoke(localPoi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BgCache] Lỗi lấy POI Offline: {ex.Message}");
        }

        if (localPoi == null || ShouldSyncApi($"poi_{id}"))
        {
            _ = Task.Run(async () =>
            {
                BeginSync();
                try
                {
                    var http = _httpClientFactory.CreateClient("SmartTourApi");
                    var networkPoi = await http.GetFromJsonAsync<Poi>($"api/pois/{id}");
                    if (networkPoi != null)
                    {
                        using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                        
                        // [Bảo vệ Dữ liệu] - Trích xuất sao lưu danh sách Yêu thích của POI này trước khi xoá (Tránh Cascade Delete)
                        var savedFavs = await updateDb.Favorites.Where(f => f.PoiId == id).ToListAsync();
                        
                        var existPoi = await updateDb.Pois.FirstOrDefaultAsync(p => p.Id == id);
                        if (existPoi != null)
                        {
                            updateDb.Pois.Remove(existPoi);
                            await updateDb.SaveChangesAsync(); 
                        }
                        
                        networkPoi.Category = null; 
                        updateDb.Pois.Add(networkPoi);
                        await updateDb.SaveChangesAsync();

                        // Phục hồi lại dữ liệu Yêu thích
                        if (savedFavs.Any())
                        {
                            foreach (var fav in savedFavs)
                                updateDb.Favorites.Add(new Favorite { PoiId = fav.PoiId, UserId = fav.UserId });
                            await updateDb.SaveChangesAsync();
                        }

                        onDataUpdated?.Invoke(networkPoi);
                    }
                }
                catch { }
                finally { EndSync(); }
            });
        }

        return localPoi ?? new Poi();
    }

    // --- CÁC HÀM CHO PROFILE VÀ FAVORITES (OFFLINE-FIRST) ---

    public async Task<UserDto> GetUserProfileAsync(int userId, Action<UserDto>? onDataUpdated = null)
    {
        UserDto localUser = null;

        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            localUser = await localDb.CachedUserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (localUser != null)
                onDataUpdated?.Invoke(localUser);
        }
        catch (Exception) { /* Lỗi query cục bộ */ }

        if (localUser == null || ShouldSyncApi($"user_{userId}"))
        {
            _ = Task.Run(async () =>
            {
                try 
                {
                    var http = _httpClientFactory.CreateClient("SmartTourApi");
                    var networkUser = await http.GetFromJsonAsync<UserDto>($"api/users/{userId}");
                    if (networkUser != null)
                    {
                        using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                        var existUser = await updateDb.CachedUserProfiles.FirstOrDefaultAsync(u => u.Id == userId);
                        if (existUser == null) updateDb.CachedUserProfiles.Add(networkUser);
                        else updateDb.Entry(existUser).CurrentValues.SetValues(networkUser);
                        
                        await updateDb.SaveChangesAsync();
                        onDataUpdated?.Invoke(networkUser);
                    }
                }
                catch (Exception) { }
            });
        }

        return localUser ?? new UserDto();
    }

    public async Task SaveUserProfileOfflineAsync(UserDto user)
    {
        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            var existUser = await localDb.CachedUserProfiles.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existUser == null) localDb.CachedUserProfiles.Add(user);
            else localDb.Entry(existUser).CurrentValues.SetValues(user);
            
            await localDb.SaveChangesAsync();
            
            // Đẩy vào hàng đợi đồng bộ PUT ngầm
            await AddToSyncQueueAsync("PUT", $"api/users/{user.Id}", user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BgCache] Lỗi Edit Profile: {ex.Message}");
        }
    }

    public async Task<List<Poi>> GetFavoritesAsync(int userId, Action<List<Poi>>? onDataUpdated = null)
    {
        List<Poi> localFavorites = new();
        try 
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            var favs = await localDb.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Poi)
                    .ThenInclude(p => p.Images)
                .AsNoTracking()
                .ToListAsync();
            localFavorites = favs.Where(f => f.Poi != null).Select(f => f.Poi!).ToList();
            if (localFavorites.Any()) onDataUpdated?.Invoke(localFavorites);
        }
        catch (Exception) { }

        if (!localFavorites.Any() || ShouldSyncApi($"favorites_{userId}"))
        {
            _ = Task.Run(async () =>
            {
                BeginSync();
                try
                {
                    var http = _httpClientFactory.CreateClient("SmartTourApi");
                    var networkFavorites = await http.GetFromJsonAsync<List<Poi>>("api/favorites");
                    if (networkFavorites != null)
                    {
                        // 1. NGAY LẬP TỨC cập nhật UI khi có mạng, bỏ qua các thao tác DB nặng!
                        onDataUpdated?.Invoke(networkFavorites);

                        // 2. Chạy lưu ngầm xuống DB cục bộ (Bọc Try/Catch kín để không sập app)
                        try 
                        {
                            using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                            
                            await updateDb.Database.ExecuteSqlRawAsync("DELETE FROM Favorites WHERE UserId = {0}", userId);
                            
                            // Gỡ bỏ các navigation property rườm rà trước khi Cache để tránh Data Conflict trên Mobile DB
                            foreach (var remotePoi in networkFavorites)
                            {
                                await updateDb.Database.ExecuteSqlRawAsync(
                                    "INSERT OR IGNORE INTO Favorites (UserId, PoiId, CreatedAt) VALUES ({0}, {1}, {2})",
                                    userId, remotePoi.Id, DateTime.UtcNow);

                                remotePoi.Category = null;
                                remotePoi.Images = null;
                                remotePoi.AudioFiles = null;
                                remotePoi.Contents = null;
                                remotePoi.OperatingHours = null;

                                var existPoi = await updateDb.Pois.FirstOrDefaultAsync(p => p.Id == remotePoi.Id);
                                if (existPoi == null)
                                    updateDb.Pois.Add(remotePoi);
                                else
                                    updateDb.Entry(existPoi).CurrentValues.SetValues(remotePoi);
                            }
                            await updateDb.SaveChangesAsync();
                        }
                        catch (Exception dbEx)
                        {
                            Console.WriteLine($"[GetFavs] Cảnh báo lỗi cập nhật bộ đệm SQLite: {dbEx.Message}");
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[GetFavs] Lỗi sync: {ex.Message}"); }
                finally { EndSync(); }
            });
        }

        return localFavorites;
    }

    // --- CHỨC NĂNG ĐẨY NGƯỢC (BACKGROUND SYNC / POST DATA KHI RỚT MẠNG) ---

    public async Task AddToSyncQueueAsync(string actionType, string apiEndpoint, object payload = null)
    {
        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            localDb.PendingActions.Add(new PendingSyncAction
            {
                ActionType = actionType,
                // Ta có thể serialize payload sang chuỗi nếu cần Body/JSON cho PUT/POST
                Payload = System.Text.Json.JsonSerializer.Serialize(new { Endpoint = apiEndpoint, Body = payload })
            });
            await localDb.SaveChangesAsync();
            Console.WriteLine($"[SyncQueue] Cất lệnh '{actionType}' vào SQLite vì đang mất mạng.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi thêm Queue: {ex.Message}");
        }
    }

    public async Task ProcessPendingActionsAsync()
    {
        await _syncLock.WaitAsync();

        try
        {
            using var db = await _dbContextFactory.CreateDbContextAsync();
            var pendingQueue = await db.PendingActions.OrderBy(a => a.CreatedAt).ToListAsync();

            if (!pendingQueue.Any()) return;

            var http = _httpClientFactory.CreateClient("SmartTourApi");

            foreach (var job in pendingQueue)
            {
                try
                {
                    var data = System.Text.Json.JsonSerializer.Deserialize<SyncJobFormat>(job.Payload);
                    if (data == null || string.IsNullOrEmpty(data.Endpoint)) 
                    {
                        db.PendingActions.Remove(job);
                        continue;
                    }

                    HttpResponseMessage? response = null;

                    if (job.ActionType == "POST")
                    {
                        response = await http.PostAsync(data.Endpoint, null);
                    }
                    else if (job.ActionType == "DELETE")
                    {
                        response = await http.DeleteAsync(data.Endpoint);
                    }
                    else if (job.ActionType == "PUT")
                    {
                        response = await http.PutAsJsonAsync(data.Endpoint, data.Body);
                    }

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        db.PendingActions.Remove(job);
                        Console.WriteLine($"[BgSync] '{job.ActionType}' {data.Endpoint} thành công!");
                    }
                    else if (response != null && (int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        db.PendingActions.Remove(job);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    break; 
                }
            }
            await db.SaveChangesAsync();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Toggle Yêu thích - API thẳng nếu có mạng → cập nhật SQLite theo kết quả thực
    /// </summary>
    public async Task<bool> ToggleFavoriteAsync(int poiId, int userId)
    {
        bool newState;

        if (Microsoft.Maui.Networking.Connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            // === CÓ MẠNG: Gọi API → Server là nguồn sự thật ===
            try
            {
                var http = _httpClientFactory.CreateClient("SmartTourApi");
                var response = await http.PostAsync($"api/favorites/toggle/{poiId}", null);
                if (response.IsSuccessStatusCode)
                {
                    // Hỏi lại Server trạng thái thực (tránh toggle chạy 2 lần)
                    newState = await http.GetFromJsonAsync<bool>($"api/favorites/check/{poiId}");

                    // Ghi SQLite bằng SQL thuần – tránh EF FK tracking conflict
                    await SyncLocalFavoriteAsync(poiId, userId, newState);
                    return newState;
                }
            }
            catch { }
        }

        // === OFFLINE / API Lỗi: Đọc trạng thái cục bộ → Toggle ngược → Queue ===
        using var db = await _dbContextFactory.CreateDbContextAsync();
        bool currentState = await db.Favorites.AnyAsync(f => f.PoiId == poiId && f.UserId == userId);
        newState = !currentState;

        await SyncLocalFavoriteAsync(poiId, userId, newState);
        await AddToSyncQueueAsync("POST", $"api/favorites/toggle/{poiId}");
        return newState;
    }

    /// <summary>
    /// Ghi thẳng vào SQLite bằng SQL thuần – bỏ qua EF tracking để tránh lỗi FK cascade
    /// </summary>
    private async Task SyncLocalFavoriteAsync(int poiId, int userId, bool shouldExist)
    {
        try
        {
            using var db = await _dbContextFactory.CreateDbContextAsync();
            bool exists = await db.Favorites.AnyAsync(f => f.PoiId == poiId && f.UserId == userId);

            if (shouldExist && !exists)
            {
                // INSERT trực tiếp, bỏ qua EF navigation property
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Favorites (UserId, PoiId, CreatedAt) VALUES ({0}, {1}, {2})",
                    userId, poiId, DateTime.UtcNow);
            }
            else if (!shouldExist && exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Favorites WHERE PoiId = {0} AND UserId = {1}",
                    poiId, userId);
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Fav] SyncLocal error: {ex.Message}"); }
    }

    /// <summary>
    /// Kiểm tra Yêu thích - Ưu tiên hỏi Server nếu có mạng, còn không thì đọc SQLite
    /// </summary>
    public async Task<bool> CheckFavoriteAsync(int poiId, int userId)
    {
        if (Microsoft.Maui.Networking.Connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            try
            {
                var http = _httpClientFactory.CreateClient("SmartTourApi");
                var isServerFav = await http.GetFromJsonAsync<bool>($"api/favorites/check/{poiId}");
                // Đồng bộ SQLite trạng thái thực tế từ Server xuống
                await SyncLocalFavoriteAsync(poiId, userId, isServerFav);
                return isServerFav;
            }
            catch { }
        }

        // Fallback Offline
        try
        {
            using var localDb = await _dbContextFactory.CreateDbContextAsync();
            return await localDb.Favorites.AnyAsync(f => f.PoiId == poiId && f.UserId == userId);
        }
        catch { return false; }
    }

    // --- TÍNH NĂNG 1: TẢI TRỌN GÓI OFFLINE (DOWNLOAD OFFLINE PACK) ---

    public class SyncProgress
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public int Percentage => TotalItems > 0 ? (int)((double)ProcessedItems / TotalItems * 100) : 0;
    }

    public class OfflinePackInfo
    {
        public int TotalPois { get; set; }
        public int TotalImages { get; set; }
        public int TotalAudio { get; set; }
        public int TotalMapTiles { get; set; }
        public double EstimatedPoiMegabytes => TotalPois * 0.005;    // Ước lượng 5KB JSON text mỗi địa điểm
        public double EstimatedMapMegabytes => TotalMapTiles * 0.02; // Ước lượng 20KB mỗi tile bản đồ 
        public double EstimatedImageMegabytes => TotalImages * 0.4;  // Ước lượng 400KB mỗi ảnh HD
        public double EstimatedAudioMegabytes => TotalAudio * 2.5;   // Ước lượng 2.5MB mỗi file MP3
        public double EstimatedMegabytes => EstimatedPoiMegabytes + EstimatedImageMegabytes + EstimatedAudioMegabytes + EstimatedMapMegabytes; 
        
        public HashSet<string> RequiredMediaUrls { get; set; } = new();
        
        // Bounding Box (Khung chữ nhật bao quanh toàn bộ POI)
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLng { get; set; }
        public double MaxLng { get; set; }
    }

    /// <summary>
    /// Bước 1: Quét nhanh qua Data API bằng JSON (Rất nhẹ) để đếm số lượng tài nguyên và ƯỚC TÍNH DUNG LƯỢNG MB.
    /// </summary>
    public async Task<OfflinePackInfo> PrepareOfflinePackAsync(IProgress<SyncProgress>? progress = null)
    {
        var proc = new SyncProgress();
        proc.CurrentStatus = "Đang quét hệ thống địa điểm...";
        progress?.Report(proc);

        // Lấy toàn bộ Categories lưu đệm
        await GetCategoriesAsync();

        // Tải các POIs từ Page 1 tới hết
        proc.CurrentStatus = "Chuẩn bị tải hàng trăm địa điểm (POIs)...";
        progress?.Report(proc);

        int pageNum = 1;
        bool hasMorePois = true;
        List<Poi> allPoisFlattened = new();
        var http = _httpClientFactory.CreateClient("SmartTourApi");

        while (hasMorePois)
        {
            // Ép buộc kết nối thẳng lên Server thay vì xài hàm GetPoisAsync (Bị dính luồng ngầm)
            var pagedResult = await http.GetFromJsonAsync<PagedResponse<Poi>>($"api/pois?pageNumber={pageNum}&pageSize=100");
            
            if (pagedResult != null && pagedResult.Items.Any())
            {
                allPoisFlattened.AddRange(pagedResult.Items);

                // Lưu cứng toàn bộ vào CSDL Cục bộ để có thông tin khung POI phục vụ gói Offline
                using var updateDb = await _dbContextFactory.CreateDbContextAsync();
                foreach (var remotePoi in pagedResult.Items)
                {
                    remotePoi.Category = null;

                    var existingFavs = await updateDb.Favorites.Where(f => f.PoiId == remotePoi.Id).ToListAsync();

                    var existPoi = await updateDb.Pois.FirstOrDefaultAsync(p => p.Id == remotePoi.Id);
                    if (existPoi != null)
                    {
                        updateDb.Pois.Remove(existPoi);
                        await updateDb.SaveChangesAsync();
                    }
                    
                    updateDb.Pois.Add(remotePoi);
                    await updateDb.SaveChangesAsync();

                    if (existingFavs.Any())
                    {
                        foreach (var fav in existingFavs)
                            updateDb.Favorites.Add(new Favorite { PoiId = fav.PoiId, UserId = fav.UserId });
                        await updateDb.SaveChangesAsync();
                    }
                }

                if (allPoisFlattened.Count >= pagedResult.TotalCount)
                    hasMorePois = false;
                else
                    pageNum++;
            }
            else
            {
                hasMorePois = false;
            }
        }

        proc.CurrentStatus = "Đã lưu DB thành công. Bắt đầu phân tích Ảnh/ m Thanh...";
        progress?.Report(proc);

        int imgCount = 0;
        int audioCount = 0;
        var requiredMediaUrls = new HashSet<string>();
        foreach (var poi in allPoisFlattened)
        {
            foreach (var img in poi.Images) 
            {
                if (!string.IsNullOrEmpty(img.ImageUrl)) { requiredMediaUrls.Add(img.ImageUrl); imgCount++; }
            }
            foreach (var audio in poi.AudioFiles) 
            {
                if (!string.IsNullOrEmpty(audio.FileUrl)) { requiredMediaUrls.Add(audio.FileUrl); audioCount++; }
            }
        }

        proc.CurrentStatus = "Hoàn tất quét dung lượng!";
        progress?.Report(proc);

        // Tính Bounding Box (Khu vực tối đa bao phủ các POI) + Mở rộng lề (Padding ~5km)
        double minLat = default, maxLat = default, minLng = default, maxLng = default;
        int totalMapTiles = 0;

        if (allPoisFlattened.Any())
        {
            minLat = allPoisFlattened.Min(p => p.Latitude) - 0.05;
            maxLat = allPoisFlattened.Max(p => p.Latitude) + 0.05;
            minLng = allPoisFlattened.Min(p => p.Longitude) - 0.05;
            maxLng = allPoisFlattened.Max(p => p.Longitude) + 0.05;

            // Tính số lượng Map Tiles để ước tính dung lượng MB
            for (int zoom = 10; zoom <= 15; zoom++)
            {
                var tileTL = LatLngToTile(maxLat, minLng, zoom);
                var tileBR = LatLngToTile(minLat, maxLng, zoom);
                int width = Math.Abs(tileBR.x - tileTL.x) + 1;
                int height = Math.Abs(tileBR.y - tileTL.y) + 1;
                totalMapTiles += (width * height);
            }
        }

        return new OfflinePackInfo
        {
            TotalPois = allPoisFlattened.Count,
            TotalImages = imgCount,
            TotalAudio = audioCount,
            TotalMapTiles = totalMapTiles,
            RequiredMediaUrls = requiredMediaUrls,
            MinLat = minLat,
            MaxLat = maxLat,
            MinLng = minLng,
            MaxLng = maxLng
        };
    }

    /// <summary>
    /// Bước 2: Nhận vào Gói dung lượng đã quét để Bắt đầu Cày File vật lý xuống màng cứng thiết bị.
    /// </summary>
    public async Task DownloadMediaPackAsync(OfflinePackInfo pack, IProgress<SyncProgress>? progress = null)
    {
        var proc = new SyncProgress
        {
            TotalItems = pack.RequiredMediaUrls.Count,
            ProcessedItems = 0,
            CurrentStatus = $"Bắt đầu Download {pack.RequiredMediaUrls.Count} file Media..."
        };
        progress?.Report(proc);

        int count = 0;
        foreach (var mediaUrl in pack.RequiredMediaUrls)
        {
            await _mediaCache.GetCachedMediaUrlAsync(mediaUrl);

            count++;
            proc.ProcessedItems = count;
            
            if (count % 3 == 0 || count == proc.TotalItems)
            {
                proc.CurrentStatus = $"Đang tải Media... {count}/{proc.TotalItems}";
                progress?.Report(proc);
            }
        }

        proc.CurrentStatus = "Hoàn tất Tải Toàn Bộ Gói Du Lịch Offline!";
        progress?.Report(proc);
    }

    /// <summary>
    /// Chuyển đổi Toạ độ địa lý (Lat/Lng) sang Hệ toạ độ Mảnh bản đồ (Slippy map tilenames) 
    /// dùng để đếm số lượng hình ảnh Map sẽ tải.
    /// </summary>
    private (int x, int y) LatLngToTile(double lat, double lng, int zoom)
    {
        double n = Math.Pow(2, zoom);
        int x = (int)((lng + 180.0) / 360.0 * n);
        double latRad = lat * Math.PI / 180.0;
        int y = (int)((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
        return (Math.Max(0, x), Math.Max(0, y));
    }
}

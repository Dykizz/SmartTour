using System.Security.Cryptography;
using System.Text;

namespace SmartTour.Mobile.Services;

/// <summary>
/// Service chuẩn Best Practice để tự động Tải, Lưu và Truy Xuất Media ( m thanh / Hình ảnh) Offline.
/// SQLite chỉ xử lý được chuỗi và số, nên ta dùng FileSystem riêng của thiết bị để chứa file nhị phân.
/// </summary>
public class MediaCacheService
{
    private readonly HttpClient _http;
    private readonly string _cacheDirectory;

    public MediaCacheService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("SmartTourApi"); // Dùng Client để tận dụng Config nếu cần
        _cacheDirectory = FileSystem.CacheDirectory; // Thư mục an toàn trên OS để chứa Cache
    }

    /// <summary>
    /// Hàm cung cấp đường dẫn Local an toàn cho trình duyệt/BlazorWebView.
    /// Nó sẽ tải Background từ Remote URL về CacheDirectory nếu chưa từng download.
    /// Nếu đã từng download (có file) -> lập tức ném ra đường dẫn Local offline.
    /// </summary>
    public async Task<string> GetCachedMediaUrlAsync(string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl) || !remoteUrl.StartsWith("http")) 
            return remoteUrl;

        // Băm MD5/SHA chuỗi URL để rút ra Filename duy nhất tránh trùng lặp
        var extension = Path.GetExtension(new Uri(remoteUrl).AbsolutePath);
        if (string.IsNullOrEmpty(extension)) extension = ".dat"; // fallback
        var filename = GetHashString(remoteUrl) + extension;
        var localFilePath = Path.Combine(_cacheDirectory, filename);

        // 1. NẾU CÓ SẴN OFFLINE -> Trả về Local Path để dùng luôn
        if (File.Exists(localFilePath))
        {
            // Tùy theo OS (Android/iOS/Windows) mà Blazor WebView cho phép đọc file:/// hay không.
            // Có thể cần map "https://appdomain/cache/filename" nhưng dùng custom local uri là chuẩn nhất cho Audio.
            // Tạm thời trả physical app path để App hoặc Audio Player gốc (như JS Audio) có thể đọc.
            return localFilePath;
        }

        // 2. NẾU CHƯA CÓ TRONG OFFINE -> Tải về và Cached
        try
        {
            var fileBytes = await _http.GetByteArrayAsync(remoteUrl);
            await File.WriteAllBytesAsync(localFilePath, fileBytes);
            
            return localFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MediaCache] Lỗi khi tải Media Offline ({remoteUrl}): {ex.Message}");
            // Rớt mạng hoặc Lỗi, tạm fall back trả về URL gốc trên mạng
            return remoteUrl;
        }
    }

    private string GetHashString(string inputString)
    {
        using var algorithm = SHA256.Create();
        var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Hàm chốt hạ: Biến File vật lý trên đĩa cứng đứt mạng thành đường dẫn tĩnh chuẩn Base64.
    /// Để vượt rào chặn quyền truy cập Local (file:///) của các trình duyệt Webview trên màn hình App.
    /// Giúp thẻ img và audio phát thẳng băng không cần gọi mạng!
    /// </summary>
    public async Task<string> GetLocalMediaDataUriAsync(string remoteUrl)
    {
        // Nhờ hàm kia tìm/tải và lấy đường dẫn File 
        var localPath = await GetCachedMediaUrlAsync(remoteUrl);
        
        // Nếu nó tìm không ra file tĩnh, vứt link mạng cứu nét cho UI load sống
        if (localPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return remoteUrl;

        try
        {
            var bytes = await File.ReadAllBytesAsync(localPath);
            string extension = Path.GetExtension(localPath).ToLower();
            string mimeType = extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".png" => "image/png",
                _ => "image/jpeg"
            };

            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MediaCache] Lỗi đọc Base64 File Offline: {ex.Message}");
            return remoteUrl; // Lỗi đĩa thì fallback về mạng!
        }
    }

    // --- TÍNH NĂNG 2: THU DỌN RÁC BỘ NHỚ CACHE ĐIỆN THOẠI ---

    /// <summary>
    /// Xóa các tệp Media m thanh/Hình ảnh đã tải quá cũ (Không truy cập đến hơn số ngày chỉ định) 
    /// nhằm giải phóng không gian ổ cứng cho điện thoại. (Nên gọi ngầm ở lúc rảnh hoặc khi khởi động App 1 lần / tuần).
    /// </summary>
    public Task CleanupOldCacheAsync(int daysToKeep = 30)
    {
        return Task.Run(() =>
        {
            try
            {
                var directory = new DirectoryInfo(_cacheDirectory);
                if (!directory.Exists) return;

                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var files = directory.GetFiles();

                int deletedCount = 0;
                foreach (var file in files)
                {
                    // Quét kiểm tra lịch sử truy cập (LRU Pattern - file nào lâu chưa đụng tới thì xóa)
                    if (file.LastAccessTimeUtc < cutoffDate)
                    {
                        file.Delete();
                        deletedCount++;
                    }
                }
                
                if (deletedCount > 0)
                    Console.WriteLine($"[MediaCache] Hoàn tất Cache Cleanup. Đã dọn dẹp {deletedCount} tệp cũ hơn {daysToKeep} ngày.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaCache] Lỗi Cleanup: {ex.Message}");
            }
        });
    }
}

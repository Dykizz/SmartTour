namespace SmartTour.Shared.Models;

/// <summary>
/// DTO nhẹ chỉ chứa thông tin cần thiết để kiểm tra Geofence trên Mobile.
/// Không bao gồm Images, Contents, OperatingHours để giảm tải mạng.
/// </summary>
public class PoiGeofenceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Bán kính vùng Geofence tính bằng mét (lấy từ cột GeofenceRadius trong DB).
    /// Fallback: 50m nếu giá trị = 0.
    /// </summary>
    public double GeofenceRadius { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public List<PoiAudioFile> AudioFiles { get; set; } = new();
}

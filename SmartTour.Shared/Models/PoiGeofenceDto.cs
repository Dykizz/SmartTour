namespace SmartTour.Shared.Models;

/// <summary>
/// DTO nhẹ chỉ chứa thông tin cần thiết để kiểm tra Geofence trên Mobile.
/// </summary>
public class PoiGeofenceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Bán kính vùng Geofence tính bằng mét.
    /// </summary>
    public double GeofenceRadius { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? QrValue { get; set; }

    // Advanced filtering fields
    public bool IsFeature { get; set; }
    public List<OperatingHours> OperatingHours { get; set; } = new();

    public List<PoiAudioFile> AudioFiles { get; set; } = new();
}


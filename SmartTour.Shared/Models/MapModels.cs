namespace SmartTour.Shared.Models;

/// <summary>
/// Kết quả tìm kiếm địa điểm từ Map Provider
/// </summary>
public class MapPrediction
{
    public string description { get; set; } = "";
    public string place_id { get; set; } = "";
}

/// <summary>
/// Chi tiết địa điểm từ Map Provider
/// </summary>
public class MapPlaceDetails
{
    public double lat { get; set; }
    public double lng { get; set; }
    public string name { get; set; } = "";
    public string address { get; set; } = "";
}

/// <summary>
/// Cấu hình cho Map Provider
/// </summary>
public class MapProviderConfig
{
    public string ProviderName { get; set; } = "Mapbox"; // Mapbox, Google
    public string ApiKey { get; set; } = "";
}

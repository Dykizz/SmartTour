namespace SmartTour.Shared.Models;

public enum PoiPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public class Poi
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tọa độ Vĩ độ (Latitude)
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Tọa độ Kinh độ (Longitude)
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Bán kính kích hoạt (mét) để bắt đầu phát nội dung khi người dùng đến gần
    /// </summary>
    public double ActivationRadius { get; set; }

    /// <summary>
    /// Mức độ ưu tiên của điểm POI này
    /// </summary>
    public PoiPriority Priority { get; set; } = PoiPriority.Medium;

    /// <summary>
    /// Đường dẫn tới hình ảnh đại diện của POI
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Đường dẫn tới file âm thanh thuyết minh (mp3, wav,...)
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Nội dung văn bản dùng cho Text-To-Speech (TTS)
    /// </summary>
    public string? TtsScript { get; set; }

    /// <summary>
    /// Thời gian cập nhật cuối cùng
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

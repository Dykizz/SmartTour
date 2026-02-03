using Microsoft.JSInterop;
using SmartTour.Shared.Models;

namespace SmartTour.Web.Services;

/// <summary>
/// Interface chuẩn cho tất cả Map Provider
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Khởi tạo bản đồ
    /// </summary>
    Task InitMapAsync<T>(double lat, double lng, string elementId, DotNetObjectReference<T> dotNetHelper) where T : class;
    
    /// <summary>
    /// Di chuyển marker đến vị trí mới
    /// </summary>
    Task SetMarkerAsync(double lat, double lng, string elementId);
    
    /// <summary>
    /// Resize bản đồ (dùng khi thay đổi kích thước container)
    /// </summary>
    Task InvalidateSizeAsync(string elementId);
    
    /// <summary>
    /// Tìm kiếm địa điểm (autocomplete)
    /// </summary>
    Task<List<MapPrediction>> GetPredictionsAsync(string input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lấy chi tiết địa điểm từ place_id
    /// </summary>
    Task<MapPlaceDetails?> GetPlaceDetailsAsync(string placeId);
    
    /// <summary>
    /// Tên provider hiện tại
    /// </summary>
    string ProviderName { get; }
}

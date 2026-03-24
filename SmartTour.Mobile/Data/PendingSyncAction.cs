using System.ComponentModel.DataAnnotations;

namespace SmartTour.Mobile.Data;

/// <summary>
/// Model đại diện cho hàng đợi đồng bộ hoá Offline (Background Sync).
/// Chứa các hành động thao tác dữ liệu (POST/PUT/DELETE) khi bị mất mạng,
/// đợi khi có mạng sẽ lấy ra và tự động đẩy lên lại Server.
/// </summary>
public class PendingSyncAction
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Loại hành động: "AddFavorite", "RemoveFavorite", "UpdateProfile"...
    public string ActionType { get; set; } = string.Empty;

    // Dữ liệu gói kèm theo để gửi lên Server (Lưu dưới dạng chuỗi JSON)
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

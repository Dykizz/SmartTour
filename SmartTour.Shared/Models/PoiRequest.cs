using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public enum RequestType
{
    Create,
    Update
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class PoiRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// null = tạo mới, có giá trị = cập nhật POI gốc với ID này
    /// </summary>
    public int? POIId { get; set; }

    [ForeignKey(nameof(POIId))]
    public Poi? OriginalPoi { get; set; }

    [Required]
    public RequestType Type { get; set; }

    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// Dữ liệu POI được serialize dạng JSON
    /// </summary>
    [Required]
    public string RequestData { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú / phản hồi từ Admin khi duyệt hoặc từ chối
    /// </summary>
    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SmartTour.Shared.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên loại hình là bắt buộc")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}

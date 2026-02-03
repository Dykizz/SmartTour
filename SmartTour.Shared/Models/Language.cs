using System.ComponentModel.DataAnnotations;

namespace SmartTour.Shared.Models;

public class Language
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã ngôn ngữ là bắt buộc (ví dụ: vi, en)")]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên ngôn ngữ là bắt buộc")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;
}

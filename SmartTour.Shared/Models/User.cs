using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Nhà cung cấp xác thực (Local, Google, ...)
    /// </summary>
    [MaxLength(50)]
    public string AuthProvider { get; set; } = "Local";

    /// <summary>
    /// ID định danh từ nhà cung cấp bên ngoài (ví dụ: Google Sub ID)
    /// </summary>
    [MaxLength(255)]
    public string? ProviderId { get; set; }
}

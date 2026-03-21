using System;

namespace SmartTour.Shared.Models;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? CurrentPackageName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarUrl { get; set; }
}

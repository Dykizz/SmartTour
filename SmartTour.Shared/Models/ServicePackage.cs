using System.ComponentModel.DataAnnotations;

namespace SmartTour.Shared.Models;

public class ServicePackage
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Mã gói không được để trống")]
    public string Code { get; set; }
    
    [Required(ErrorMessage = "Tên gói không được để trống")]
    public string Name { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }
    
    [Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 đến 3650 ngày")]
    public int DurationDays { get; set; }
    
    public string Description { get; set; }
    
    public int MaxPoiAllowed { get; set; } 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? SoftDeleteAt { get; set; }
}

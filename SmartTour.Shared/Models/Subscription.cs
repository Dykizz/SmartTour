using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; } // Mỗi User chỉ có 1 Subscription duy nhất

    [Required]
    public int PackageId { get; set; } // ID bản ghi cụ thể trong ServicePackages

    public int? LastPaymentId { get; set; } // Liên kết tới lần thanh toán gần nhất thành công

    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtPurchase { get; set; } // Lưu giá tại thời điểm mua để tính khấu trừ nâng cấp

    [Required]
    public DateTime StartDate { get; set; } // Ngày bắt đầu có hiệu lực

    [Required]
    public DateTime EndDate { get; set; } // Ngày hết hạn

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [ForeignKey(nameof(PackageId))]
    public virtual ServicePackage? ServicePackage { get; set; }
    
    [ForeignKey(nameof(LastPaymentId))]
    public virtual Payment? LastPayment { get; set; }
}

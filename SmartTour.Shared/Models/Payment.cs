using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; } // Liên kết ID người mua

    [Required]
    public string PackageCode { get; set; } // Ví dụ: 'VIP_MONTH', 'VIP_YEAR'

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Số tiền thực tế thanh toán

    public string? ExternalTransactionNo { get; set; } // Mã giao dịch từ cổng thanh toántrả về

    [Required]
    public string Status { get; set; } // 'Pending', 'Success', 'Failed'

    [Required]
    public string Type { get; set; } // 'New', 'Upgrade', 'Renew'

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property 
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}

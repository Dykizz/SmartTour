using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class OperatingHours
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PoiId { get; set; }

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan? OpenTime { get; set; } = new TimeSpan(8, 0, 0);

    [Required]
    public TimeSpan? CloseTime { get; set; } = new TimeSpan(17, 0, 0);

    public bool IsActive { get; set; } = true;
}

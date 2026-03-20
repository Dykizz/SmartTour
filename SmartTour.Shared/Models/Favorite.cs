using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class Favorite
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    public int PoiId { get; set; }

    [ForeignKey(nameof(PoiId))]
    public Poi? Poi { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

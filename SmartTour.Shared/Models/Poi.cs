using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class Poi
{
    [Key]
    public int Id { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    [Required]
    public double GeofenceRadius { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeature { get; set; } = false;

    public string? QrValue { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }

    [Required]
    public int UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))]
    public User? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<OperatingHours> OperatingHours { get; set; } = new List<OperatingHours>();
    public ICollection<PoiImage> Images { get; set; } = new List<PoiImage>();
    public ICollection<PoiContent> Contents { get; set; } = new List<PoiContent>();
    public ICollection<PoiAudioFile> AudioFiles { get; set; } = new List<PoiAudioFile>();
}

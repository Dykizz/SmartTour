using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class PoiImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PoiId { get; set; }

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    public bool IsThumbnail { get; set; } = false;
}

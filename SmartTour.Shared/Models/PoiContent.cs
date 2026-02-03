using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class PoiContent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PoiId { get; set; }

    [ForeignKey("PoiId")]
    public Poi? Poi { get; set; }

    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

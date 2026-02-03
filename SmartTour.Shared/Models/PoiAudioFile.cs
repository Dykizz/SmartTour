using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTour.Shared.Models;

public class PoiAudioFile
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

    [Required]
    public string FileUrl { get; set; } = string.Empty;

    public string? TtsScript { get; set; }

    public double DurationSeconds { get; set; }

    public int SortOrder { get; set; }

    public bool IsTts { get; set; } = true;
}

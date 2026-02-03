namespace SmartTour.Shared.Models;

public class Tour
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Poi> PointsOfInterest { get; set; } = new();
}

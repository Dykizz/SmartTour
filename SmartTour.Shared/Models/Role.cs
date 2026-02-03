namespace SmartTour.Shared.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static readonly Role Admin = new() { Id = 1, Name = "ADMIN" };
    public static readonly Role Seller = new() { Id = 2, Name = "SELLER" };
    public static readonly Role Visitor = new() { Id = 3, Name = "VISITOR" };
}

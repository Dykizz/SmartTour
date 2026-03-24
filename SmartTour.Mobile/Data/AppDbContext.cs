using Microsoft.EntityFrameworkCore;
using SmartTour.Shared.Models;

namespace SmartTour.Mobile.Data;

public class AppDbContext : DbContext
{
    // Các DB tables đại diện cho dữ liệu cần lưu offline trong app
    public DbSet<Poi> Pois { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<UserDto> CachedUserProfiles { get; set; }
    public DbSet<PoiGeofenceDto> CachedGeofences { get; set; }
    
    // Background Sync Queue (Khi rớt mạng)
    public DbSet<PendingSyncAction> PendingActions { get; set; }

    public AppDbContext()
    {
        // Tự động tạo cấu trúc CSDL nếu chưa tồn tại
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Lấy đường dẫn lưu file an toàn trên thiết bị (Android/iOS)
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "SmartTourLocal.db");
        optionsBuilder.UseSqlite($"Filename={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map cấu trúc cho Offline DTOs vì chúng không có ForeignKey ràng buộc
        modelBuilder.Entity<UserDto>().HasKey(x => x.Id);
        modelBuilder.Entity<PoiGeofenceDto>().HasKey(x => x.Id);

        // Bỏ qua một số relation phức tạp không cần lưu trữ full ở môi trường offline (để tránh lỗi Multiple Cascade)
        modelBuilder.Entity<Poi>().Ignore(p => p.CreatedBy);
        modelBuilder.Entity<Poi>().Ignore(p => p.UpdatedBy);
        // Có thể ignore thêm nếu cần thiết
    }
}

using Microsoft.EntityFrameworkCore;
using SmartTour.Shared.Models;

namespace SmartTour.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<OperatingHours> OperatingHours => Set<OperatingHours>();
    public DbSet<PoiImage> PoiImages => Set<PoiImage>();
    public DbSet<PoiContent> PoiContents => Set<PoiContent>();
    public DbSet<PoiAudioFile> PoiAudioFiles => Set<PoiAudioFile>();
    public DbSet<PoiRequest> PoiRequests => Set<PoiRequest>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Poi>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.Property(e => e.GeofenceRadius).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedBy)
                .WithMany()
                .HasForeignKey(e => e.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PoiRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestData).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OriginalPoi)
                .WithMany()
                .HasForeignKey(e => e.POIId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "ADMIN" },
            new Role { Id = 2, Name = "SELLER" },
            new Role { Id = 3, Name = "VISITOR" }
        );

        // Configuration for User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Cà phê", Description = "Các quán chuyên phục vụ cà phê và đồ uống" },
            new Category { Id = 2, Name = "Nhà hàng", Description = "Nơi phục vụ các món ăn chính, đặc sản" },
            new Category { Id = 3, Name = "Quán ăn nhanh", Description = "Đồ ăn nhanh, đồ ăn vặt" },
            new Category { Id = 4, Name = "Quán Bar/Pub", Description = "Không gian âm nhạc và đồ uống có cồn" },
            new Category { Id = 5, Name = "Khác", Description = "Các loại hình kinh doanh khác" }
        );

        // Seed Languages
        modelBuilder.Entity<Language>().HasData(
            new Language { Id = 1, Code = "vi", Name = "Tiếng Việt", IsDefault = true, IsActive = true },
            new Language { Id = 2, Code = "en", Name = "English", IsDefault = false, IsActive = true }
        );

        modelBuilder.Entity<ServicePackage>().HasData(
            new ServicePackage { Id = 1, Code = "FREE", Name = "Gói miễn phí", Price = 0, DurationDays = 365, Description = "Dành cho người dùng cá nhân phổ thông", MaxPoiAllowed = 1, CreatedAt = DateTime.UtcNow, IsActive = true },
            new ServicePackage { Id = 2, Code = "PRO_MONTH", Name = "Vĩnh Khánh Pro (Tháng)", Price = 150000, DurationDays = 30, Description = "Phù hợp cho các quán kinh doanh nhỏ", MaxPoiAllowed = 5, CreatedAt = DateTime.UtcNow, IsActive = true },
            new ServicePackage { Id = 3, Code = "VIP_YEAR", Name = "VIP Toàn Năng (Năm)", Price = 1200000, DurationDays = 365, Description = "Đầy đủ tính năng cao cấp", MaxPoiAllowed = 20, CreatedAt = DateTime.UtcNow, IsActive = true }
        );

    }
}

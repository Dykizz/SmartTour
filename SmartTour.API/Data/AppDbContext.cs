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

    }
}

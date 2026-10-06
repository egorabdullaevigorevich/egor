using ListingsApi.Data.Entities;
using Microsoft.EntityFrameworkCore;
 
namespace ListingsApi.Data;
 
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<District> Districts => Set<District>();
    public DbSet<Listing> Listings => Set<Listing>();
 
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Настройка таблицы Districts
        b.Entity<District>(e =>
        {
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(d => d.Name).IsUnique(); // Запрет дубликатов районов
        });
 
        // Настройка таблицы Listings
        b.Entity<Listing>(e =>
        {
            e.Property(l => l.Title).HasMaxLength(200).IsRequired();
            e.Property(l => l.Address).HasMaxLength(300); // Ограничение для адреса
 
            // Конверсия decimal в double для корректной сортировки в SQLite
            e.Property(l => l.Price).HasConversion<double>();
 
            e.HasOne(l => l.District)
             .WithMany(d => d.Listings)
             .HasForeignKey(l => l.DistrictId)
             .OnDelete(DeleteBehavior.Restrict); // Нельзя удалить район с объявлениями
        });
 
        // Добавление начальных районов
        b.Entity<District>().HasData(
            new District { Id = 1, Name = "Чиланзар" },
            new District { Id = 2, Name = "Юнусабад" },
            new District { Id = 3, Name = "Мирзо-Улугбек" });
 
        // Добавление начальных объявлений
        var seedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        b.Entity<Listing>().HasData(
            new Listing { Id = 1, Title = "2-комн. рядом с метро Чиланзар", Price = 48000, Rooms = 2, DistrictId = 1, Address = "ул. Мукими, 12", CreatedAt = seedDate },
            new Listing { Id = 2, Title = "3-комн. с ремонтом", Price = 71000, Rooms = 3, DistrictId = 2, Address = "квартал 4, дом 7", CreatedAt = seedDate },
            new Listing { Id = 3, Title = "1-комн. студия", Price = 32000, Rooms = 1, DistrictId = 3, Address = "пр-т Мустакиллик, 45", CreatedAt = seedDate });
    }
}

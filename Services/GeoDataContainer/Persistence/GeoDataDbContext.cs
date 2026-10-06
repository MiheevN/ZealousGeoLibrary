using Microsoft.EntityFrameworkCore;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Контекст базы данных для хранения точек нескольких глобусов и карт.
/// Используется провайдер-агностично: конкретный провайдер (SQLite, SQL Server,
/// PostgreSQL, InMemory и т.д.) настраивается при регистрации в DI.
/// </summary>
public class GeoDataDbContext : DbContext
{
    /// <summary>
    /// Создает контекст с заданными настройками
    /// </summary>
    /// <param name="options">Настройки контекста</param>
    public GeoDataDbContext(DbContextOptions<GeoDataDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Точки всех контейнеров
    /// </summary>
    public DbSet<GeoPointEntity> Points => Set<GeoPointEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<GeoPointEntity>();

        entity.ToTable("GeoPoints");

        // Составной ключ: точка с одним и тем же Id может быть в разных контейнерах.
        // Ключ начинается с ContainerId, поэтому выборка контейнера идёт по индексу ключа.
        entity.HasKey(p => new { p.ContainerId, p.Id });

        entity.Property(p => p.ContainerId).HasMaxLength(200).IsRequired();
        entity.Property(p => p.Id).HasMaxLength(GeoPoint.MaxIdLength).IsRequired();
        entity.Property(p => p.Title).HasMaxLength(GeoPoint.MaxTitleLength).IsRequired();
        entity.Property(p => p.Category).HasMaxLength(GeoPoint.MaxCategoryLength);
        entity.Property(p => p.Color).HasMaxLength(GeoPoint.MaxColorLength);
        entity.Property(p => p.Icon).HasMaxLength(GeoPoint.MaxIconLength);
        entity.Property(p => p.Url).HasMaxLength(GeoPoint.MaxUrlLength);

        entity.HasIndex(p => new { p.ContainerId, p.Category });
    }
}

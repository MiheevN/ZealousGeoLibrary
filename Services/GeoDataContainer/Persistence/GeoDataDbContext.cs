using Microsoft.EntityFrameworkCore;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Контекст базы данных для хранения гео-данных участников нескольких глобусов.
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
    /// Набор участников, хранящихся в базе данных
    /// </summary>
    public DbSet<GeoDataParticipantEntity> Participants => Set<GeoDataParticipantEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<GeoDataParticipantEntity>();

        entity.ToTable("GeoDataParticipants");

        // Составной ключ: один и тот же участник может присутствовать в разных контейнерах
        entity.HasKey(p => new { p.ContainerId, p.Id });

        // Индекс по контейнеру ускоряет выборку данных конкретного глобуса
        entity.HasIndex(p => p.ContainerId);

        entity.Property(p => p.ContainerId).HasMaxLength(200).IsRequired();
        entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
        entity.Property(p => p.Address).HasMaxLength(200).IsRequired();
        entity.Property(p => p.Email).HasMaxLength(254).IsRequired();
        entity.Property(p => p.Location).HasMaxLength(200).IsRequired();
        entity.Property(p => p.City).HasMaxLength(100);
        entity.Property(p => p.Country).HasMaxLength(100);
        entity.Property(p => p.SocialMedia).HasMaxLength(200);
        entity.Property(p => p.Message).HasMaxLength(500);
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// БД, созданная прежними версиями (таблица GeoDataParticipants), продолжает работать:
/// EnsureGeoDataDatabaseCreatedAsync создаёт таблицу точек и переносит в неё участников.
/// Прежняя БД создаётся той же моделью EF, что была в библиотеке, поэтому форматы GUID и
/// дат в SQLite настоящие.
/// </summary>
public sealed class LegacyParticipantsMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly List<ServiceProvider> _providers = new();

    public LegacyParticipantsMigrationTests()
    {
        _connection.Open();
    }

    [Fact]
    public async Task LegacyRows_MoveToPoints_AndOldTableIsKeptRenamed()
    {
        var berlinId = Guid.NewGuid();
        var registeredAt = new DateTime(2024, 5, 1, 12, 30, 0, DateTimeKind.Utc);
        SeedLegacy(
            new LegacyRow
            {
                Id = berlinId, ContainerId = "europe", Name = "Berlin", Email = "berlin@example.com",
                Address = "Unter den Linden", Location = "Berlin", Latitude = 52.52, Longitude = 13.405,
                City = "Berlin", Country = "Germany", Message = "Hallo", Telegram = "@berlin", RegisteredAt = registeredAt
            },
            new LegacyRow { Id = Guid.NewGuid(), ContainerId = "asia", Name = "Tokyo", Latitude = 35.68, Longitude = 139.69 },
            new LegacyRow { Id = Guid.NewGuid(), ContainerId = "europe", Name = "Nowhere" });

        var manager = await CreateManagerAsync();

        var europe = await manager.GetContainer("europe")!.GetAllParticipantsAsync();
        var berlin = Assert.Single(europe);
        Assert.Equal(berlinId, berlin.Id);
        Assert.Equal("Berlin", berlin.Name);
        Assert.Equal("berlin@example.com", berlin.Email);
        Assert.Equal("Unter den Linden", berlin.Address);
        Assert.Equal("Germany", berlin.Country);
        Assert.Equal("Hallo", berlin.Message);
        Assert.Equal("@berlin", berlin.SocialContacts?.Telegram);
        Assert.Equal(52.52, berlin.Latitude);
        Assert.Equal(registeredAt, berlin.RegisteredAt);
        Assert.Equal(1, manager.GetContainer("asia")!.Count);

        // Прежняя таблица переименована; строка без координат осталась в ней.
        Assert.False(TableExists(GeoDataDatabaseInitializer.LegacyParticipantsTable));
        Assert.True(TableExists(GeoDataDatabaseInitializer.MigratedParticipantsTable));
        Assert.Equal(3L, Scalar($"SELECT COUNT(*) FROM \"{GeoDataDatabaseInitializer.MigratedParticipantsTable}\""));
    }

    [Fact]
    public async Task SecondStart_DoesNotDuplicateOrRestoreDeletedPoints()
    {
        var id = Guid.NewGuid();
        SeedLegacy(new LegacyRow { Id = id, ContainerId = "europe", Name = "Paris", Latitude = 48.85, Longitude = 2.35 },
                   new LegacyRow { Id = Guid.NewGuid(), ContainerId = "europe", Name = "Rome", Latitude = 41.9, Longitude = 12.5 });
        var manager = await CreateManagerAsync();
        await manager.GetContainer("europe")!.RemoveParticipantAsync(id);

        var restarted = await CreateManagerAsync();

        var names = (await restarted.GetContainer("europe")!.GetAllParticipantsAsync()).Select(p => p.Name);
        Assert.Equal(new[] { "Rome" }, names);
    }

    [Fact]
    public async Task ExistingDatabaseWithOtherTables_GetsPointsTable()
    {
        // Приложение хранит свои таблицы в той же БД: EnsureCreated сам такую БД не трогает.
        Execute("CREATE TABLE \"AppUsers\" (\"Id\" INTEGER PRIMARY KEY, \"Login\" TEXT NOT NULL)");

        var manager = await CreateManagerAsync();
        var result = await manager.GetOrCreateContainer("offices").AddPointAsync(
            new() { Id = "hq", Latitude = 1, Longitude = 2, Title = "HQ" });

        Assert.True(result.Success);
        Assert.True(TableExists("GeoPoints"));
        Assert.True(TableExists("AppUsers"));
        Assert.False(TableExists(GeoDataDatabaseInitializer.MigratedParticipantsTable));
    }

    [Fact]
    public async Task NewDatabase_IsCreatedWithoutLegacyTables()
    {
        var manager = await CreateManagerAsync();

        Assert.Empty(manager.GetContainerIds());
        Assert.True(TableExists("GeoPoints"));
        Assert.False(TableExists(GeoDataDatabaseInitializer.LegacyParticipantsTable));
    }

    public void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
        _connection.Dispose();
    }

    private async Task<IGeoDataContainerManager> CreateManagerAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataDatabase(options => options.UseSqlite(_connection));
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);

        await provider.EnsureGeoDataDatabaseCreatedAsync();
        return provider.GetRequiredService<IGeoDataContainerManager>();
    }

    private void SeedLegacy(params LegacyRow[] rows)
    {
        using var context = new LegacyContext(new DbContextOptionsBuilder<LegacyContext>().UseSqlite(_connection).Options);
        context.Database.EnsureCreated();
        context.Participants.AddRange(rows);
        context.SaveChanges();
    }

    private bool TableExists(string name) =>
        (long)Scalar($"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{name}'")! > 0;

    private object? Scalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    // Модель таблицы участников из прежних версий библиотеки, как она была в GeoDataDbContext.
    private sealed class LegacyContext(DbContextOptions<LegacyContext> options) : DbContext(options)
    {
        public DbSet<LegacyRow> Participants => Set<LegacyRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<LegacyRow>();
            entity.ToTable("GeoDataParticipants");
            entity.HasKey(p => new { p.ContainerId, p.Id });
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

    private sealed class LegacyRow
    {
        public Guid Id { get; set; }
        public string ContainerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? SocialMedia { get; set; }
        public string? Message { get; set; }
        public string? LifeGoals { get; set; }
        public string? Skills { get; set; }
        public string? Discord { get; set; }
        public string? Telegram { get; set; }
        public string? Vk { get; set; }
        public string? Website { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}

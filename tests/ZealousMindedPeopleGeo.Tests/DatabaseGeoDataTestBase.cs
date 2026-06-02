using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo.Tests;

/// <summary>
/// Базовый класс тестов хранилища гео-данных в БД.
/// Использует SQLite в режиме in-memory: соединение держится открытым на время теста,
/// что обеспечивает реалистичную проверку работы с реляционной базой данных без внешних зависимостей.
/// </summary>
public abstract class DatabaseGeoDataTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    /// <summary>Менеджер контейнеров гео-данных, использующий БД</summary>
    protected IGeoDataContainerManager Manager { get; }

    /// <summary>Провайдер сервисов теста</summary>
    protected IServiceProvider Services => _provider;

    protected DatabaseGeoDataTestBase()
    {
        // Открытое in-memory соединение живёт, пока открыт _connection
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataDatabase(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();
        _provider.EnsureGeoDataDatabaseCreatedAsync().GetAwaiter().GetResult();

        Manager = _provider.GetRequiredService<IGeoDataContainerManager>();
    }

    /// <summary>
    /// Создаёт тестового участника с заданными параметрами
    /// </summary>
    protected static Participant CreateParticipant(
        string name = "Test Participant",
        double? latitude = 55.7558,
        double? longitude = 37.6176,
        Guid? id = null)
    {
        return new Participant
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com",
            Address = "Test Address",
            Location = "Test Location",
            Latitude = latitude,
            Longitude = longitude
        };
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

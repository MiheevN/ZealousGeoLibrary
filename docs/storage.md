# Своё хранилище

[← Документация](README.md) · Раздел для C# / Blazor

Как хранить точки и участников в своей базе вместо памяти.

Библиотека хранит точки и участников только в памяти. Чтобы данные переживали
перезапуск или жили в базе приложения, реализуйте интерфейс и зарегистрируйте его —
компоненты, загрузка и выгрузка JSON/GeoJSON и синхронизация между пользователями
работают с ним так же, как со встроенным хранилищем.

| Интерфейс | Что хранит | Встроенная реализация |
|---|---|---|
| `IGeoDataContainerManager` + `IGeoDataContainer` | точки (`GeoPoint`) в именованных контейнерах | `GeoDataContainerManager` (в памяти, singleton) |
| `IParticipantRepository` | участников для компонентов без `DataContainerId` | `InMemoryParticipantRepository` (в памяти, scoped) |

Для контейнеров проще всего унаследовать `GeoDataContainerManagerBase`: загрузку и
выгрузку JSON и GeoJSON и рассылку `OnDataChanged` он уже умеет, остаётся управлять
контейнерами. Сам контейнер реализует `IGeoDataContainer` поверх вашей базы:

```csharp
public sealed class DbGeoDataContainerManager : GeoDataContainerManagerBase
{
    private readonly IDbContextFactory<AppDbContext> _db;

    public DbGeoDataContainerManager(IDbContextFactory<AppDbContext> db, ILogger<DbGeoDataContainerManager> logger)
        : base(logger) => _db = db;

    // Контейнер сообщает об изменениях через HandleDataChanged — так их увидят все
    // открытые глобусы и карты.
    public override IGeoDataContainer GetOrCreateContainer(string containerId) =>
        new DbGeoDataContainer(containerId, _db, HandleDataChanged);

    public override IGeoDataContainer? GetContainer(string containerId) => ContainerExists(containerId) ? GetOrCreateContainer(containerId) : null;
    public override bool ContainerExists(string containerId) { /* SELECT по ContainerId */ }
    public override bool RemoveContainer(string containerId) { /* DELETE по ContainerId */ }
    public override IEnumerable<string> GetContainerIds() { /* SELECT DISTINCT ContainerId */ }
}

// Program.cs — до или после AddZealousMindedPeopleGeo
builder.Services.AddSingleton<IGeoDataContainerManager, DbGeoDataContainerManager>();
builder.Services.AddZealousMindedPeopleGeo();
```

Что должно соблюдаться (так ведёт себя встроенное хранилище):

- контейнер хранит **копии** точек: изменения объекта после `AddPointAsync` его не трогают;
- точка с занятым `Id` не добавляется, `GeoPoint.Validate()` с ошибкой — тоже;
- `OnDataChanged` вызывается после успешного изменения и только если что-то изменилось;
- `Properties` точки — произвольные строки: удобно хранить одним JSON-столбцом,
  тогда схема таблицы не зависит от полей приложения.

Проверить своё хранилище можно готовым набором тестов: скопируйте в свой тестовый проект
`GeoDataStorageContractTests*.cs` из `tests/ZealousMindedPeopleGeo.Tests/GeoData`,
унаследуйте класс и верните свой менеджер из `Manager` (пример —
`InMemoryGeoDataStorageTests`).

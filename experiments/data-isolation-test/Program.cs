using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

// Экспериментальная проверка изоляции данных между демонстрационными представлениями.
//
// Issue #53: каждое представление витрины настраивается собственным набором данных,
// и эти данные не должны влиять на другие инстансы. Изоляция реализована через
// именованные контейнеры гео-данных (CommunityGlobeViewer.DataContainerId) и явные
// списки участников (CommunityMapComponent.Participants).
//
// Здесь мы проверяем, что:
//   1. Готовые наборы DemoDataSets возвращают свежие независимые копии (новые Id).
//   2. Загрузка наборов в разные контейнеры не приводит к смешиванию данных.
//   3. Изменение одного контейнера не влияет на другой.

var failures = new List<string>();

void Check(string name, bool condition)
{
    if (condition)
    {
        Console.WriteLine($"  ✅ {name}");
    }
    else
    {
        Console.WriteLine($"  ❌ {name}");
        failures.Add(name);
    }
}

Console.WriteLine("== DemoDataSets: свежие независимые копии ==");
var russianA = DemoDataSets.RussianCities();
var russianB = DemoDataSets.RussianCities();
Check("RussianCities не пуст", russianA.Count > 0);
Check("Два вызова дают разные экземпляры участников",
    !russianA.Select(p => p.Id).Intersect(russianB.Select(p => p.Id)).Any());
Check("Все наборы имеют уникальные ключи",
    DemoDataSets.All.Select(d => d.Key).Distinct().Count() == DemoDataSets.All.Count);
Check("FindByKey находит набор по ключу",
    DemoDataSets.FindByKey("world-capitals") is not null);

Console.WriteLine("== Изоляция между именованными контейнерами ==");
ILoggerFactory loggerFactory = NullLoggerFactory.Instance;
var manager = new GeoDataContainerManager(
    loggerFactory.CreateLogger<GeoDataContainerManager>(),
    loggerFactory);

const string russianContainer = "showcase-russia-data";
const string capitalsContainer = "showcase-capitals-data";

await manager.LoadDataAsync(russianContainer, DemoDataSets.RussianCities());
await manager.LoadDataAsync(capitalsContainer, DemoDataSets.WorldCapitals());

var russianCount = manager.GetContainer(russianContainer)!.Count;
var capitalsCount = manager.GetContainer(capitalsContainer)!.Count;

Check("Контейнер России наполнен", russianCount == DemoDataSets.RussianCities().Count);
Check("Контейнер столиц наполнен", capitalsCount == DemoDataSets.WorldCapitals().Count);

var russianData = (await manager.GetContainer(russianContainer)!.GetAllParticipantsAsync()).ToList();
var capitalsData = (await manager.GetContainer(capitalsContainer)!.GetAllParticipantsAsync()).ToList();
Check("Данные контейнеров не пересекаются по Id",
    !russianData.Select(p => p.Id).Intersect(capitalsData.Select(p => p.Id)).Any());

Console.WriteLine("== Изменение одного контейнера не влияет на другой ==");
var newPoint = new Participant
{
    Name = "Сочи",
    Address = "Сочи, Россия",
    Email = "sochi@example.com",
    Location = "Сочи, Россия",
    Latitude = 43.5855,
    Longitude = 39.7231,
};
await manager.GetOrCreateContainer(russianContainer).AddParticipantAsync(newPoint);

Check("Добавление точки увеличило только контейнер России",
    manager.GetContainer(russianContainer)!.Count == russianCount + 1);
Check("Контейнер столиц не изменился",
    manager.GetContainer(capitalsContainer)!.Count == capitalsCount);

Console.WriteLine();
if (failures.Count == 0)
{
    Console.WriteLine("ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ");
    return 0;
}

Console.WriteLine($"ПРОВАЛЕНО ПРОВЕРОК: {failures.Count}");
return 1;

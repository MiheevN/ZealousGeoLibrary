using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Участники поверх точек: хранилища держат участника как точку, и ни одно поле не теряется.
/// </summary>
public abstract partial class GeoDataStorageContractTests
{
    [Fact]
    public async Task Participant_RoundTripsEveryFieldThroughStorage()
    {
        var container = Manager.GetOrCreateContainer("people");
        var participant = new Participant
        {
            Name = "Анна",
            Address = "",
            Email = "anna@example.com",
            Location = "Казань",
            Latitude = 55.79,
            Longitude = 49.12,
            City = "",
            Country = "Россия",
            SocialMedia = null,
            Message = "Привет",
            LifeGoals = "Учиться",
            Skills = "C#, Blazor",
            SocialContacts = new SocialContacts { Telegram = "@anna", Website = "https://anna.example" },
            RegisteredAt = new DateTime(2025, 3, 14, 15, 9, 26, 535, DateTimeKind.Utc)
        };

        await container.AddParticipantAsync(participant);
        var loaded = await container.GetParticipantByIdAsync(participant.Id);

        Assert.NotNull(loaded);
        Assert.Equal(participant.Id, loaded!.Id);
        Assert.Equal("Анна", loaded.Name);
        Assert.Equal("", loaded.Address);
        Assert.Equal("anna@example.com", loaded.Email);
        Assert.Equal("Казань", loaded.Location);
        Assert.Equal(55.79, loaded.Latitude);
        Assert.Equal(49.12, loaded.Longitude);
        Assert.Equal("", loaded.City);          // пустая строка не превращается в null
        Assert.Equal("Россия", loaded.Country);
        Assert.Null(loaded.SocialMedia);
        Assert.Equal("Привет", loaded.Message);
        Assert.Equal("Учиться", loaded.LifeGoals);
        Assert.Equal("C#, Blazor", loaded.Skills);
        Assert.Equal("@anna", loaded.SocialContacts?.Telegram);
        Assert.Equal("https://anna.example", loaded.SocialContacts?.Website);
        Assert.Null(loaded.SocialContacts?.Discord);
        Assert.Equal(participant.RegisteredAt, loaded.RegisteredAt);
        Assert.Equal(DateTimeKind.Utc, loaded.RegisteredAt.Kind);
    }

    [Fact]
    public async Task Participant_IsStoredAsReadablePoint()
    {
        var container = Manager.GetOrCreateContainer("people");
        var participant = CreateParticipant("Berlin", 52.52, 13.405);
        participant.Message = "Hallo";

        await container.AddParticipantAsync(participant);

        var point = await container.GetPointAsync(participant.Id.ToString());
        Assert.NotNull(point);
        Assert.Equal("Berlin", point!.Title);
        Assert.Equal("Hallo", point.Description);
        Assert.Equal("berlin@example.com", point.Properties[ParticipantPointProperties.Email]);
    }

    [Fact]
    public async Task ParticipantWithoutCoordinates_IsRejected()
    {
        var container = Manager.GetOrCreateContainer("people");

        var single = await container.AddParticipantAsync(CreateParticipant("Nowhere", latitude: null));
        var batch = await container.AddParticipantsAsync(new[]
        {
            CreateParticipant("Somewhere"),
            CreateParticipant("Nowhere", longitude: null)
        });
        var load = await Manager.LoadDataAsync("loaded", new[] { CreateParticipant("A"), CreateParticipant("B", latitude: null) });

        Assert.False(single.Success);
        Assert.Contains("coordinates", single.ErrorMessage);
        Assert.Equal(1, batch.ProcessedCount);
        Assert.Equal(1, batch.SkippedCount);
        Assert.Equal(1, container.Count);
        Assert.Equal(1, load.ProcessedCount);
        Assert.Equal(1, load.SkippedCount);
    }

    [Fact]
    public async Task PointWithTextId_IsReachableThroughParticipantApi()
    {
        var container = Manager.GetOrCreateContainer("mixed");
        await container.AddPointAsync(new GeoPoint { Id = "sensor-17", Latitude = 1, Longitude = 2, Title = "Sensor" });

        var participant = Assert.Single(await container.GetAllParticipantsAsync());
        var sameIdAgain = Assert.Single(await container.GetAllParticipantsAsync()).Id;
        Assert.Equal(participant.Id, sameIdAgain);   // вычисленный GUID стабилен
        Assert.Equal("Sensor", (await container.GetParticipantByIdAsync(participant.Id))?.Name);

        var moved = CreateParticipant("Sensor moved", 5, 6, id: participant.Id);
        Assert.True((await container.UpdateParticipantAsync(moved)).Success);
        var point = await container.GetPointAsync("sensor-17");
        Assert.Equal("Sensor moved", point?.Title);   // точка осталась под своим Id
        Assert.Equal(5, point?.Latitude);

        Assert.True((await container.RemoveParticipantAsync(participant.Id)).Success);
        Assert.Equal(0, container.Count);
    }
}

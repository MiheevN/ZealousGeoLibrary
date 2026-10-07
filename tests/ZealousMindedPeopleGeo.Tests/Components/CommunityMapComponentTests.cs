using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Components;
using ZealousMindedPeopleGeo.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Repositories;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.Components;

/// <summary>
/// CommunityMapComponent: какие параметры уходят в community-map.js и что видит пользователь.
/// JS interop работает в строгом режиме bUnit, поэтому любой неожиданный вызов роняет тест.
/// </summary>
public class CommunityMapComponentTests : BunitContext
{
    private const string MapScript = "/_content/ZealousMindedPeopleGeo/js/community-map.js";

    private readonly ZealousMindedPeopleGeoOptions _options = new();
    private readonly GeoDataContainerManager _containers =
        new(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance);

    public CommunityMapComponentTests()
    {
        Services.AddLogging();
        Services.AddSingleton<IOptions<ZealousMindedPeopleGeoOptions>>(_ => Options.Create(_options));
        Services.AddSingleton<IParticipantRepository>(
            new InMemoryParticipantRepository(NullLogger<InMemoryParticipantRepository>.Instance));
        Services.AddSingleton<IGeoDataContainerManager>(_containers);

        JSInterop.SetupModule(MapScript);
        JSInterop.SetupVoid("setDotNetHelper", _ => true).SetVoidResult();
        JSInterop.SetupVoid("initializeCommunityMap", _ => true).SetVoidResult();
        JSInterop.SetupVoid("loadPointsOnMap", _ => true).SetVoidResult();
        JSInterop.SetupVoid("disposeCommunityMap", _ => true).SetVoidResult();
        JSInterop.SetupVoid("setCommunityMapClustering", _ => true).SetVoidResult();
    }

    [Fact]
    public void Initialize_WithoutSettings_UsesEqualEarthAndWholeWorldDefaults()
    {
        var cut = RenderMap(p => p.Add(c => c.MapId, "map-a"));

        var init = WaitForInitialization(cut);

        Assert.Equal(MapScript, Assert.Single(JSInterop.Invocations["import"]).Arguments[0]);
        Assert.Equal("", init.Arguments[0]);           // apiKey больше не используется
        Assert.Equal(20.0, init.Arguments[1]);         // широта центра
        Assert.Equal(0.0, init.Arguments[2]);          // долгота центра = центральный меридиан
        Assert.Equal(2.0, init.Arguments[3]);          // зум
        Assert.Equal("map-a", init.Arguments[4]);
        Assert.Equal("EqualEarth", Option(init, "projection"));
        Assert.Equal(0.0, Option(init, "centralMeridian"));
        Assert.Equal(true, Option(init, "clustering"));
        Assert.Equal(24, Option(init, "clusterRadius"));
    }

    [Fact]
    public void Parameters_OverrideSettings_AndCenterFollowsCentralMeridian()
    {
        var cut = RenderMap(p => p
            .Add(c => c.Projection, MapProjection.Equirectangular)
            .Add(c => c.CentralMeridian, 150.0)
            .Add(c => c.Zoom, 1.0));

        var init = WaitForInitialization(cut);

        Assert.Equal(20.0, init.Arguments[1]);
        Assert.Equal(150.0, init.Arguments[2]);
        Assert.Equal(1.0, init.Arguments[3]);
        Assert.Equal("Equirectangular", Option(init, "projection"));
        Assert.Equal(150.0, Option(init, "centralMeridian"));
    }

    [Fact]
    public void MapConfiguration_IsUsedWhenParametersAreMissing()
    {
        _options.Map = new MapConfiguration
        {
            Projection = MapProjection.Equirectangular,
            CentralMeridian = -90,
            DefaultLatitude = 10,
            DefaultLongitude = -80,
            DefaultZoom = 3
        };

        var init = WaitForInitialization(RenderMap(p => p.Add(c => c.CenterLatitude, 45.0)));

        Assert.Equal(45.0, init.Arguments[1]); // параметр важнее настройки
        Assert.Equal(-80.0, init.Arguments[2]);
        Assert.Equal(3.0, init.Arguments[3]);
        Assert.Equal("Equirectangular", Option(init, "projection"));
        Assert.Equal(-90.0, Option(init, "centralMeridian"));
    }

    [Fact]
    public void Clustering_ComesFromSettings()
    {
        _options.Map = new MapConfiguration { ClusterPoints = false, ClusterRadius = 60 };

        var init = WaitForInitialization(RenderMap(p => p.Add(c => c.MapId, "map-settings")));

        Assert.Equal(false, Option(init, "clustering"));
        Assert.Equal(60, Option(init, "clusterRadius"));
    }

    [Fact]
    public void ClusteringParameters_OverrideSettings_RadiusIsClamped()
    {
        _options.Map = new MapConfiguration { ClusterPoints = false, ClusterRadius = 60 };

        var init = WaitForInitialization(RenderMap(p => p
            .Add(c => c.ClusterPoints, true)
            .Add(c => c.ClusterRadius, 1000)));

        Assert.Equal(true, Option(init, "clustering"));
        Assert.Equal(200, Option(init, "clusterRadius"));
    }

    [Fact]
    public void ClusteringParameters_ChangeLive_WithoutRecreatingMap()
    {
        var cut = RenderMap(p => p.Add(c => c.MapId, "map-live"));
        WaitForInitialization(cut);

        // Повторная отрисовка с теми же значениями в JS ничего не отправляет.
        cut.Render(p => p.Add(c => c.Title, "Same clustering"));
        Assert.Empty(JSInterop.Invocations["setCommunityMapClustering"]);

        cut.Render(p => p.Add(c => c.ClusterPoints, false));
        cut.Render(p => p.Add(c => c.ClusterRadius, -5));

        var updates = JSInterop.Invocations["setCommunityMapClustering"];
        Assert.Equal(2, updates.Count);
        Assert.Equal(false, Value(updates[0].Arguments[0]!, "clustering"));
        Assert.Equal(24, Value(updates[0].Arguments[0]!, "clusterRadius"));
        Assert.Equal(false, Value(updates[1].Arguments[0]!, "clustering"));
        Assert.Equal(0, Value(updates[1].Arguments[0]!, "clusterRadius"));
        Assert.Equal("map-live", updates[1].Arguments[1]);
        Assert.Single(JSInterop.Invocations["initializeCommunityMap"]);
    }

    [Fact]
    public void GoogleMapsApiKey_IsNotSentToBrowser()
    {
        _options.GoogleMapsApiKey = "server-side-geocoding-key";

        var init = WaitForInitialization(RenderMap(p => p.Add(c => c.MapId, "map-key")));

        Assert.DoesNotContain(
            JSInterop.Invocations.SelectMany(invocation => invocation.Arguments),
            argument => argument?.ToString()?.Contains("server-side-geocoding-key") == true);
        Assert.Equal("", init.Arguments[0]);
    }

    [Fact]
    public void Participants_AreSentToMapAndListed()
    {
        var participants = new List<Participant>
        {
            CreateParticipant("seattle", latitude: 47.6, longitude: -122.3),
            CreateParticipant("", latitude: 1, longitude: 1),
            CreateParticipant("Nowhere", latitude: 0, longitude: 0)
        };

        var cut = RenderMap(p => p.Add(c => c.MapId, "map-b"), participants);
        WaitForInitialization(cut);

        var load = JSInterop.Invocations["loadPointsOnMap"][0];
        Assert.Equal("map-b", load.Arguments[1]);
        var sent = SentPoints(load);
        Assert.Equal(new[] { "seattle", "" }, sent.Select(p => p.GetProperty("title").GetString()));
        Assert.Equal(participants[0].Id.ToString(), sent[0].GetProperty("id").GetString());
        Assert.Equal("seattle@example.com", sent[0].GetProperty("properties").GetProperty("email").GetString());
        // Аватар — первая буква имени, для пустого имени «?». Участник с координатами 0,0
        // не найден геокодированием: его нет ни на карте, ни в списке, ни в счётчике.
        var avatars = cut.FindAll(".participant-avatar").Select(a => a.TextContent.Trim());
        Assert.Equal(new[] { "S", "?" }, avatars);
        Assert.Equal("2", cut.Find(".participants-toggle-count").TextContent.Trim());
    }

    [Fact]
    public void ParticipantsList_SitsBesideMap_AndToggleHidesIt()
    {
        var cut = RenderMap(
            p => p.Add(c => c.MapId, "map-t"),
            new List<Participant> { CreateParticipant("Berlin", latitude: 52.52, longitude: 13.405) });
        WaitForInitialization(cut);

        var toggle = cut.Find("button.participants-toggle");
        var panel = cut.Find(".participants-panel");
        Assert.Equal("map-t-participants", panel.Id);
        Assert.Equal(panel.Id, toggle.GetAttribute("aria-controls"));
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        Assert.False(panel.HasAttribute("hidden"));
        // Список — сосед холста в .map-body, а не слой поверх карты.
        Assert.Equal("map-body", panel.ParentElement!.ClassName);
        Assert.Equal("map-t", panel.ParentElement.QuerySelector(".map-canvas")!.Id);

        toggle.Click();

        Assert.Equal("false", cut.Find("button.participants-toggle").GetAttribute("aria-expanded"));
        Assert.True(cut.Find(".participants-panel").HasAttribute("hidden"));

        cut.Find("button.participants-toggle").Click();

        Assert.Equal("true", cut.Find("button.participants-toggle").GetAttribute("aria-expanded"));
        Assert.False(cut.Find(".participants-panel").HasAttribute("hidden"));
    }

    [Fact]
    public void ParticipantsListOpen_False_StartsCollapsed()
    {
        var cut = RenderMap(p => p.Add(c => c.ParticipantsListOpen, false));
        WaitForInitialization(cut);

        Assert.Equal("false", cut.Find("button.participants-toggle").GetAttribute("aria-expanded"));
        Assert.True(cut.Find(".participants-panel").HasAttribute("hidden"));
        Assert.NotNull(cut.Find(".participants-empty"));
    }

    [Fact]
    public void ShowParticipantsList_False_LeavesOnlyCount()
    {
        var cut = RenderMap(p => p.Add(c => c.ShowParticipantsList, false));
        WaitForInitialization(cut);

        Assert.Empty(cut.FindAll(".participants-toggle"));
        Assert.Empty(cut.FindAll(".participants-panel"));
        Assert.Contains("Total Participants: 0", cut.Find(".participants-count").TextContent);
    }

    [Fact]
    public async Task MarkerClick_OpensDetailsAndRaisesCallback()
    {
        var berlin = CreateParticipant("Berlin", latitude: 52.52, longitude: 13.405);
        Participant? clicked = null;
        var cut = RenderMap(
            p => p.Add(c => c.OnMarkerClick, (Participant participant) => clicked = participant),
            new List<Participant> { berlin });
        WaitForInitialization(cut);

        await cut.InvokeAsync(() => cut.Instance.OnParticipantMarkerClick(berlin.Id.ToString()));

        Assert.Equal(berlin.Id, clicked?.Id);
        Assert.Equal("Berlin", clicked?.Name);
        Assert.Equal(berlin.Email, clicked?.Email);
        Assert.Equal("Berlin", cut.Find(".participant-modal .modal-header h4").TextContent);

        cut.Find(".participant-modal .close-btn").Click();
        Assert.Empty(cut.FindAll(".participant-modal"));
    }

    [Fact]
    public async Task Dispose_ReleasesMapInJs()
    {
        WaitForInitialization(RenderMap(p => p.Add(c => c.MapId, "map-c")));

        await DisposeComponentsAsync();

        Assert.Equal("map-c", Assert.Single(JSInterop.Invocations["disposeCommunityMap"]).Arguments[0]);
    }

    [Fact]
    public async Task DisposeAsync_BeforeFirstRender_DoesNotTouchJs()
    {
        // При пререндере компонент освобождается, так и не дойдя до OnAfterRenderAsync:
        // JS в этот момент недоступен, и вызывать его нельзя.
        var component = new CommunityMapComponent();

        var error = await Record.ExceptionAsync(() => component.DisposeAsync().AsTask());

        Assert.Null(error);
    }

    // --- Точки ---------------------------------------------------------------

    [Fact]
    public void Points_AreSentWithCategoryColors_AndListed()
    {
        var points = new[]
        {
            Point("berlin", "office"), Point("tokyo", "event"), Point("rome", "office"), Point("oslo")
        };
        points[0].Properties["staff"] = "40";
        points[1].Icon = "🎤";

        var cut = Render<CommunityMapComponent>(p => p.Add(c => c.Points, points));
        WaitForInitialization(cut);

        var sent = SentPoints(JSInterop.Invocations["loadPointsOnMap"][0]);
        Assert.Equal(new[] { "berlin", "tokyo", "rome", "oslo" }, sent.Select(p => p.GetProperty("id").GetString()));
        Assert.Equal(
            new[] { GeoPointPalette.CategoryColors[0], GeoPointPalette.CategoryColors[1], GeoPointPalette.CategoryColors[0], GeoPointPalette.OtherColor },
            sent.Select(p => p.GetProperty("color").GetString()));
        Assert.Equal("40", sent[0].GetProperty("properties").GetProperty("staff").GetString());
        Assert.Equal(new[] { "B", "🎤", "R", "O" }, cut.FindAll(".participant-avatar").Select(a => a.TextContent.Trim()));
        Assert.Equal("Members", cut.Find(".participants-toggle").FirstChild!.TextContent.Trim());
    }

    [Fact]
    public void Legend_ListsCategories_AndFilterKeepsColors()
    {
        var points = new[] { Point("a", "office"), Point("b", "event"), Point("c", "event") };
        var cut = Render<CommunityMapComponent>(p => p.Add(c => c.Points, points));
        WaitForInitialization(cut);

        var items = cut.FindAll(".map-legend-item");
        Assert.Equal(new[] { "office", "event" }, items.Select(i => i.QuerySelector(".map-legend-name")!.TextContent));
        Assert.Equal(new[] { "1", "2" }, items.Select(i => i.QuerySelector(".map-legend-count")!.TextContent));
        Assert.All(items, i => Assert.Equal("true", i.GetAttribute("aria-pressed")));

        cut.FindAll(".map-legend-item")[0].Click();

        // Скрыли «office»: на карте и в списке только «event», и её цвет прежний.
        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations["loadPointsOnMap"].Count));
        var sent = SentPoints(JSInterop.Invocations["loadPointsOnMap"][1]);
        Assert.Equal(new[] { "b", "c" }, sent.Select(p => p.GetProperty("id").GetString()));
        Assert.All(sent, p => Assert.Equal(GeoPointPalette.CategoryColors[1], p.GetProperty("color").GetString()));
        Assert.Equal("false", cut.FindAll(".map-legend-item")[0].GetAttribute("aria-pressed"));
        Assert.Equal(2, cut.FindAll(".participant-card").Count);
        Assert.Equal("2", cut.Find(".participants-toggle-count").TextContent.Trim());

        cut.FindAll(".map-legend-item")[1].Click();
        Assert.Equal("All categories are hidden", cut.Find(".participants-empty").TextContent.Trim());
    }

    [Fact]
    public void Legend_IsHiddenWithoutCategories_OrWhenTurnedOff()
    {
        var plain = Render<CommunityMapComponent>(p => p.Add(c => c.Points, new[] { Point("a") }).Add(c => c.MapId, "m1"));
        var off = Render<CommunityMapComponent>(p => p
            .Add(c => c.Points, new[] { Point("a", "office") })
            .Add(c => c.ShowLegend, false)
            .Add(c => c.MapId, "m2"));

        plain.WaitForAssertion(() => Assert.Empty(plain.FindAll(".map-legend")));
        off.WaitForAssertion(() => Assert.Empty(off.FindAll(".map-legend")));
        Assert.Contains(SentPoints(JSInterop.Invocations["loadPointsOnMap"][0]),
            p => p.GetProperty("color").GetString() == GeoPointPalette.DefaultColor);
    }

    [Fact]
    public void CategoryColors_AndPointColor_OverrideAutomaticColors_UnsafeValuesIgnored()
    {
        var own = Point("own", "office");
        own.Color = "#ff00ff";
        var unsafeColor = Point("unsafe", "office");
        unsafeColor.Color = "red; background-image: url(https://evil.example)";
        var colors = new Dictionary<string, string> { ["office"] = "#ffcf5a" };

        var cut = Render<CommunityMapComponent>(p => p
            .Add(c => c.Points, new[] { own, unsafeColor })
            .Add(c => c.CategoryColors, colors));
        WaitForInitialization(cut);

        var sent = SentPoints(JSInterop.Invocations["loadPointsOnMap"][0]);
        Assert.Equal("#ff00ff", sent[0].GetProperty("color").GetString());
        Assert.Equal("#ffcf5a", sent[1].GetProperty("color").GetString());
        Assert.DoesNotContain("evil", cut.Markup);
    }

    [Fact]
    public void DataContainer_IsReadAndMapFollowsItsChanges()
    {
        var offices = _containers.GetOrCreateContainer("offices");
        offices.AddPointAsync(Point("berlin", "office")).AsTask().Wait();

        var cut = Render<CommunityMapComponent>(p => p.Add(c => c.DataContainerId, "offices"));
        WaitForInitialization(cut);
        Assert.Single(SentPoints(JSInterop.Invocations["loadPointsOnMap"][0]));

        cut.InvokeAsync(() => offices.AddPointAsync(Point("paris", "office")).AsTask()).Wait();

        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations["loadPointsOnMap"].Count));
        Assert.Equal(2, SentPoints(JSInterop.Invocations["loadPointsOnMap"][1]).Length);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".participant-card").Count));

        // Чужой контейнер карту не трогает.
        cut.InvokeAsync(() => _containers.GetOrCreateContainer("events").AddPointAsync(Point("x")).AsTask()).Wait();
        Assert.Equal(2, JSInterop.Invocations["loadPointsOnMap"].Count);
    }

    [Fact]
    public void NewPointsParameter_RefreshesMapWithoutRecreatingIt()
    {
        var cut = Render<CommunityMapComponent>(p => p.Add(c => c.Points, new[] { Point("a") }));
        WaitForInitialization(cut);

        cut.Render(p => p.Add(c => c.Points, new[] { Point("b"), Point("c") }));

        cut.WaitForAssertion(() => Assert.Equal(2, JSInterop.Invocations["loadPointsOnMap"].Count));
        Assert.Equal(2, SentPoints(JSInterop.Invocations["loadPointsOnMap"][1]).Length);
        Assert.Single(JSInterop.Invocations["initializeCommunityMap"]);
    }

    [Fact]
    public async Task PointClick_OpensDefaultCard_WithPropertiesAndSafeLinkOnly()
    {
        var hq = Point("hq", "office");
        hq.Title = "HQ";
        hq.Description = "Main office";
        hq.Url = "https://example.com/hq";
        hq.Properties["staff"] = "120";
        var bad = Point("bad");
        bad.Url = "javascript:alert(1)";
        GeoPoint? clicked = null;
        var cut = Render<CommunityMapComponent>(p => p
            .Add(c => c.Points, new[] { hq, bad })
            .Add(c => c.OnPointClick, (GeoPoint point) => clicked = point));
        WaitForInitialization(cut);

        await cut.InvokeAsync(() => cut.Instance.OnPointMarkerClick("hq"));

        Assert.Equal("hq", clicked?.Id);
        var card = cut.Find(".participant-modal");
        Assert.Equal("HQ", card.QuerySelector(".modal-header h4")!.TextContent);
        Assert.Contains("Main office", card.TextContent);
        Assert.Contains("staff:", card.TextContent);
        Assert.Equal("https://example.com/hq", card.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("noopener noreferrer", card.QuerySelector("a")!.GetAttribute("rel"));

        await cut.InvokeAsync(() => cut.Instance.OnPointMarkerClick("bad"));
        Assert.Null(cut.Find(".participant-modal").QuerySelector("a"));
        Assert.DoesNotContain("javascript:", cut.Markup);
    }

    [Fact]
    public async Task PointTemplate_ReplacesDefaultCard()
    {
        RenderFragment<GeoPoint> template = point => builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "custom-card");
            builder.AddContent(2, $"Custom {point.Title}");
            builder.CloseElement();
        };
        var cut = Render<CommunityMapComponent>(p => p
            .Add(c => c.Points, new[] { Point("hq") })
            .Add(c => c.PointTemplate, template));
        WaitForInitialization(cut);

        cut.Find(".participant-card").Click();

        Assert.Equal("Custom hq", cut.Find(".participant-modal .custom-card").TextContent);
        Assert.Empty(cut.FindAll(".participant-modal .participant-details"));
    }

    [Fact]
    public void EachMap_GetsItsOwnClickHandler()
    {
        var cut = Render<CommunityMapComponent>(p => p.Add(c => c.Points, new[] { Point("a") }));

        var init = WaitForInitialization(cut);

        Assert.IsType<DotNetObjectReference<CommunityMapComponent>>(Option(init, "dotNetHelper"));
        Assert.Empty(JSInterop.Invocations["setDotNetHelper"]);
    }

    [Fact]
    public void TitleAndListTitle_AreConfigurable()
    {
        var cut = Render<CommunityMapComponent>(p => p
            .Add(c => c.Points, new[] { Point("a") })
            .Add(c => c.Title, "Offices")
            .Add(c => c.ListTitle, "Places"));
        WaitForInitialization(cut);

        Assert.Equal("Offices", cut.Find(".map-header h3").TextContent);
        Assert.StartsWith("Places", cut.Find(".participants-toggle").TextContent.Trim());
    }

    // Явный список участников, чтобы компонент не обращался к общему репозиторию.
    private IRenderedComponent<CommunityMapComponent> RenderMap(
        Action<ComponentParameterCollectionBuilder<CommunityMapComponent>> parameters,
        List<Participant>? participants = null)
    {
        return Render<CommunityMapComponent>(p =>
        {
            p.Add(c => c.Participants, participants ?? new List<Participant>());
            parameters(p);
        });
    }

    private JSRuntimeInvocation WaitForInitialization(IRenderedComponent<CommunityMapComponent> cut)
    {
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["loadPointsOnMap"]));
        return Assert.Single(JSInterop.Invocations["initializeCommunityMap"]);
    }

    // Точки уходят в JS одной JSON-строкой.
    private static JsonElement[] SentPoints(JSRuntimeInvocation load) =>
        JsonDocument.Parse((string)load.Arguments[0]!).RootElement.EnumerateArray().ToArray();

    private static GeoPoint Point(string id, string? category = null, double latitude = 10, double longitude = 20) => new()
    {
        Id = id,
        Latitude = latitude,
        Longitude = longitude,
        Title = id,
        Category = category
    };

    // Настройки карты передаются в JS анонимным объектом { projection, centralMeridian, clustering, … }.
    private static object? Option(JSRuntimeInvocation init, string name) => Value(init.Arguments[5]!, name);

    private static object? Value(object options, string name) =>
        options.GetType().GetProperty(name)!.GetValue(options);
}

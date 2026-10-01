using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Components;
using ZealousMindedPeopleGeo.Models;
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

    public CommunityMapComponentTests()
    {
        Services.AddLogging();
        Services.AddSingleton<IOptions<ZealousMindedPeopleGeoOptions>>(_ => Options.Create(_options));
        Services.AddSingleton<IParticipantRepository>(
            new InMemoryParticipantRepository(NullLogger<InMemoryParticipantRepository>.Instance));

        JSInterop.SetupModule(MapScript);
        JSInterop.SetupVoid("setDotNetHelper", _ => true).SetVoidResult();
        JSInterop.SetupVoid("initializeCommunityMap", _ => true).SetVoidResult();
        JSInterop.SetupVoid("loadParticipantsOnMap", _ => true).SetVoidResult();
        JSInterop.SetupVoid("disposeCommunityMap", _ => true).SetVoidResult();
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
    public void Participants_AreSentToMapAndListed()
    {
        var participants = new List<Participant>
        {
            CreateParticipant("seattle", latitude: 47.6, longitude: -122.3),
            CreateParticipant("", latitude: 1, longitude: 1),
            CreateParticipant("Nowhere", latitude: 0, longitude: 0)
        };

        var cut = RenderMap(p => p.Add(c => c.MapId, "map-b"), participants);
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["loadParticipantsOnMap"]));

        var load = JSInterop.Invocations["loadParticipantsOnMap"][0];
        Assert.Contains("\"seattle\"", (string)load.Arguments[0]!);
        Assert.Equal("map-b", load.Arguments[1]);
        // Аватар — первая буква имени, для пустого имени «?»; точки с координатами 0,0 в список не попадают.
        var avatars = cut.FindAll(".participant-avatar").Select(a => a.TextContent.Trim());
        Assert.Equal(new[] { "S", "?" }, avatars);
        Assert.Contains("Total Participants: 3", cut.Markup);
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

        Assert.Same(berlin, clicked);
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
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations["loadParticipantsOnMap"]));
        return Assert.Single(JSInterop.Invocations["initializeCommunityMap"]);
    }

    // Настройки проекции передаются в JS анонимным объектом { projection, centralMeridian }.
    private static object? Option(JSRuntimeInvocation init, string name)
    {
        var options = init.Arguments[5]!;
        return options.GetType().GetProperty(name)!.GetValue(options);
    }
}

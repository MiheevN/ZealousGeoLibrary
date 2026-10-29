using System.Collections.Concurrent;
using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Components;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.Repositories;

namespace ZealousMindedPeopleGeo.Tests.Components;

/// <summary>
/// Глобус показывает изменения своего контейнера у всех, кто его открыл, без опроса.
/// Менеджер контейнеров — singleton, поэтому его событие доходит до каждой сессии.
/// </summary>
public class GlobeSynchronizationTests : BunitContext
{
    private const string ContainerId = "shared";

    private readonly GeoDataContainerManager _containers =
        new(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance);
    private readonly RecordingMediator _mediator = new();
    private readonly GlobeStateService _globeState = new();

    public GlobeSynchronizationTests()
    {
        Services.AddSingleton<IGeoDataContainerManager>(_containers);
        Services.AddSingleton<IGlobeMediator>(_mediator);
        Services.AddSingleton(_globeState);
        // Глобусы с контейнером к общему репозиторию не обращаются.
        Services.AddSingleton(DispatchProxy.Create<IParticipantRepository, GeoDataParticipantFormTests.UnusedService>());
    }

    [Fact]
    public async Task PointAddedToContainer_AppearsOnEveryGlobeShowingIt()
    {
        await Container(ContainerId).AddPointAsync(Point("berlin", 52.52, 13.40));
        RenderReadyGlobe("first", ContainerId);
        RenderReadyGlobe("second", ContainerId);

        // Точку добавляет другой пользователь — событие приходит не из потока рендеринга.
        await Task.Run(async () => await Container(ContainerId).AddPointAsync(Point("tokyo", 35.68, 139.69)));

        foreach (var globe in new[] { "globe-first", "globe-second" })
        {
            WaitForPoints(globe, "berlin", "tokyo");
        }
        Assert.Equal(2, _globeState.GetOrCreateGlobe("first").ParticipantCount);
    }

    [Fact]
    public async Task RemovedPoint_DisappearsFromTheGlobe()
    {
        await Container(ContainerId).AddPointAsync(Point("berlin", 52.52, 13.40));
        await Container(ContainerId).AddPointAsync(Point("tokyo", 35.68, 139.69));
        RenderReadyGlobe("first", ContainerId);

        await Container(ContainerId).RemovePointAsync("berlin");

        WaitForPoints("globe-first", "tokyo");
    }

    [Fact]
    public async Task ChangeInAnotherContainer_LeavesTheGlobeAlone()
    {
        RenderReadyGlobe("first", ContainerId);
        var calls = _mediator.AddPointsCalls("globe-first");

        await Container("other").AddPointAsync(Point("oslo", 59.91, 10.75));

        Assert.Equal(calls, _mediator.AddPointsCalls("globe-first"));
    }

    [Fact]
    public async Task ClosedGlobe_StopsListening()
    {
        RenderReadyGlobe("first", ContainerId);
        var calls = _mediator.AddPointsCalls("globe-first");
        Assert.Equal(1, ContainerSubscriberCount());

        await DisposeComponentsAsync();
        // bUnit может вернуть управление до Dispose, если рендерер ещё занят: ждём отписки.
        WaitForAssertion(() => Assert.Equal(0, ContainerSubscriberCount()), TimeSpan.FromSeconds(5));
        await Container(ContainerId).AddPointAsync(Point("oslo", 59.91, 10.75));

        Assert.Equal(calls, _mediator.AddPointsCalls("globe-first"));
    }

    [Fact]
    public async Task ParticipantPanel_WithContainer_AddsThePointToTheContainer()
    {
        using var english = new CultureScope("en");
        RenderReadyGlobe("first", ContainerId);
        var panel = Render<CommunityGlobeParticipantManager>(p => p
            .Add(c => c.GlobeId, "first")
            .Add(c => c.DataContainerId, ContainerId));

        panel.FindAll("input")[0].Change("Anna");
        panel.FindAll("input")[1].Change("55.75");
        panel.FindAll("input")[2].Change("37.62");
        panel.Find("button.btn-success").Click();

        panel.WaitForAssertion(() => Assert.Contains("Anna", panel.Find(".alert-success").TextContent));
        var saved = Assert.Single(await Container(ContainerId).GetPointsAsync());
        Assert.Equal("Anna", saved.Title);
        WaitForPoints("globe-first", saved.Id);
    }

    [Fact]
    public void Viewer_PassesGroupingSettingsToTheGlobe()
    {
        Render<CommunityGlobeViewer>(p => p.Add(c => c.GlobeId, "plain").Add(c => c.DataContainerId, ContainerId));
        Render<CommunityGlobeViewer>(p => p
            .Add(c => c.GlobeId, "custom")
            .Add(c => c.DataContainerId, ContainerId)
            .Add(c => c.ClusterPoints, false)
            .Add(c => c.ClusterRadius, 500));

        WaitForAssertion(() => Assert.True(_mediator.IsReady("globe-plain") && _mediator.IsReady("globe-custom")), TimeSpan.FromSeconds(5));
        Assert.True(_mediator.Options["globe-plain"].ClusterPoints);
        Assert.Equal(40, _mediator.Options["globe-plain"].ClusterRadius);
        Assert.False(_mediator.Options["globe-custom"].ClusterPoints);
        Assert.Equal(200, _mediator.Options["globe-custom"].ClusterRadius);
    }

    [Fact]
    public void Viewer_GivesTheGlobeKeyboardTextsInTheUserLanguage()
    {
        using var russian = new CultureScope("ru");
        Render<CommunityGlobeViewer>(p => p.Add(c => c.GlobeId, "ru").Add(c => c.DataContainerId, ContainerId));

        WaitForAssertion(() => Assert.True(_mediator.IsReady("globe-ru")), TimeSpan.FromSeconds(5));
        var options = _mediator.Options["globe-ru"];
        Assert.Equal("ru", options.Locale);
        Assert.NotNull(options.Texts);
        Assert.StartsWith("Стрелки вращают глобус", options.Texts["keyboardHint"]);
        Assert.Equal("{0} точек", options.Texts["clusterMany"]);
    }

    [Theory]
    [InlineData("ru", "Браузер не может показать 3D-глобус: WebGL выключен")]
    [InlineData("en", "Your browser cannot show the 3D globe: WebGL is turned off")]
    public void Viewer_WithoutWebGl_ExplainsWhyInTheUserLanguage(string language, string message)
    {
        using var culture = new CultureScope(language);
        _mediator.Failures["globe-nogl"] = GlobeInitializationError.WebGlUnavailable;

        var globe = Render<CommunityGlobeViewer>(p => p.Add(c => c.GlobeId, "nogl").Add(c => c.DataContainerId, ContainerId));

        globe.WaitForAssertion(() => Assert.StartsWith(message, globe.Find(".globe-error-message").TextContent));
        Assert.DoesNotContain("technical detail", globe.Markup);
        // Повтор не поможет, пока браузер не умеет WebGL.
        Assert.Empty(globe.FindAll(".globe-error-overlay button"));
    }

    [Fact]
    public void Viewer_ScriptsDidNotLoad_RetryStartsTheGlobe()
    {
        using var culture = new CultureScope("ru");
        _mediator.Failures["globe-retry"] = GlobeInitializationError.ScriptsUnavailable;

        var globe = Render<CommunityGlobeViewer>(p => p.Add(c => c.GlobeId, "retry").Add(c => c.DataContainerId, ContainerId));
        globe.WaitForAssertion(() => Assert.Equal(
            "Не удалось загрузить глобус. Проверьте соединение и попробуйте ещё раз.",
            globe.Find(".globe-error-message").TextContent));

        _mediator.Failures.TryRemove("globe-retry", out _);
        globe.Find(".globe-error-overlay button").Click();

        globe.WaitForAssertion(() => Assert.True(_mediator.IsReady("globe-retry")), TimeSpan.FromSeconds(5));
        globe.WaitForAssertion(() => Assert.Empty(globe.FindAll(".globe-error-overlay")));
    }

    private IGeoDataContainer Container(string id) => _containers.GetOrCreateContainer(id);

    private int ContainerSubscriberCount() =>
        (typeof(GeoDataContainerManagerBase)
            .GetField(nameof(GeoDataContainerManagerBase.OnDataChanged), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_containers) as Delegate)?.GetInvocationList().Length ?? 0;

    private IRenderedComponent<CommunityGlobeViewer> RenderReadyGlobe(string globeId, string containerId)
    {
        var globe = Render<CommunityGlobeViewer>(p => p
            .Add(c => c.GlobeId, globeId)
            .Add(c => c.DataContainerId, containerId));
        globe.WaitForAssertion(() => Assert.True(_mediator.IsReady($"globe-{globeId}")), TimeSpan.FromSeconds(5));
        return globe;
    }

    private void WaitForPoints(string globe, params string[] ids) =>
        WaitForAssertion(() => Assert.Equal(ids.OrderBy(x => x), _mediator.LastPoints(globe).OrderBy(x => x)), TimeSpan.FromSeconds(5));

    private static void WaitForAssertion(Action assertion, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                assertion();
                return;
            }
            catch when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(20);
            }
        }
    }

    private static GeoPoint Point(string id, double latitude, double longitude) =>
        new() { Id = id, Title = id, Latitude = latitude, Longitude = longitude };

    /// <summary>
    /// Глобус без JS: сразу «готов» и запоминает, какие точки ему отдали.
    /// </summary>
    private sealed class RecordingMediator : IGlobeMediator
    {
        private readonly ConcurrentDictionary<string, List<string[]>> _points = new();
        private readonly ConcurrentDictionary<string, bool> _ready = new();
        private readonly ConcurrentDictionary<string, Func<GlobeState, Task>> _callbacks = new();

        public ConcurrentDictionary<string, GlobeOptions> Options { get; } = new();

        /// <summary>Глобусы, которые не запустятся, и почему.</summary>
        public ConcurrentDictionary<string, GlobeInitializationError> Failures { get; } = new();

        public bool IsReady(string containerId) => _ready.ContainsKey(containerId);

        public int AddPointsCalls(string containerId) => _points.TryGetValue(containerId, out var calls) ? calls.Count : 0;

        public string[] LastPoints(string containerId) =>
            _points.TryGetValue(containerId, out var calls) && calls.Count > 0 ? calls[^1] : Array.Empty<string>();

        public Task<GlobeOperationResult> SetReadyCallbackAsync(string containerId, Func<GlobeState, Task> callback)
        {
            _callbacks[containerId] = callback;
            return Ok();
        }

        public async Task<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options)
        {
            Options[containerId] = options;
            if (Failures.TryGetValue(containerId, out var failure))
            {
                return new GlobeInitializationResult { Success = false, Error = failure, ErrorMessage = "technical detail" };
            }
            if (_callbacks.TryGetValue(containerId, out var callback))
            {
                await callback(new GlobeState { IsInitialized = true });
            }
            _ready[containerId] = true;
            return new GlobeInitializationResult { Success = true };
        }

        public Task<GlobeOperationResult> AddPointsAsync(string containerId, IEnumerable<GeoPoint> points, IReadOnlyDictionary<string, string>? categoryColors = null)
        {
            var list = _points.GetOrAdd(containerId, _ => new List<string[]>());
            lock (list)
            {
                list.Add(points.Select(p => p.Id).ToArray());
            }
            return Ok();
        }

        public Task<GlobeOperationResult> AddParticipantAsync(string containerId, Participant participant) => Ok();
        public Task<GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Participant> participants) => Ok();
        public Task<GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId) => Ok();
        public Task<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0) => Ok();
        public Task<GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod) => Ok();
        public Task<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5) => Ok();
        public Task<GlobeState> GetStateAsync(string containerId) => Task.FromResult(new GlobeState { IsInitialized = true });
        public Task<GlobeOperationResult> ClearAsync(string containerId) => Ok();
        public Task<GlobeOperationResult> DisposeAsync(string containerId) => Ok();
        public Task<bool> IsAvailableAsync() => Task.FromResult(true);
        public Task<bool> IsGlobeAvailableAsync(string containerId) => Task.FromResult(true);

        private static Task<GlobeOperationResult> Ok() => Task.FromResult(new GlobeOperationResult { Success = true });
    }
}

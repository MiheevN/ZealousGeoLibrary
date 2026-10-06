using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text.Json;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo.Components;

/// <summary>
/// 2D-карта точек: участников сообщества или любых других (<see cref="GeoPoint"/>).
/// Точки берутся из первого заданного источника: <see cref="Points"/>,
/// <see cref="Participants"/>, контейнер <see cref="DataContainerId"/>, общий
/// репозиторий участников.
/// </summary>
public partial class CommunityMapComponent : IAsyncDisposable
{
    [Parameter] public string MapId { get; set; } = "map";
    [Parameter] public string Height { get; set; } = "500px";

    /// <summary>Заголовок в шапке карты.</summary>
    [Parameter] public string Title { get; set; } = "Community Map";

    /// <summary>Подпись кнопки, которая открывает и закрывает список точек.</summary>
    [Parameter] public string ListTitle { get; set; } = "Members";

    /// <summary>Показывать ли список точек рядом с картой.</summary>
    [Parameter] public bool ShowParticipantsList { get; set; } = true;

    /// <summary>
    /// Открыт ли список точек при первой отрисовке. Дальше его открывает и
    /// закрывает кнопка в шапке карты.
    /// </summary>
    [Parameter] public bool ParticipantsListOpen { get; set; } = true;

    /// <summary>
    /// Показывать ли легенду категорий над картой. Легенда появляется, только если у
    /// точек есть категории; щелчок по категории скрывает и показывает её точки.
    /// </summary>
    [Parameter] public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Точки для карты. Если задан, важнее всех остальных источников.
    /// </summary>
    [Parameter] public IEnumerable<GeoPoint>? Points { get; set; }

    /// <summary>
    /// Необязательный явный набор участников для отображения на карте.
    /// Если задан, карта использует именно его вместо общего
    /// <see cref="Services.Repositories.IParticipantRepository"/>. Это позволяет
    /// нескольким картам показывать независимые, изолированные данные.
    /// </summary>
    [Parameter] public IEnumerable<Participant>? Participants { get; set; }

    /// <summary>
    /// Контейнер гео-данных, из которого карта берёт точки, если не заданы
    /// <see cref="Points"/> и <see cref="Participants"/>. Карта обновляется сама,
    /// когда данные контейнера меняются.
    /// </summary>
    [Parameter] public string? DataContainerId { get; set; }

    /// <summary>
    /// Цвета категорий (название → цвет CSS). Важнее автоматических цветов
    /// <see cref="GeoPointPalette"/>; собственный <see cref="GeoPoint.Color"/> точки важнее всего.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string>? CategoryColors { get; set; }

    /// <summary>
    /// Содержимое карточки точки вместо встроенного: открывается по клику на маркер
    /// или на строку списка.
    /// </summary>
    [Parameter] public RenderFragment<GeoPoint>? PointTemplate { get; set; }

    /// <summary>Клик по маркеру точки.</summary>
    [Parameter] public EventCallback<GeoPoint> OnPointClick { get; set; }

    /// <summary>Клик по маркеру, точка — в виде участника.</summary>
    [Parameter] public EventCallback<Participant> OnMarkerClick { get; set; }

    /// <summary>
    /// Проекция карты. Если не задана, берётся из <see cref="MapConfiguration.Projection"/>
    /// (по умолчанию Equal Earth).
    /// </summary>
    [Parameter] public MapProjection? Projection { get; set; }

    /// <summary>
    /// Центральный меридиан карты в градусах. Если не задан, берётся из
    /// <see cref="MapConfiguration.CentralMeridian"/> (по умолчанию 0 — Гринвич).
    /// </summary>
    [Parameter] public double? CentralMeridian { get; set; }

    /// <summary>
    /// Начальное приближение: 1 — мир целиком. Если не задано, берётся из
    /// <see cref="MapConfiguration.DefaultZoom"/> (без настроек — 2).
    /// </summary>
    [Parameter] public double? Zoom { get; set; }

    /// <summary>
    /// Широта начального центра карты. Если не задана, берётся из
    /// <see cref="MapConfiguration.DefaultLatitude"/> (без настроек — 20).
    /// </summary>
    [Parameter] public double? CenterLatitude { get; set; }

    /// <summary>
    /// Долгота начального центра карты. Если не задана, берётся из
    /// <see cref="MapConfiguration.DefaultLongitude"/>, а без настроек совпадает
    /// с центральным меридианом.
    /// </summary>
    [Parameter] public double? CenterLongitude { get; set; }

    private const string MapScriptPath = "/_content/ZealousMindedPeopleGeo/js/community-map.js";

    // Точки уходят в JS одним JSON: свойства с camelCase, ключи Properties как есть.
    private static readonly JsonSerializerOptions PointJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private List<GeoPoint> _points = new();
    private GeoPointPalette _palette = GeoPointPalette.For(Array.Empty<GeoPoint>());
    private readonly HashSet<string> _hiddenCategories = new(StringComparer.Ordinal);
    private GeoPoint? _selectedPoint;
    private bool _isLoading = true;
    private bool _mapReady;
    private bool _participantsListOpen;
    private bool _subscribed;
    private DotNetObjectReference<CommunityMapComponent>? _dotNetRef;
    private IJSObjectReference? _mapModule;

    // Источник, из которого загружены текущие точки: при его смене карта обновляется.
    private object? _loadedPoints;
    private object? _loadedParticipants;
    private string? _loadedContainerId;
    private object? _loadedCategoryColors;

    private string ParticipantsPanelId => $"{MapId}-participants";

    private IEnumerable<GeoPoint> VisiblePoints =>
        _points.Where(p => !_hiddenCategories.Contains(GeoPointPalette.CategoryOf(p)));

    protected override void OnInitialized()
    {
        _participantsListOpen = ParticipantsListOpen;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_mapReady)
        {
            return;
        }

        if (!ReferenceEquals(_loadedPoints, Points)
            || !ReferenceEquals(_loadedParticipants, Participants)
            || _loadedContainerId != DataContainerId
            || !ReferenceEquals(_loadedCategoryColors, CategoryColors))
        {
            await RefreshPointsAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeMapAsync();
        }
    }

    private async Task InitializeMapAsync()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            await LoadPointsAsync();

            var projection = Projection ?? Options.Value.Map?.Projection ?? MapProjection.EqualEarth;
            var centralMeridian = CentralMeridian ?? Options.Value.Map?.CentralMeridian ?? 0.0;
            var centerLat = CenterLatitude ?? Options.Value.Map?.DefaultLatitude ?? 20.0;
            var centerLng = CenterLongitude ?? Options.Value.Map?.DefaultLongitude ?? centralMeridian;
            var zoom = Zoom ?? Options.Value.Map?.DefaultZoom ?? 2;

            // Скрипт карты подключается модулем: браузер исполняет его один раз на адрес,
            // сколько бы карт ни было на странице. <HeadContent> для этого не годится:
            // в <head> попадает только последний из них, а при пререндере скрипт
            // исполнялся дважды.
            _mapModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", MapScriptPath);

            // Обработчик кликов передаётся каждой карте свой: общий setDotNetHelper
            // отправлял клики всех карт страницы последней из них.
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync(
                "initializeCommunityMap",
                // Первый аргумент (ключ Google Maps) скрипт игнорирует. Ключ из настроек
                // нужен только серверному геокодированию, в браузер его не отправляем.
                string.Empty,
                centerLat,
                centerLng,
                zoom,
                MapId,
                // JS принимает имя проекции без учёта регистра: "EqualEarth", "Equirectangular".
                new { projection = projection.ToString(), centralMeridian, dotNetHelper = _dotNetRef });

            await PushPointsAsync();
            _mapReady = true;

            if (!_subscribed)
            {
                ContainerManager.OnDataChanged += HandleContainerChanged;
                _subscribed = true;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка инициализации карты");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadPointsAsync()
    {
        _loadedPoints = Points;
        _loadedParticipants = Participants;
        _loadedContainerId = DataContainerId;
        _loadedCategoryColors = CategoryColors;

        List<GeoPoint> points;
        if (Points is not null)
        {
            points = Points.Where(p => p is not null && p.Validate() is null).Select(p => p.Clone()).ToList();
        }
        else if (Participants is not null)
        {
            points = FromParticipants(Participants);
        }
        else if (!string.IsNullOrEmpty(DataContainerId))
        {
            points = (await ContainerManager.GetOrCreateContainer(DataContainerId).GetPointsAsync()).ToList();
        }
        else
        {
            points = FromParticipants(await ParticipantRepository.GetAllParticipantsAsync());
        }

        _points = points;
        _palette = GeoPointPalette.For(points, CategoryColors);

        // Скрытыми остаются только категории, которые ещё есть на карте.
        _hiddenCategories.IntersectWith(_palette.Categories.Select(c => c.Name));

        Logger.LogInformation("Загружено {Count} точек для карты {MapId}", points.Count, MapId);
    }

    // Участник с нулевой широтой или долготой считается ненайденным: геокодирование
    // не дало координат. На карту и в список он не попадает.
    private static List<GeoPoint> FromParticipants(IEnumerable<Participant> participants) =>
        participants
            .Where(p => p is not null && p.Latitude is { } lat && p.Longitude is { } lng && lat != 0 && lng != 0)
            .Select(p => p.ToGeoPoint())
            .Where(p => p.Validate() is null)
            .ToList();

    private async Task RefreshPointsAsync()
    {
        try
        {
            await LoadPointsAsync();
            if (_selectedPoint is not null && _points.All(p => p.Id != _selectedPoint.Id))
            {
                _selectedPoint = null;
            }

            await PushPointsAsync();
        }
        catch (JSDisconnectedException)
        {
            // Страница закрыта: обновлять нечего.
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обновления точек карты {MapId}", MapId);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task PushPointsAsync()
    {
        var payload = VisiblePoints.Select(point => new
        {
            id = point.Id,
            latitude = point.Latitude,
            longitude = point.Longitude,
            title = point.Title,
            description = point.Description,
            category = point.Category,
            color = _palette.ColorFor(point),
            icon = point.Icon,
            url = SafeUrl(point.Url),
            properties = point.Properties
        });

        await JSRuntime.InvokeVoidAsync("loadPointsOnMap", JsonSerializer.Serialize(payload, PointJsonOptions), MapId);
    }

    private void HandleContainerChanged(string containerId, GeoDataChangeType changeType)
    {
        if (_mapReady && Points is null && Participants is null && containerId == DataContainerId)
        {
            _ = InvokeAsync(RefreshPointsAsync);
        }
    }

    private void ToggleParticipantsList() => _participantsListOpen = !_participantsListOpen;

    private async Task ToggleCategoryAsync(string category)
    {
        if (!_hiddenCategories.Remove(category))
        {
            _hiddenCategories.Add(category);
        }

        if (_selectedPoint is not null && _hiddenCategories.Contains(GeoPointPalette.CategoryOf(_selectedPoint)))
        {
            _selectedPoint = null;
        }

        if (_mapReady)
        {
            await PushPointsAsync();
        }
    }

    private bool IsCategoryVisible(string category) => !_hiddenCategories.Contains(category);

    /// <summary>
    /// Вызывается из JS при клике по маркеру точки.
    /// </summary>
    [JSInvokable]
    public async Task OnPointMarkerClick(string pointId)
    {
        var point = _points.FirstOrDefault(p => p.Id == pointId);
        if (point is null)
        {
            return;
        }

        _selectedPoint = point;
        await InvokeAsync(StateHasChanged);

        if (OnPointClick.HasDelegate)
        {
            await OnPointClick.InvokeAsync(point.Clone());
        }

        if (OnMarkerClick.HasDelegate)
        {
            await OnMarkerClick.InvokeAsync(point.ToParticipant());
        }
    }

    /// <summary>
    /// Прежнее имя <see cref="OnPointMarkerClick"/>: идентификатор участника — это
    /// идентификатор его точки.
    /// </summary>
    [JSInvokable]
    public Task OnParticipantMarkerClick(string participantId) => OnPointMarkerClick(participantId);

    /// <summary>Открывает карточку точки.</summary>
    public void ShowPointInfo(GeoPoint point)
    {
        _selectedPoint = point;
        InvokeAsync(StateHasChanged);
    }

    /// <summary>Открывает карточку участника, если его точка есть на карте.</summary>
    public void ShowParticipantInfo(Participant participant)
    {
        var point = _points.FirstOrDefault(p => p.Id == participant.Id.ToString());
        if (point is not null)
        {
            ShowPointInfo(point);
        }
    }

    public void CloseParticipantModal()
    {
        _selectedPoint = null;
        InvokeAsync(StateHasChanged);
    }

    // Подпись внутри маркера и аватара: короткая иконка или первая буква заголовка.
    private static string MarkerLabel(GeoPoint point)
    {
        var icon = point.Icon?.Trim();
        if (!string.IsNullOrEmpty(icon) && new StringInfo(icon).LengthInTextElements <= 2)
        {
            return icon;
        }

        var title = point.Title.Trim();
        return title.Length == 0 ? "?" : StringInfo.GetNextTextElement(title).ToUpperInvariant();
    }

    // Ссылка из данных попадает в href: пропускаем только http(s) и mailto, чтобы
    // javascript: и подобные схемы не исполнялись по клику.
    private static string? SafeUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto)
            ? uri.ToString()
            : null;

    private static string CategoryTitle(string category) => category.Length == 0 ? "No category" : category;

    private static string Truncate(string? text, int length) =>
        string.IsNullOrEmpty(text) || text.Length <= length ? text ?? string.Empty : text[..length].TrimEnd() + "…";

    private static string? PlaceOf(GeoPoint point)
    {
        point.Properties.TryGetValue(ParticipantPointProperties.City, out var city);
        point.Properties.TryGetValue(ParticipantPointProperties.Country, out var country);
        // Город, совпадающий с заголовком (точка «London» в Лондоне), не повторяем.
        if (string.Equals(city?.Trim(), point.Title.Trim(), StringComparison.CurrentCultureIgnoreCase))
        {
            city = null;
        }

        var parts = new[] { city, country }.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    // Свойства для встроенной карточки: подписи для полей участника, остальные — как есть.
    private static IEnumerable<(string Label, string Value)> CardProperties(GeoPoint point)
    {
        foreach (var (key, value) in point.Properties)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (key == ParticipantPointProperties.RegisteredAt)
            {
                var date = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                    ? parsed.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                    : value;
                yield return ("Registration Date", date);
                continue;
            }

            yield return (PropertyLabels.TryGetValue(key, out var label) ? label : key, value);
        }
    }

    private static readonly Dictionary<string, string> PropertyLabels = new(StringComparer.Ordinal)
    {
        [ParticipantPointProperties.Address] = "Address",
        [ParticipantPointProperties.Email] = "Email",
        [ParticipantPointProperties.Location] = "Location",
        [ParticipantPointProperties.City] = "City",
        [ParticipantPointProperties.Country] = "Country",
        [ParticipantPointProperties.SocialMedia] = "Social Media",
        [ParticipantPointProperties.LifeGoals] = "Life Goals",
        [ParticipantPointProperties.Skills] = "Skills",
        [ParticipantPointProperties.Discord] = "Discord",
        [ParticipantPointProperties.Telegram] = "Telegram",
        [ParticipantPointProperties.Vk] = "VK",
        [ParticipantPointProperties.Website] = "Website"
    };

    public async ValueTask DisposeAsync()
    {
        if (_subscribed)
        {
            ContainerManager.OnDataChanged -= HandleContainerChanged;
        }

        // Без загруженного модуля карта в браузере не создавалась (например, при пререндере).
        if (_mapModule is not null)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("disposeCommunityMap", MapId);
                await _mapModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Соединение JS уже закрыто (например, на странице переключили компонент).
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Не удалось освободить ресурсы карты {MapId}", MapId);
            }
        }

        _dotNetRef?.Dispose();
    }
}

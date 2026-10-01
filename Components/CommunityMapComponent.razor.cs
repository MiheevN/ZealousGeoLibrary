using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Text.Json;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Components;

public partial class CommunityMapComponent : IAsyncDisposable
{
    [Parameter] public string MapId { get; set; } = "map";
    [Parameter] public string Height { get; set; } = "500px";
    [Parameter] public bool ShowParticipantsList { get; set; } = true;
    [Parameter] public EventCallback<Participant> OnMarkerClick { get; set; }

    /// <summary>
    /// Необязательный явный набор участников для отображения на карте.
    /// Если задан, карта использует именно его вместо общего
    /// <see cref="Services.Repositories.IParticipantRepository"/>. Это позволяет
    /// нескольким картам показывать независимые, изолированные данные.
    /// </summary>
    [Parameter] public IEnumerable<Participant>? Participants { get; set; }

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

    private Participant? SelectedParticipant;
    private IEnumerable<Participant> ParticipantsView = new List<Participant>();
    private bool _isLoading = true;
    private DotNetObjectReference<CommunityMapComponent>? _dotNetRef;

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

            ParticipantsView = Participants ?? await ParticipantRepository.GetAllParticipantsAsync();
            Logger.LogInformation("Загружено {Count} участников для карты", ParticipantsView.Count());

            var apiKey = Options.Value.GoogleMapsApiKey ?? "";
            var centerLat = Options.Value.Map?.DefaultLatitude ?? 20.0;
            var centerLng = Options.Value.Map?.DefaultLongitude ?? 0.0;
            var zoom = Options.Value.Map?.DefaultZoom ?? 2;
            var projection = Projection ?? Options.Value.Map?.Projection ?? MapProjection.EqualEarth;
            var centralMeridian = CentralMeridian ?? Options.Value.Map?.CentralMeridian ?? 0.0;

            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("setDotNetHelper", _dotNetRef);
            await JSRuntime.InvokeVoidAsync(
                "initializeCommunityMap",
                apiKey,
                centerLat,
                centerLng,
                zoom,
                MapId,
                // JS принимает имя проекции без учёта регистра: "EqualEarth", "Equirectangular".
                new { projection = projection.ToString(), centralMeridian });

            var participantsJson = JsonSerializer.Serialize(ParticipantsView);
            await JSRuntime.InvokeVoidAsync("loadParticipantsOnMap", participantsJson, MapId);

            _isLoading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка инициализации карты");
            _isLoading = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnParticipantMarkerClick(string participantId)
    {
        var participant = ParticipantsView.FirstOrDefault(p => p.Id.ToString() == participantId);
        if (participant != null)
        {
            SelectedParticipant = participant;
            await InvokeAsync(StateHasChanged);

            if (OnMarkerClick.HasDelegate)
            {
                await OnMarkerClick.InvokeAsync(participant);
            }
        }
    }

    public void ShowParticipantInfo(Participant participant)
    {
        SelectedParticipant = participant;
        InvokeAsync(StateHasChanged);
    }

    public void CloseParticipantModal()
    {
        SelectedParticipant = null;
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("disposeCommunityMap", MapId);
        }
        catch (JSDisconnectedException)
        {
            // Соединение JS уже закрыто (например, на странице переключили компонент).
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Не удалось освободить ресурсы карты {MapId}", MapId);
        }

        _dotNetRef?.Dispose();
    }
}

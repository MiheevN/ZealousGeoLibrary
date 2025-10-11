using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Components;

public partial class CommunityMapComponent : IDisposable
{
    [Parameter] public IEnumerable<Participant> Participants { get; set; } = new List<Participant>();
    [Parameter] public string Height { get; set; } = "500px";
    [Parameter] public bool ShowParticipantsList { get; set; } = true;
    [Parameter] public EventCallback<Participant> OnMarkerClick { get; set; }

    private Participant? SelectedParticipant;

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

    public void Dispose()
    {
        // Очистка ресурсов при необходимости
    }
}
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping;

public class GlobeStateService
{
    private readonly Dictionary<string, GlobeInstanceState> _globes = new();

    public event Action<string>? OnStateChanged;

    public GlobeInstanceState GetOrCreateGlobe(string globeId)
    {
        if (!_globes.ContainsKey(globeId))
        {
            _globes[globeId] = new GlobeInstanceState();
        }
        return _globes[globeId];
    }

    public void UpdateState(string globeId, Action<GlobeInstanceState> updateAction)
    {
        var state = GetOrCreateGlobe(globeId);
        updateAction(state);
        OnStateChanged?.Invoke(globeId);
    }

    public bool GlobeExists(string globeId) => _globes.ContainsKey(globeId);

    public void RemoveGlobe(string globeId)
    {
        _globes.Remove(globeId);
    }
}

public class GlobeInstanceState
{
    public bool IsAutoRotating { get; set; } = true;
    public int ParticipantCount { get; set; }
    public List<Participant> Participants { get; set; } = new();
    public bool IsInitialized { get; set; }
    public int CurrentLod { get; set; } = 2;
    public double CameraLatitude { get; set; }
    public double CameraLongitude { get; set; }
    public double CameraZoom { get; set; } = 2.5;
}

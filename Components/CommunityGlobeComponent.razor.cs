using Microsoft.AspNetCore.Components;

namespace ZealousMindedPeopleGeo.Components;

/// <summary>
/// Компонент-обертка для обратной совместимости.
/// Вся логика перенесена в модульные компоненты.
/// </summary>
public partial class CommunityGlobeComponent : IDisposable
{
    public void Dispose()
    {
        // Cleanup handled by child components
    }
}

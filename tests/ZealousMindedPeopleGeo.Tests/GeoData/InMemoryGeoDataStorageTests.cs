using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Хранилище гео-данных в памяти: общий контракт плюс собственная семантика —
/// контейнер существует с момента создания, даже пустой.
/// </summary>
public sealed class InMemoryGeoDataStorageTests : GeoDataStorageContractTests
{
    protected override IGeoDataContainerManager Manager { get; } =
        new GeoDataContainerManager(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance);

    [Fact]
    public void EmptyContainer_ExistsAfterCreation()
    {
        var container = Manager.GetOrCreateContainer("empty");

        Assert.True(Manager.ContainerExists("empty"));
        Assert.Same(container, Manager.GetContainer("empty"));
        Assert.Same(container, Manager.GetOrCreateContainer("empty"));
        Assert.Contains("empty", Manager.GetContainerIds());
    }

    [Fact]
    public void RemoveContainer_EmptyContainer_ReturnsTrueWithoutEvent()
    {
        Manager.GetOrCreateContainer("empty");
        var events = 0;
        Manager.OnDataChanged += (_, _) => events++;

        Assert.True(Manager.RemoveContainer("empty"));
        Assert.Equal(0, events);
    }
}

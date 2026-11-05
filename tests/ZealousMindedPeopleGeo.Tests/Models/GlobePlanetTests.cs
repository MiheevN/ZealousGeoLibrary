using System.Runtime.CompilerServices;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// Встроенные планеты: карты лежат в библиотеке, объект каждый раз новый.
/// </summary>
public class GlobePlanetTests
{
    public static TheoryData<string> BuiltInPlanets => new() { nameof(GlobePlanet.Mars), nameof(GlobePlanet.Moon) };

    private static GlobePlanet Get(string name) => name == nameof(GlobePlanet.Mars) ? GlobePlanet.Mars : GlobePlanet.Moon;

    [Theory]
    [MemberData(nameof(BuiltInPlanets))]
    public void BuiltInPlanet_MapIsAStaticFileOfTheLibrary(string name)
    {
        const string prefix = "/_content/ZealousMindedPeopleGeo/";
        var url = Get(name).DayTextureUrl;

        Assert.StartsWith(prefix, url);
        var file = Path.Combine(RepositoryRoot(), "wwwroot", url[prefix.Length..]);
        Assert.True(File.Exists(file), $"{name}: {file} is missing");
    }

    [Theory]
    [MemberData(nameof(BuiltInPlanets))]
    public void BuiltInPlanet_IsANewObjectEveryTime_SoChangingItIsSafe(string name)
    {
        var changed = Get(name);
        changed.DayTextureUrl = "other.jpg";

        Assert.NotSame(changed, Get(name));
        Assert.NotEqual("other.jpg", Get(name).DayTextureUrl);
    }

    [Fact]
    public void Moon_HasNoAtmosphere_MarsHasAThinOne()
    {
        Assert.Null(GlobePlanet.Moon.AtmosphereColor);
        Assert.NotNull(GlobePlanet.Mars.AtmosphereColor);
        Assert.InRange(GlobePlanet.Mars.AtmosphereOpacity, 0.05, 0.5);
    }

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));
}

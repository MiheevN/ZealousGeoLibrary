using System.Reflection;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Showcase;

/// <summary>
/// Сведения о подключённой библиотеке для оболочки витрины.
/// </summary>
public static class ShowcaseInfo
{
    /// <summary>
    /// Версия пакета ZealousMindedPeopleGeo без хэша коммита.
    /// </summary>
    public static string LibraryVersion { get; } =
        typeof(Participant).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0]
        ?? "—";
}

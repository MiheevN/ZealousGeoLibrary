using System.Globalization;

namespace ZealousMindedPeopleGeo.Tests;

/// <summary>
/// Язык сообщений (<see cref="CultureInfo.CurrentUICulture"/>) и формат чисел
/// (<see cref="CultureInfo.CurrentCulture"/>) на время теста: тексты ошибок не
/// зависят от языка системы, на которой запущены тесты.
/// </summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(string uiCulture, string? culture = null)
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        CultureInfo.CurrentCulture = culture is null ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}

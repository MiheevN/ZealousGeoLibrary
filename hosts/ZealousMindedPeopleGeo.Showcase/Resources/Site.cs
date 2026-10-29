using System.Globalization;
using System.Resources;

namespace ZealousMindedPeopleGeo.Showcase.Resources;

/// <summary>
/// Надписи витрины на языке <see cref="CultureInfo.CurrentUICulture"/>: Site.resx — английский
/// (он же запасной), Site.ru.resx — русский. Тексты компонентов библиотеки — в её Ui.resx.
/// </summary>
public static class Site
{
    private static readonly ResourceManager Resources = new(typeof(Site));

    public static string BrandSub => Text(nameof(BrandSub));
    public static string NavLabel => Text(nameof(NavLabel));
    public static string NavOverview => Text(nameof(NavOverview));
    public static string NavComponents => Text(nameof(NavComponents));
    public static string NavMap => Text(nameof(NavMap));
    public static string NavGlobe => Text(nameof(NavGlobe));
    public static string NavShowcase => Text(nameof(NavShowcase));
    public static string FooterLibrary => Text(nameof(FooterLibrary));
    public static string FooterEnvironment => Text(nameof(FooterEnvironment));
    public static string Language => Text(nameof(Language));
    public static string ErrorUnhandled => Text(nameof(ErrorUnhandled));
    public static string ErrorReload => Text(nameof(ErrorReload));
    public static string Close => Text(nameof(Close));
    public static string TitleSuffix(params object?[] arguments) => Format(nameof(TitleSuffix), arguments);
    public static string HomeEyebrow(params object?[] arguments) => Format(nameof(HomeEyebrow), arguments);
    public static string HomeDescription => Text(nameof(HomeDescription));
    public static string HomeOpenMap => Text(nameof(HomeOpenMap));
    public static string HomeOpenGlobe => Text(nameof(HomeOpenGlobe));
    public static string HomeHeroAlt => Text(nameof(HomeHeroAlt));
    public static string HomePages => Text(nameof(HomePages));
    public static string HomeMapDescription => Text(nameof(HomeMapDescription));
    public static string HomeGlobeDescription => Text(nameof(HomeGlobeDescription));
    public static string HomeShowcaseDescription => Text(nameof(HomeShowcaseDescription));
    public static string HomeDataSets => Text(nameof(HomeDataSets));
    public static string HomeDataSet => Text(nameof(HomeDataSet));
    public static string HomeDataSetDescription => Text(nameof(HomeDataSetDescription));
    public static string HomePointCount => Text(nameof(HomePointCount));
    public static string HomeRun => Text(nameof(HomeRun));
    public static string HomeRunWatch => Text(nameof(HomeRunWatch));
    public static string HomeRunNote => Text(nameof(HomeRunNote));
    public static string NotFoundTitle => Text(nameof(NotFoundTitle));
    public static string NotFoundDescription => Text(nameof(NotFoundDescription));
    public static string NotFoundHome => Text(nameof(NotFoundHome));
    public static string MapDescription => Text(nameof(MapDescription));
    public static string PointCount(params object?[] arguments) => Format(nameof(PointCount), arguments);
    public static string MapOptions => Text(nameof(MapOptions));
    public static string MapProjection => Text(nameof(MapProjection));
    public static string MapRectangular => Text(nameof(MapRectangular));
    public static string MapCentralMeridian => Text(nameof(MapCentralMeridian));
    public static string MapMeridianAmerica => Text(nameof(MapMeridianAmerica));
    public static string MapMeridianGreenwich => Text(nameof(MapMeridianGreenwich));
    public static string MapMeridianAsia => Text(nameof(MapMeridianAsia));
    public static string MapMeridianPacific => Text(nameof(MapMeridianPacific));
    public static string MapGrouping => Text(nameof(MapGrouping));
    public static string On => Text(nameof(On));
    public static string Off => Text(nameof(Off));
    public static string DataSet => Text(nameof(DataSet));
    public static string MapDownload => Text(nameof(MapDownload));
    public static string MapUpload => Text(nameof(MapUpload));
    public static string MapListTitle => Text(nameof(MapListTitle));
    public static string ImportTooLarge(params object?[] arguments) => Format(nameof(ImportTooLarge), arguments);
    public static string ImportNotGeoJson(params object?[] arguments) => Format(nameof(ImportNotGeoJson), arguments);
    public static string ImportNoPoints(params object?[] arguments) => Format(nameof(ImportNoPoints), arguments);
    public static string ImportNoPointsReason(params object?[] arguments) => Format(nameof(ImportNoPointsReason), arguments);
    public static string ImportLoaded(params object?[] arguments) => Format(nameof(ImportLoaded), arguments);
    public static string ImportLoadedSkipped(params object?[] arguments) => Format(nameof(ImportLoadedSkipped), arguments);
    public static string GlobeDescriptionBefore => Text(nameof(GlobeDescriptionBefore));
    public static string GlobeDescriptionAfter => Text(nameof(GlobeDescriptionAfter));
    public static string Categories => Text(nameof(Categories));
    public static string Lighting => Text(nameof(Lighting));
    public static string LightingDay => Text(nameof(LightingDay));
    public static string LightingDayDescription => Text(nameof(LightingDayDescription));
    public static string LightingRealTime => Text(nameof(LightingRealTime));
    public static string LightingRealTimeDescription => Text(nameof(LightingRealTimeDescription));
    public static string Planet => Text(nameof(Planet));
    public static string PlanetEarth => Text(nameof(PlanetEarth));
    public static string PlanetEarthDescription => Text(nameof(PlanetEarthDescription));
    public static string PlanetDemo => Text(nameof(PlanetDemo));
    public static string PlanetDemoDescription => Text(nameof(PlanetDemoDescription));
    public static string NoCategory => Text(nameof(NoCategory));
    public static string Points => Text(nameof(Points));

    private static string Text(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(name), arguments);
}

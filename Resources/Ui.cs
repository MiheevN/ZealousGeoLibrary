using System.Globalization;
using System.Resources;

namespace ZealousMindedPeopleGeo.Resources;

/// <summary>
/// Надписи компонентов на языке <see cref="CultureInfo.CurrentUICulture"/>. Тексты лежат в
/// Ui.resx (английский, он же запасной) и Ui.{язык}.resx — как и у <see cref="Messages"/>.
/// Свойства Validation* — для <c>ErrorMessageResourceType</c> атрибутов моделей.
/// </summary>
internal static class Ui
{
    private static readonly ResourceManager Resources = new(typeof(Ui));

    public static string Close => Text(nameof(Close));
    public static string Coordinates => Text(nameof(Coordinates));
    public static string FormTitle => Text(nameof(FormTitle));
    public static string FormSubmit => Text(nameof(FormSubmit));
    public static string FormSaving => Text(nameof(FormSaving));
    public static string FormClear => Text(nameof(FormClear));
    public static string FormName => Text(nameof(FormName));
    public static string FormNamePlaceholder => Text(nameof(FormNamePlaceholder));
    public static string FormEmail => Text(nameof(FormEmail));
    public static string FormEmailHint => Text(nameof(FormEmailHint));
    public static string FormAddress => Text(nameof(FormAddress));
    public static string FormAddressPlaceholder => Text(nameof(FormAddressPlaceholder));
    public static string FormGeocoding => Text(nameof(FormGeocoding));
    public static string FormSocialMedia => Text(nameof(FormSocialMedia));
    public static string FormSocialMediaPlaceholder => Text(nameof(FormSocialMediaPlaceholder));
    public static string FormMessage => Text(nameof(FormMessage));
    public static string FormMessagePlaceholder => Text(nameof(FormMessagePlaceholder));
    public static string FormLocationFound => Text(nameof(FormLocationFound));
    public static string FormFoundAddress => Text(nameof(FormFoundAddress));
    public static string FormSaved => Text(nameof(FormSaved));
    public static string FormSaveFailed => Text(nameof(FormSaveFailed));
    public static string FormError => Text(nameof(FormError));
    public static string FormEnterAddress => Text(nameof(FormEnterAddress));
    public static string FormGeocodingFailed => Text(nameof(FormGeocodingFailed));
    public static string ValidationNameRequired => Text(nameof(ValidationNameRequired));
    public static string ValidationNameTooLong => Text(nameof(ValidationNameTooLong));
    public static string ValidationAddressRequired => Text(nameof(ValidationAddressRequired));
    public static string ValidationAddressTooLong => Text(nameof(ValidationAddressTooLong));
    public static string ValidationEmailInvalid => Text(nameof(ValidationEmailInvalid));
    public static string ValidationEmailTooLong => Text(nameof(ValidationEmailTooLong));
    public static string ValidationLocationRequired => Text(nameof(ValidationLocationRequired));
    public static string ValidationLocationTooLong => Text(nameof(ValidationLocationTooLong));
    public static string ValidationLatitudeRange => Text(nameof(ValidationLatitudeRange));
    public static string ValidationLongitudeRange => Text(nameof(ValidationLongitudeRange));
    public static string ValidationCityTooLong => Text(nameof(ValidationCityTooLong));
    public static string ValidationCountryTooLong => Text(nameof(ValidationCountryTooLong));
    public static string ValidationSocialMediaTooLong => Text(nameof(ValidationSocialMediaTooLong));
    public static string ValidationMessageTooLong => Text(nameof(ValidationMessageTooLong));
    public static string ValidationUrlInvalid => Text(nameof(ValidationUrlInvalid));
    public static string MapTitle => Text(nameof(MapTitle));
    public static string MapListTitle => Text(nameof(MapListTitle));
    public static string MapShowList => Text(nameof(MapShowList));
    public static string MapHideList => Text(nameof(MapHideList));
    public static string MapCategories => Text(nameof(MapCategories));
    public static string MapNoCategory => Text(nameof(MapNoCategory));
    public static string MapEmpty => Text(nameof(MapEmpty));
    public static string MapAllHidden => Text(nameof(MapAllHidden));
    public static string MapLoading => Text(nameof(MapLoading));
    public static string MapCanvasLabel => Text(nameof(MapCanvasLabel));
    public static string MapZoomIn => Text(nameof(MapZoomIn));
    public static string MapZoomOut => Text(nameof(MapZoomOut));
    public static string MapResetView => Text(nameof(MapResetView));
    public static string MapPointFallback => Text(nameof(MapPointFallback));
    public static string MapClusterExpand => Text(nameof(MapClusterExpand));
    public static string MapClusterZoom => Text(nameof(MapClusterZoom));
    public static string MapClusterMore => Text(nameof(MapClusterMore));
    public static string MapClusterOne => Text(nameof(MapClusterOne));
    public static string MapClusterFew => Text(nameof(MapClusterFew));
    public static string MapClusterMany => Text(nameof(MapClusterMany));
    public static string MapClusterOther => Text(nameof(MapClusterOther));
    public static string PropertyCategory => Text(nameof(PropertyCategory));
    public static string PropertyDescription => Text(nameof(PropertyDescription));
    public static string PropertyAddress => Text(nameof(PropertyAddress));
    public static string PropertyEmail => Text(nameof(PropertyEmail));
    public static string PropertyLocation => Text(nameof(PropertyLocation));
    public static string PropertyCity => Text(nameof(PropertyCity));
    public static string PropertyCountry => Text(nameof(PropertyCountry));
    public static string PropertySocialMedia => Text(nameof(PropertySocialMedia));
    public static string PropertyLifeGoals => Text(nameof(PropertyLifeGoals));
    public static string PropertySkills => Text(nameof(PropertySkills));
    public static string PropertyWebsite => Text(nameof(PropertyWebsite));
    public static string PropertyRegisteredAt => Text(nameof(PropertyRegisteredAt));

    public static string FormAddressNotFound(string? reason) => Format(nameof(FormAddressNotFound), reason ?? string.Empty);

    public static string MapTotal(int count) => Format(nameof(MapTotal), count);

    public static string MapShowCategory(string category) => Format(nameof(MapShowCategory), category);

    public static string MapHideCategory(string category) => Format(nameof(MapHideCategory), category);

    /// <summary>
    /// Надписи для community-map.js: ключи — как в его <c>DEFAULT_TEXTS</c>. Формы
    /// «{0} точка/точки/точек» скрипт выбирает сам через <c>Intl.PluralRules</c>.
    /// </summary>
    public static Dictionary<string, string> MapScriptTexts() => new()
    {
        ["canvasLabel"] = MapCanvasLabel,
        ["zoomIn"] = MapZoomIn,
        ["zoomOut"] = MapZoomOut,
        ["resetView"] = MapResetView,
        ["pointFallback"] = MapPointFallback,
        ["clusterExpand"] = MapClusterExpand,
        ["clusterZoom"] = MapClusterZoom,
        ["clusterMore"] = MapClusterMore,
        ["clusterOne"] = MapClusterOne,
        ["clusterFew"] = MapClusterFew,
        ["clusterMany"] = MapClusterMany,
        ["clusterOther"] = MapClusterOther,
        ["category"] = PropertyCategory,
        ["description"] = PropertyDescription,
        ["coordinates"] = Coordinates,
        ["address"] = PropertyAddress,
        ["email"] = PropertyEmail,
        ["location"] = PropertyLocation,
        ["socialMedia"] = PropertySocialMedia,
        ["skills"] = PropertySkills,
        ["lifeGoals"] = PropertyLifeGoals,
        ["website"] = PropertyWebsite,
        ["registeredAt"] = PropertyRegisteredAt
    };

    private static string Text(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(string name, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(name), arguments);
}

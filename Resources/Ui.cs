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
    public static string FormAddressNotFound(params object?[] arguments) => Format(nameof(FormAddressNotFound), arguments);
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
    public static string MapTotal(params object?[] arguments) => Format(nameof(MapTotal), arguments);
    public static string MapCategories => Text(nameof(MapCategories));
    public static string MapShowCategory(params object?[] arguments) => Format(nameof(MapShowCategory), arguments);
    public static string MapHideCategory(params object?[] arguments) => Format(nameof(MapHideCategory), arguments);
    public static string MapNoCategory => Text(nameof(MapNoCategory));
    public static string MapEmpty => Text(nameof(MapEmpty));
    public static string MapAllHidden => Text(nameof(MapAllHidden));
    public static string MapLoading => Text(nameof(MapLoading));
    public static string MapCanvasLabel => Text(nameof(MapCanvasLabel));
    public static string MapZoomIn => Text(nameof(MapZoomIn));
    public static string MapZoomOut => Text(nameof(MapZoomOut));
    public static string MapResetView => Text(nameof(MapResetView));
    public static string MapKeyboardHint => Text(nameof(MapKeyboardHint));
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
    public static string GlobeLoading => Text(nameof(GlobeLoading));
    public static string GlobeRetry => Text(nameof(GlobeRetry));
    public static string GlobeBadSize(params object?[] arguments) => Format(nameof(GlobeBadSize), arguments);
    public static string GlobeInitFailed(params object?[] arguments) => Format(nameof(GlobeInitFailed), arguments);
    public static string GlobeAddPointsFailed(params object?[] arguments) => Format(nameof(GlobeAddPointsFailed), arguments);
    public static string GlobeLoadPointsFailed(params object?[] arguments) => Format(nameof(GlobeLoadPointsFailed), arguments);
    public static string GlobeRotation => Text(nameof(GlobeRotation));
    public static string GlobeToggleRotation => Text(nameof(GlobeToggleRotation));
    public static string GlobePointCount => Text(nameof(GlobePointCount));
    public static string GlobeDetail => Text(nameof(GlobeDetail));
    public static string GlobeDetailLow => Text(nameof(GlobeDetailLow));
    public static string GlobeDetailMedium => Text(nameof(GlobeDetailMedium));
    public static string GlobeDetailHigh => Text(nameof(GlobeDetailHigh));
    public static string GlobeDetailUltra => Text(nameof(GlobeDetailUltra));
    public static string GlobeCenterOnLocation => Text(nameof(GlobeCenterOnLocation));
    public static string GlobeNoLocation => Text(nameof(GlobeNoLocation));
    public static string DemoRussianCities => Text(nameof(DemoRussianCities));
    public static string DemoRussianCitiesDescription => Text(nameof(DemoRussianCitiesDescription));
    public static string DemoWorldCapitals => Text(nameof(DemoWorldCapitals));
    public static string DemoWorldCapitalsDescription => Text(nameof(DemoWorldCapitalsDescription));
    public static string DemoTechHubs => Text(nameof(DemoTechHubs));
    public static string DemoTechHubsDescription => Text(nameof(DemoTechHubsDescription));
    public static string GlobeCanvasLabel => Text(nameof(GlobeCanvasLabel));
    public static string GlobeKeyboardHint => Text(nameof(GlobeKeyboardHint));
    public static string ManagerTitle => Text(nameof(ManagerTitle));
    public static string ManagerCollapse => Text(nameof(ManagerCollapse));
    public static string ManagerExpand => Text(nameof(ManagerExpand));
    public static string ManagerName => Text(nameof(ManagerName));
    public static string ManagerNamePlaceholder => Text(nameof(ManagerNamePlaceholder));
    public static string ManagerLatitude => Text(nameof(ManagerLatitude));
    public static string ManagerLongitude => Text(nameof(ManagerLongitude));
    public static string ManagerAdding => Text(nameof(ManagerAdding));
    public static string ManagerAdd => Text(nameof(ManagerAdd));
    public static string ManagerClear => Text(nameof(ManagerClear));
    public static string ManagerCenter => Text(nameof(ManagerCenter));
    public static string ManagerQuickAdd => Text(nameof(ManagerQuickAdd));
    public static string ManagerMoscow => Text(nameof(ManagerMoscow));
    public static string ManagerSaintPetersburg => Text(nameof(ManagerSaintPetersburg));
    public static string ManagerNovosibirsk => Text(nameof(ManagerNovosibirsk));
    public static string ManagerYekaterinburg => Text(nameof(ManagerYekaterinburg));
    public static string ManagerKazan => Text(nameof(ManagerKazan));
    public static string ManagerKrasnodar => Text(nameof(ManagerKrasnodar));
    public static string ManagerRandom => Text(nameof(ManagerRandom));
    public static string ManagerRandomName(params object?[] arguments) => Format(nameof(ManagerRandomName), arguments);
    public static string ManagerCentered => Text(nameof(ManagerCentered));
    public static string ManagerCenterFailed(params object?[] arguments) => Format(nameof(ManagerCenterFailed), arguments);
    public static string ManagerEnterName => Text(nameof(ManagerEnterName));
    public static string ManagerEnterCoordinates => Text(nameof(ManagerEnterCoordinates));
    public static string ManagerLatitudeRange => Text(nameof(ManagerLatitudeRange));
    public static string ManagerLongitudeRange => Text(nameof(ManagerLongitudeRange));
    public static string ManagerAdded(params object?[] arguments) => Format(nameof(ManagerAdded), arguments);
    public static string ManagerError(params object?[] arguments) => Format(nameof(ManagerError), arguments);
    public static string SettingsTitle => Text(nameof(SettingsTitle));
    public static string SettingsApply => Text(nameof(SettingsApply));
    public static string SettingsSave => Text(nameof(SettingsSave));
    public static string SettingsLoad => Text(nameof(SettingsLoad));
    public static string SettingsReset => Text(nameof(SettingsReset));
    public static string SettingsSize => Text(nameof(SettingsSize));
    public static string SettingsWidth(params object?[] arguments) => Format(nameof(SettingsWidth), arguments);
    public static string SettingsHeight(params object?[] arguments) => Format(nameof(SettingsHeight), arguments);
    public static string SettingsMarkers => Text(nameof(SettingsMarkers));
    public static string SettingsMarkerSize(params object?[] arguments) => Format(nameof(SettingsMarkerSize), arguments);
    public static string SettingsMarkerOffset(params object?[] arguments) => Format(nameof(SettingsMarkerOffset), arguments);
    public static string SettingsMarkerColor => Text(nameof(SettingsMarkerColor));
    public static string SettingsMarkerOpacity(params object?[] arguments) => Format(nameof(SettingsMarkerOpacity), arguments);
    public static string SettingsMarkerEdgeColor => Text(nameof(SettingsMarkerEdgeColor));
    public static string SettingsRippleInterval(params object?[] arguments) => Format(nameof(SettingsRippleInterval), arguments);
    public static string SettingsRippleDuration(params object?[] arguments) => Format(nameof(SettingsRippleDuration), arguments);
    public static string SettingsAutoRotate => Text(nameof(SettingsAutoRotate));
    public static string SettingsRotateSpeed(params object?[] arguments) => Format(nameof(SettingsRotateSpeed), arguments);
    public static string SettingsLighting => Text(nameof(SettingsLighting));
    public static string SettingsSunFollowsCamera => Text(nameof(SettingsSunFollowsCamera));
    public static string SettingsSunIntensity(params object?[] arguments) => Format(nameof(SettingsSunIntensity), arguments);
    public static string SettingsSunDistance(params object?[] arguments) => Format(nameof(SettingsSunDistance), arguments);
    public static string SettingsSunColor => Text(nameof(SettingsSunColor));
    public static string SettingsAmbientIntensity(params object?[] arguments) => Format(nameof(SettingsAmbientIntensity), arguments);
    public static string SettingsAmbientColor => Text(nameof(SettingsAmbientColor));
    public static string SettingsHemisphereIntensity(params object?[] arguments) => Format(nameof(SettingsHemisphereIntensity), arguments);
    public static string SettingsAtmosphereClouds => Text(nameof(SettingsAtmosphereClouds));
    public static string SettingsEnableAtmosphere => Text(nameof(SettingsEnableAtmosphere));
    public static string SettingsAtmosphereColor => Text(nameof(SettingsAtmosphereColor));
    public static string SettingsAtmosphereOpacity(params object?[] arguments) => Format(nameof(SettingsAtmosphereOpacity), arguments);
    public static string SettingsEnableClouds => Text(nameof(SettingsEnableClouds));
    public static string SettingsCloudsOpacity(params object?[] arguments) => Format(nameof(SettingsCloudsOpacity), arguments);
    public static string SettingsCloudsSpeed(params object?[] arguments) => Format(nameof(SettingsCloudsSpeed), arguments);
    public static string SettingsCamera => Text(nameof(SettingsCamera));
    public static string SettingsEnableZoom => Text(nameof(SettingsEnableZoom));
    public static string SettingsMinZoom(params object?[] arguments) => Format(nameof(SettingsMinZoom), arguments);
    public static string SettingsMaxZoom(params object?[] arguments) => Format(nameof(SettingsMaxZoom), arguments);
    public static string SettingsColors => Text(nameof(SettingsColors));
    public static string SettingsBackground => Text(nameof(SettingsBackground));
    public static string SettingsJson => Text(nameof(SettingsJson));
    public static string PwaLoading => Text(nameof(PwaLoading));
    public static string PwaInstallTitle => Text(nameof(PwaInstallTitle));
    public static string PwaInstallText => Text(nameof(PwaInstallText));
    public static string PwaInstall => Text(nameof(PwaInstall));
    public static string PwaLater => Text(nameof(PwaLater));
    public static string PwaInfoTitle => Text(nameof(PwaInfoTitle));
    public static string PwaStatus => Text(nameof(PwaStatus));
    public static string PwaInstalled => Text(nameof(PwaInstalled));
    public static string PwaNotInstalled => Text(nameof(PwaNotInstalled));
    public static string PwaCache => Text(nameof(PwaCache));
    public static string PwaCacheSummary(params object?[] arguments) => Format(nameof(PwaCacheSummary), arguments);
    public static string PwaInstallApp => Text(nameof(PwaInstallApp));
    public static string PwaCheckUpdates => Text(nameof(PwaCheckUpdates));
    public static string PwaClearCache => Text(nameof(PwaClearCache));
    public static string PwaShowCacheDetails => Text(nameof(PwaShowCacheDetails));
    public static string PwaCacheDetails => Text(nameof(PwaCacheDetails));
    public static string PwaCacheItems(params object?[] arguments) => Format(nameof(PwaCacheItems), arguments);
    public static string PwaNotificationTest => Text(nameof(PwaNotificationTest));
    public static string PwaNotificationTitle => Text(nameof(PwaNotificationTitle));
    public static string PwaNotificationTitlePlaceholder => Text(nameof(PwaNotificationTitlePlaceholder));
    public static string PwaNotificationBody => Text(nameof(PwaNotificationBody));
    public static string PwaNotificationBodyPlaceholder => Text(nameof(PwaNotificationBodyPlaceholder));
    public static string PwaSendNotification => Text(nameof(PwaSendNotification));
    public static string PwaCancel => Text(nameof(PwaCancel));
    public static string PwaDefaultNotificationTitle => Text(nameof(PwaDefaultNotificationTitle));
    public static string PwaDefaultNotificationBody => Text(nameof(PwaDefaultNotificationBody));
    public static string PwaUpdateConfirm => Text(nameof(PwaUpdateConfirm));
    public static string ShowcaseTitle => Text(nameof(ShowcaseTitle));
    public static string ShowcaseSubtitle => Text(nameof(ShowcaseSubtitle));
    public static string ShowcaseTabGlobe1 => Text(nameof(ShowcaseTabGlobe1));
    public static string ShowcaseTabGlobe2 => Text(nameof(ShowcaseTabGlobe2));
    public static string ShowcaseTabMap => Text(nameof(ShowcaseTabMap));
    public static string ShowcaseTabControls => Text(nameof(ShowcaseTabControls));
    public static string ShowcaseTabForm => Text(nameof(ShowcaseTabForm));
    public static string ShowcaseTabData => Text(nameof(ShowcaseTabData));
    public static string ShowcaseGlobe1Title => Text(nameof(ShowcaseGlobe1Title));
    public static string ShowcaseGlobe1Note => Text(nameof(ShowcaseGlobe1Note));
    public static string ShowcasePointCount(params object?[] arguments) => Format(nameof(ShowcasePointCount), arguments);
    public static string ShowcaseGlobe2Title => Text(nameof(ShowcaseGlobe2Title));
    public static string ShowcaseGlobe2Note => Text(nameof(ShowcaseGlobe2Note));
    public static string ShowcaseGlobe2Independent => Text(nameof(ShowcaseGlobe2Independent));
    public static string ShowcaseMapTitle => Text(nameof(ShowcaseMapTitle));
    public static string ShowcaseMapNote(params object?[] arguments) => Format(nameof(ShowcaseMapNote), arguments);
    public static string ShowcaseControlsTitle => Text(nameof(ShowcaseControlsTitle));
    public static string ShowcaseControlsNote => Text(nameof(ShowcaseControlsNote));
    public static string ShowcaseControlsTab => Text(nameof(ShowcaseControlsTab));
    public static string ShowcaseFormTitle => Text(nameof(ShowcaseFormTitle));
    public static string ShowcaseFormNote => Text(nameof(ShowcaseFormNote));
    public static string ShowcaseFormNoteEnd => Text(nameof(ShowcaseFormNoteEnd));
    public static string ShowcaseFormHeading => Text(nameof(ShowcaseFormHeading));
    public static string ShowcaseFormAdded(params object?[] arguments) => Format(nameof(ShowcaseFormAdded), arguments);
    public static string ShowcasePwaTitle => Text(nameof(ShowcasePwaTitle));
    public static string ShowcasePwaNote => Text(nameof(ShowcasePwaNote));
    public static string ShowcasePwaShowStatus => Text(nameof(ShowcasePwaShowStatus));
    public static string ShowcaseDataTitle => Text(nameof(ShowcaseDataTitle));
    public static string ShowcaseDataNote => Text(nameof(ShowcaseDataNote));
    public static string ShowcaseContainer => Text(nameof(ShowcaseContainer));
    public static string ShowcasePurpose => Text(nameof(ShowcasePurpose));
    public static string ShowcasePoints => Text(nameof(ShowcasePoints));
    public static string ShowcaseFormContainer => Text(nameof(ShowcaseFormContainer));
    public static string ShowcaseExport => Text(nameof(ShowcaseExport));

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
        ["keyboardHint"] = MapKeyboardHint,
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

    /// <summary>
    /// Надписи 3D-глобуса для community-globe.js: подпись, подсказка про клавиши и то,
    /// что экранный диктор говорит о выбранном маркере.
    /// </summary>
    public static Dictionary<string, string> GlobeScriptTexts() => new()
    {
        ["canvasLabel"] = GlobeCanvasLabel,
        ["keyboardHint"] = GlobeKeyboardHint,
        ["pointFallback"] = MapPointFallback,
        ["clusterOne"] = MapClusterOne,
        ["clusterFew"] = MapClusterFew,
        ["clusterMany"] = MapClusterMany,
        ["clusterOther"] = MapClusterOther
    };

    private static string Text(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(name), arguments);
}

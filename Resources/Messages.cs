using System.Globalization;
using System.Resources;

namespace ZealousMindedPeopleGeo.Resources;

/// <summary>
/// Сообщения библиотеки на языке <see cref="CultureInfo.CurrentUICulture"/>; числа в
/// них форматируются по <see cref="CultureInfo.CurrentCulture"/>. Тексты лежат в
/// Messages.resx (английский, он же запасной для остальных языков) и
/// Messages.{язык}.resx. Новый язык — ещё один такой файл с теми же ключами.
/// </summary>
internal static class Messages
{
    private static readonly ResourceManager Resources = new(typeof(Messages));

    public static string PointIdRequired => Text(nameof(PointIdRequired));

    public static string PointIdTooLong(int maxLength) => Format(nameof(PointIdTooLong), maxLength);

    public static string PointLatitudeOutOfRange(string id, double latitude) =>
        Format(nameof(PointLatitudeOutOfRange), id, latitude);

    public static string PointLongitudeOutOfRange(string id, double longitude) =>
        Format(nameof(PointLongitudeOutOfRange), id, longitude);

    public static string PointFieldTooLong(string id, string field, int maxLength) =>
        Format(nameof(PointFieldTooLong), id, field, maxLength);

    /// <summary>«Feature 3» или «Feature 3 ('road')» — подлежащее остальных GeoJson*-сообщений.</summary>
    public static string GeoJsonFeature(int number, string? id) =>
        id is null ? Format(nameof(GeoJsonFeature), number) : Format("GeoJsonFeatureWithId", number, id);

    public static string GeoJsonNotFeature(string feature) => Format(nameof(GeoJsonNotFeature), feature);

    public static string GeoJsonNoGeometry(string feature) => Format(nameof(GeoJsonNoGeometry), feature);

    public static string GeoJsonNotPoint(string feature, string geometryType) =>
        Format(nameof(GeoJsonNotPoint), feature, geometryType);

    public static string GeoJsonNoCoordinates(string feature) => Format(nameof(GeoJsonNoCoordinates), feature);

    public static string GeoJsonBadCoordinates(string feature) => Format(nameof(GeoJsonBadCoordinates), feature);

    public static string GeoJsonBadPosition(string feature, int position) =>
        Format(nameof(GeoJsonBadPosition), feature, position);

    public static string GeoJsonInvalidPoint(string feature, string error) =>
        Format(nameof(GeoJsonInvalidPoint), feature, error);

    public static string GeoJsonNotFeatureCollection => Text(nameof(GeoJsonNotFeatureCollection));

    public static string GeoJsonNoFeatures => Text(nameof(GeoJsonNoFeatures));

    public static string JsonFileNotFound(string path) => Format(nameof(JsonFileNotFound), path);

    public static string JsonEmpty => Text(nameof(JsonEmpty));

    public static string JsonWrongShape => Text(nameof(JsonWrongShape));

    public static string JsonParseError(string error) => Format(nameof(JsonParseError), error);

    public static string ParticipantNoCoordinates(string name, Guid id) =>
        Format(nameof(ParticipantNoCoordinates), name, id);

    private static string Text(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(string name, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(name), arguments);
}

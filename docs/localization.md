# Язык сообщений

[← Документация](README.md) · Раздел для C# / Blazor

На каком языке компоненты пишут надписи и сообщения и как добавить перевод.

Надписи всех компонентов — формы, 2D-карты (вместе с кнопками и подсказками внутри неё),
глобуса и его панелей, `PwaManagerComponent` (и вопроса об обновлении приложения),
`LibraryShowcaseComponent`, — сообщения проверки полей формы, причины пропуска при импорте
(`GeoJsonReadResult.Skipped`, `ErrorMessage` загрузки JSON), ошибки `GeoPoint.Validate()` и
`JsonException` из `GeoPointGeoJson.Read` выходят на языке `CultureInfo.CurrentUICulture`, а
числа в сообщениях форматируются по `CultureInfo.CurrentCulture`. Переводы есть на
английский (он же для языков без перевода) и русский:

| `CurrentUICulture` | Сообщение |
|---|---|
| `ru`, `ru-RU` | Объект 1 («road»): геометрия LineString — не точка |
| `en` и остальные | Feature 1 ('road'): geometry LineString is not a point |

Параметры с текстом (`Title`, `ListTitle`, `SubmitButtonText`) по умолчанию тоже берут
перевод; заданное значение показывается как есть.

Язык выбирает приложение, например middleware локализации ASP.NET Core:

```csharp
app.UseRequestLocalization(new RequestLocalizationOptions
{
    // Язык браузера, если для него есть перевод, иначе русский. Формат чисел не меняется.
    DefaultRequestCulture = new RequestCulture(CultureInfo.CurrentCulture, new CultureInfo("ru")),
    SupportedCultures = new[] { CultureInfo.CurrentCulture },
    SupportedUICultures = new[] { new CultureInfo("ru"), new CultureInfo("en") }
});
```

Интерактивные компоненты Blazor Server работают через WebSocket, а в его запросе
заголовка `Accept-Language` может не быть. Поэтому выбранный при загрузке страницы язык
стоит запомнить в cookie, как это делает `App.razor` витрины. Подробности, в том числе
для WebAssembly, — в [документации ASP.NET Core](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization).

Новый язык — два файла с теми же ключами, что у английских: `Resources/Messages.<код>.resx`
(сообщения) и `Resources/Ui.<код>.resx` (надписи компонентов). Тест `MessagesTests` сверяет
ключи и подстановки `{0}` каждого перевода с английским, если добавить код языка в его
список `Translations`.

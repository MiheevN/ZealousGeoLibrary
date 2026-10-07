using System.Globalization;
using Microsoft.AspNetCore.Localization;
using ZealousMindedPeopleGeo;
using ZealousMindedPeopleGeo.Showcase.Components;

// Хост, чтобы открыть библиотеку глазами.
// dotnet run --project hosts/ZealousMindedPeopleGeo.Showcase
// http://localhost:5290  обзор, /map карта, /globe глобус, /showcase все компоненты

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Всё, что нужно компонентам библиотеки. Стили и скрипты она подключает сама.
builder.Services.AddZealousMindedPeopleGeo();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Сообщения библиотеки (например, почему объект GeoJSON не загрузился) выходят на
// языке CurrentUICulture: здесь — на языке браузера, если он есть среди переводов,
// иначе по-русски, как вся витрина; ?ui-culture=en в адресе выбирает язык явно.
// Формат чисел и дат не меняется. App.razor запоминает язык в cookie для схемы Blazor.
var messageLanguages = new[] { new CultureInfo("ru"), new CultureInfo("en") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.CurrentCulture, messageLanguages[0]),
    SupportedCultures = new[] { CultureInfo.CurrentCulture },
    SupportedUICultures = messageLanguages
});

// Необязательно: офлайн-кэш. Без этого заголовка сайт и установка PWA работают,
// но сервис-воркер библиотеки не регистрируется (см. README, раздел PWA).
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/_content/ZealousMindedPeopleGeo/sw.js", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Service-Worker-Allowed"] = "/";
        context.Response.Headers.CacheControl = "no-cache";
    }

    await next();
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

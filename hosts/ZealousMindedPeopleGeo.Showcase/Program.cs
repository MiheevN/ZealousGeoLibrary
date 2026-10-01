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

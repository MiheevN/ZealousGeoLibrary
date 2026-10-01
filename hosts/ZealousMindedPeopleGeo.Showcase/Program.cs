using ZealousMindedPeopleGeo;
using ZealousMindedPeopleGeo.Showcase.Components;

// Хост, чтобы открыть библиотеку глазами.
// dotnet run --project hosts/ZealousMindedPeopleGeo.Showcase
// http://localhost:5290  обзор, /map карта, /globe глобус, /showcase все компоненты

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddZealousMindedPeopleGeoServices();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// sw.js библиотеки лежит в /_content/..., а управлять должен всем сайтом (см. README, раздел PWA).
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

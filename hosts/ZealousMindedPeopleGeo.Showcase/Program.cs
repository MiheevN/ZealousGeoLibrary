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
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

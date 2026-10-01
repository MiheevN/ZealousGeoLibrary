using ZealousMindedPeopleGeo;
using ZealousMindedPeopleGeo.Showcase.Components;

// Хост, чтобы открыть библиотеку глазами.
// dotnet run --project hosts/ZealousMindedPeopleGeo.Showcase
// http://localhost:5290  витрина, /globe один глобус, /map карта

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddZealousMindedPeopleGeoServices();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

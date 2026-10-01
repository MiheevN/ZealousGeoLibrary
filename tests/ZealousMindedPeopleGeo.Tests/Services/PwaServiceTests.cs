using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Services;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// PwaService ходит в браузер только через модуль js/pwa.js: eval с return в начале скрипта падал с SyntaxError.
/// </summary>
public class PwaServiceTests
{
    [Fact]
    public async Task Methods_CallPwaModuleFunctions_InsteadOfEval()
    {
        var module = new FakeModule
        {
            ["getInstallInfo"] = new PwaInstallInfo { CanInstall = true, IsSupported = true },
            ["getCacheInfo"] = new PwaCacheInfo
            {
                TotalSize = 2048,
                CacheCount = 1,
                Caches = { new CacheDetails { Name = "zealous-geo-static-v1.0.0", Size = 2048, ItemCount = 3 } }
            },
            ["sendNotification"] = true,
        };
        var jsRuntime = new FakeJsRuntime(module);
        await using var service = new PwaService(jsRuntime, NullLogger<PwaService>.Instance);

        var installInfo = await service.GetInstallInfoAsync();
        var cacheInfo = await service.GetCacheInfoAsync();
        var sent = await service.SendNotificationAsync("Заголовок с 'кавычками'", "Текст");
        await service.UpdateServiceWorkerAsync();

        Assert.True(installInfo.CanInstall);
        Assert.True(installInfo.IsSupported);
        Assert.Equal(2048, cacheInfo.TotalSize);
        Assert.Equal("zealous-geo-static-v1.0.0", Assert.Single(cacheInfo.Caches).Name);
        Assert.True(sent);

        Assert.Equal(new[] { "import" }, jsRuntime.Identifiers);
        Assert.Equal("/_content/ZealousMindedPeopleGeo/js/pwa.js", jsRuntime.ImportedPath);
        Assert.Equal(new[] { "getInstallInfo", "getCacheInfo", "sendNotification", "updateServiceWorker" }, module.Identifiers);
        Assert.Equal("Заголовок с 'кавычками'", module.Args["sendNotification"]?[0]);
    }

    private sealed class FakeJsRuntime(FakeModule module) : IJSRuntime
    {
        public List<string> Identifiers { get; } = new();
        public string? ImportedPath { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifiers.Add(identifier);
            if (identifier != "import")
            {
                throw new InvalidOperationException($"Unexpected global JS call '{identifier}'.");
            }

            ImportedPath = args?[0] as string;
            return ValueTask.FromResult((TValue)(object)module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class FakeModule : Dictionary<string, object>, IJSObjectReference
    {
        public List<string> Identifiers { get; } = new();
        public Dictionary<string, object?[]?> Args { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifiers.Add(identifier);
            Args[identifier] = args;
            return ValueTask.FromResult(TryGetValue(identifier, out var result) ? (TValue)result : default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

using System.Reflection;
using Microsoft.Extensions.Logging;
using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseYandexMapKit(ApiKey ?? "MISSING-API-KEY", locale: "ru_RU");

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    /// <summary>Key passed at build time (see the sample .csproj); <see langword="null"/> if missing.</summary>
    public static string? ApiKey { get; } = typeof(MauiProgram).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "YandexMapKitApiKey")?.Value is { Length: > 0 } key ? key : null;
}

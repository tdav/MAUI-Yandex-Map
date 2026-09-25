using Microsoft.Maui.LifecycleEvents;

namespace Yandex.MapKit.Maui;

public static class AppHostBuilderExtensions
{
    /// <summary>Registers <see cref="YandexMapView"/> and wires MapKit into the app lifecycle.</summary>
    /// <param name="apiKey">MapKit Mobile SDK key. Do not commit it: pass it from a build secret.</param>
    /// <param name="locale">Map language, e.g. <c>"ru_RU"</c>; <see langword="null"/> — system locale.</param>
    public static MauiAppBuilder UseYandexMapKit(this MauiAppBuilder builder, string apiKey, string? locale = null) =>
        builder.UseYandexMapKit(options =>
        {
            options.ApiKey = apiKey;
            options.Locale = locale;
        });

    public static MauiAppBuilder UseYandexMapKit(this MauiAppBuilder builder, Action<YandexMapKitOptions> configure)
    {
        var options = new YandexMapKitOptions();
        configure(options);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("Yandex MapKit API key is required.", nameof(configure));

        YandexMapKitOptions.Current = options;

        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<YandexMapView, YandexMapViewHandler>());
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnStart(_ => YandexMapKitLifecycle.OnActivityStarted())
                .OnStop(_ => YandexMapKitLifecycle.OnActivityStopped()));
#elif IOS
            // Before FinishedLaunching creates the window (and therefore the map handlers).
            events.AddiOS(ios => ios.WillFinishLaunching((_, _) =>
            {
                YandexMapKitLifecycle.EnsureInitialized(duringLaunch: true);
                return true;
            }));
#endif
        });

        return builder;
    }
}

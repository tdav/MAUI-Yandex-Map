using YandexMapsMobile;

namespace Yandex.MapKit.Maui;

/// <summary>Initializes MapKit once per process (API key and locale first).</summary>
internal static class YandexMapKitLifecycle
{
    private static bool _initialized;

    /// <param name="duringLaunch">
    /// <see langword="true"/> when called from the app launch callbacks: MapKit then follows the application
    /// lifecycle by itself. Initialized later, it has to be started explicitly.
    /// </param>
    public static void EnsureInitialized(bool duringLaunch = false)
    {
        if (_initialized)
            return;

        var options = YandexMapKitOptions.Current;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Yandex MapKit is not configured: call builder.UseYandexMapKit(apiKey) in MauiProgram.");

        YMKMapKit.SetApiKey(options.ApiKey);
        if (!string.IsNullOrEmpty(options.Locale))
            YMKMapKit.SetLocale(options.Locale);

        var mapKit = YMKMapKit.SharedInstance;
        _initialized = true;

        if (!duringLaunch)
            mapKit.OnStart();
    }
}

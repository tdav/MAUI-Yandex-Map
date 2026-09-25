using Android.Content;
using Com.Yandex.Mapkit;
using AMapView = Com.Yandex.Mapkit.Mapview.MapView;

namespace Yandex.MapKit.Maui;

/// <summary>
/// One MapKit instance per process: initializes it lazily (API key and locale first) and forwards
/// Activity start/stop to MapKit and every live <see cref="AMapView"/>. Without onStart the map is an empty grid.
/// </summary>
internal static class YandexMapKitLifecycle
{
    private static readonly List<WeakReference<AMapView>> Views = [];
    private static bool _initialized;
    private static bool _activityStarted;
    private static bool _mapKitStarted;

    public static void EnsureInitialized(Context context)
    {
        if (_initialized)
            return;

        var options = YandexMapKitOptions.Current;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Yandex MapKit is not configured: call builder.UseYandexMapKit(apiKey) in MauiProgram.");

        // setApiKey/setLocale must precede initialize().
        MapKitFactory.SetApiKey(options.ApiKey);
        if (!string.IsNullOrEmpty(options.Locale))
            MapKitFactory.SetLocale(options.Locale);
        MapKitFactory.Initialize(context.ApplicationContext ?? context);
        _initialized = true;

        // Maps created after the Activity has already started (e.g. on a pushed page).
        if (_activityStarted)
            StartMapKit();
    }

    public static void Attach(AMapView view)
    {
        Prune();
        Views.Add(new WeakReference<AMapView>(view));
        if (_activityStarted)
            view.OnStart();
    }

    public static void Detach(AMapView view)
    {
        Views.RemoveAll(w => !w.TryGetTarget(out var target) || ReferenceEquals(target, view));
        if (_activityStarted)
            view.OnStop();
    }

    public static void OnActivityStarted()
    {
        _activityStarted = true;
        if (!_initialized)
            return;

        StartMapKit();
        foreach (var view in LiveViews())
            view.OnStart();
    }

    public static void OnActivityStopped()
    {
        _activityStarted = false;
        if (!_initialized)
            return;

        foreach (var view in LiveViews())
            view.OnStop();
        if (_mapKitStarted)
        {
            MapKitFactory.Instance?.OnStop();
            _mapKitStarted = false;
        }
    }

    private static void StartMapKit()
    {
        if (_mapKitStarted)
            return;

        MapKitFactory.Instance?.OnStart();
        _mapKitStarted = true;
    }

    private static List<AMapView> LiveViews()
    {
        Prune();
        var result = new List<AMapView>(Views.Count);
        foreach (var weak in Views)
        {
            if (weak.TryGetTarget(out var view))
                result.Add(view);
        }

        return result;
    }

    private static void Prune() => Views.RemoveAll(w => !w.TryGetTarget(out _));
}

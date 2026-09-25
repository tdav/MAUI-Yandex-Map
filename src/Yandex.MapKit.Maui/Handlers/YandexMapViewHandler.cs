using Microsoft.Maui.Handlers;
#if ANDROID
using PlatformView = Com.Yandex.Mapkit.Mapview.MapView;
#elif IOS
using PlatformView = YandexMapsMobile.YMKMapView;
#else
using PlatformView = System.Object;
#endif

namespace Yandex.MapKit.Maui;

/// <summary>Connects <see cref="YandexMapView"/> to the native MapKit view.</summary>
public partial class YandexMapViewHandler : ViewHandler<YandexMapView, PlatformView>
{
    public static readonly IPropertyMapper<YandexMapView, YandexMapViewHandler> Mapper =
        new PropertyMapper<YandexMapView, YandexMapViewHandler>(ViewMapper)
        {
            [nameof(YandexMapView.Center)] = MapCamera,
            [nameof(YandexMapView.Zoom)] = MapCamera,
            [nameof(YandexMapView.Azimuth)] = MapCamera,
            [nameof(YandexMapView.Tilt)] = MapCamera,
            [nameof(YandexMapView.IsNightModeEnabled)] = MapNightMode,
            [nameof(YandexMapView.IsShowingUser)] = MapShowingUser,
            [nameof(YandexMapView.IsZoomEnabled)] = MapGestures,
            [nameof(YandexMapView.IsScrollEnabled)] = MapGestures,
            [nameof(YandexMapView.IsRotateEnabled)] = MapGestures,
            [nameof(YandexMapView.IsTiltEnabled)] = MapGestures,
            [nameof(YandexMapView.IsClusteringEnabled)] = MapClustering,
            [nameof(YandexMapView.ClusterRadius)] = MapClustering,
            [nameof(YandexMapView.ClusterMinZoom)] = MapClustering,
            [nameof(YandexMapView.ClusterColor)] = MapClustering,
        };

    public static readonly CommandMapper<YandexMapView, YandexMapViewHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(YandexMapView.MoveTo)] = MapMoveTo,
        };

    public YandexMapViewHandler() : base(Mapper, CommandMapper)
    {
    }

    public YandexMapViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
    }

    public static void MapCamera(YandexMapViewHandler handler, YandexMapView view)
    {
        if (!view.IsCameraMappingSuppressed)
            handler.MoveCamera(view.CameraPosition, animated: false);
    }

    public static void MapMoveTo(YandexMapViewHandler handler, YandexMapView view, object? arg)
    {
        if (arg is MoveToRequest request)
            handler.MoveCamera(request.Position, request.Animated);
    }

    public static void MapNightMode(YandexMapViewHandler handler, YandexMapView view) => handler.UpdateNightMode();

    public static void MapShowingUser(YandexMapViewHandler handler, YandexMapView view) => handler.UpdateShowingUser();

    public static void MapGestures(YandexMapViewHandler handler, YandexMapView view) => handler.UpdateGestures();

    public static void MapClustering(YandexMapViewHandler handler, YandexMapView view) => handler.UpdateClustering();

    private partial void MoveCamera(MapCameraPosition position, bool animated);
    private partial void UpdateNightMode();
    private partial void UpdateShowingUser();
    private partial void UpdateGestures();
    private partial void UpdateClustering();
}

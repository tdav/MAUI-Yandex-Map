using CoreGraphics;
using ObjCRuntime;
using YandexMapsMobile;

namespace Yandex.MapKit.Maui;

public partial class YandexMapViewHandler
{
    private const float AnimationDuration = 0.4f;

    // MapKit keeps listeners weakly: the handler must hold them strongly or they are collected.
    private InputListener? _inputListener;
    private CameraListener? _cameraListener;
    private YMKUserLocationLayer? _userLocationLayer;
    private YMKMapObjectCollection? _elementsCollection;
    private YMKMapObjectCollection? _pinsCollection;
    private PinAdapter? _pinAdapter;
    private CollectionSynchronizer<MapPin>? _pinSync;
    private CollectionSynchronizer<MapElement>? _elementSync;

    internal MapObjectTapListener? MapObjectTapListener { get; private set; }
    internal ClusterListener? ClusterListener { get; private set; }
    internal ClusterTapListener? ClusterTapListener { get; private set; }
    internal MarkerImages? Images { get; private set; }

    private YMKMap? Map => PlatformView?.MapWindow?.Map;

    protected override YMKMapView CreatePlatformView()
    {
        YandexMapKitLifecycle.EnsureInitialized();

        // OpenGL is not available on the simulator for Apple silicon: MapKit needs its Vulkan (Metal) renderer there.
        return Runtime.Arch == Arch.SIMULATOR
            ? new YMKMapView(CGRect.Empty, vulkanPreferred: true, transparencySupport: false)
            : new YMKMapView(CGRect.Empty);
    }

    protected override void ConnectHandler(YMKMapView platformView)
    {
        base.ConnectHandler(platformView);

        var map = platformView.MapWindow.Map;
        Images = new MarkerImages(MauiContext!);

        _inputListener = new InputListener(this);
        _cameraListener = new CameraListener(this);
        MapObjectTapListener = new MapObjectTapListener(this);
        ClusterListener = new ClusterListener(this);
        ClusterTapListener = new ClusterTapListener(this);
        map.AddInputListener(_inputListener);
        map.AddCameraListener(_cameraListener);

        // Shapes below pins: collections are drawn in insertion order.
        _elementsCollection = map.MapObjects.AddCollection();
        _pinsCollection = map.MapObjects.AddCollection();
        _elementSync = new CollectionSynchronizer<MapElement>(VirtualView.MapElements, new ElementAdapter(this, _elementsCollection));
        _pinAdapter = new PinAdapter(this, _pinsCollection, VirtualView.IsClusteringEnabled);
        _pinSync = new CollectionSynchronizer<MapPin>(VirtualView.Pins, _pinAdapter);
    }

    protected override void DisconnectHandler(YMKMapView platformView)
    {
        _pinSync?.Dispose();
        _elementSync?.Dispose();
        _pinAdapter?.Dispose();
        _pinSync = null;
        _elementSync = null;
        _pinAdapter = null;

        var map = platformView.MapWindow?.Map;
        if (map is { IsValid: true })
        {
            if (_inputListener is not null)
                map.RemoveInputListener(_inputListener);
            if (_cameraListener is not null)
                map.RemoveCameraListener(_cameraListener);
            map.MapObjects.Clear();
        }

        if (_userLocationLayer is { IsValid: true })
            _userLocationLayer.SetVisible(false);
        _userLocationLayer = null;

        _inputListener = null;
        _cameraListener = null;
        MapObjectTapListener = null;
        ClusterListener = null;
        ClusterTapListener = null;
        Images = null;
        _elementsCollection = null;
        _pinsCollection = null;

        base.DisconnectHandler(platformView);
    }

    private partial void MoveCamera(MapCameraPosition position, bool animated)
    {
        if (Map is not { } map)
            return;

        var camera = YMKCameraPosition.Create(
            YMKPoint.Create(position.Target.Latitude, position.Target.Longitude),
            (float)position.Zoom, (float)position.Azimuth, (float)position.Tilt);

        if (animated)
            map.Move(camera, YMKAnimation.Create(YMKAnimationType.Smooth, AnimationDuration), null);
        else
            map.Move(camera);
    }

    private partial void UpdateNightMode()
    {
        if (Map is { } map)
            map.NightModeEnabled = VirtualView.IsNightModeEnabled;
    }

    private partial void UpdateGestures()
    {
        if (Map is not { } map)
            return;

        map.ZoomGesturesEnabled = VirtualView.IsZoomEnabled;
        map.ScrollGesturesEnabled = VirtualView.IsScrollEnabled;
        map.RotateGesturesEnabled = VirtualView.IsRotateEnabled;
        map.TiltGesturesEnabled = VirtualView.IsTiltEnabled;
    }

    private partial void UpdateShowingUser()
    {
        if (PlatformView?.MapWindow is not { } window)
            return;

        if (VirtualView.IsShowingUser)
        {
            _userLocationLayer ??= YMKMapKit.SharedInstance.CreateUserLocationLayer(window);
            _userLocationLayer.SetVisible(true);
        }
        else
        {
            _userLocationLayer?.SetVisible(false);
        }
    }

    private partial void UpdateClustering()
    {
        if (_pinSync is null || _pinAdapter is null || _pinsCollection is null)
            return;

        if (_pinAdapter.IsClustered != VirtualView.IsClusteringEnabled)
        {
            var old = _pinAdapter;
            _pinAdapter = new PinAdapter(this, _pinsCollection, VirtualView.IsClusteringEnabled);
            _pinSync.Reset(_pinAdapter);
            old.Dispose();
        }
        else
        {
            // Radius, min zoom or badge color changed: clusters are rebuilt (and re-styled) by clusterPlacemarks.
            _pinAdapter.Recluster();
        }
    }

    internal static GeoPoint ToGeoPoint(YMKPoint point) => new(point.Latitude, point.Longitude);

    private sealed class InputListener(YandexMapViewHandler handler) : YMKMapInputListener
    {
        public override void OnMapTap(YMKMap map, YMKPoint point) =>
            handler.VirtualView?.OnPlatformMapClicked(ToGeoPoint(point));

        public override void OnMapLongTap(YMKMap map, YMKPoint point) =>
            handler.VirtualView?.OnPlatformMapLongClicked(ToGeoPoint(point));
    }

    private sealed class CameraListener(YandexMapViewHandler handler) : YMKMapCameraListener
    {
        public override void OnCameraPositionChanged(YMKMap map, YMKCameraPosition cameraPosition, YMKCameraUpdateReason cameraUpdateReason, bool finished) =>
            handler.VirtualView?.OnPlatformCameraChanged(
                new MapCameraPosition(ToGeoPoint(cameraPosition.Target), cameraPosition.Zoom, cameraPosition.Azimuth, cameraPosition.Tilt),
                cameraUpdateReason == YMKCameraUpdateReason.Gestures ? CameraChangeReason.Gestures : CameraChangeReason.Application,
                finished);
    }
}

internal sealed class MapObjectTapListener(YandexMapViewHandler handler) : YMKMapObjectTapListener
{
    public override bool OnMapObjectTap(YMKMapObject mapObject, YMKPoint point) =>
        handler.VirtualView switch
        {
            { } view when mapObject.UserData is PinTag pin => view.OnPlatformPinClicked(pin.Pin),
            { } view when mapObject.UserData is ElementTag element =>
                view.OnPlatformElementClicked(element.Element, YandexMapViewHandler.ToGeoPoint(point)),
            _ => false,
        };
}

internal sealed class ClusterListener(YandexMapViewHandler handler) : YMKClusterListener
{
    public override void OnClusterAdded(YMKCluster cluster)
    {
        if (handler.VirtualView is not { } view || handler.Images is not { } images || handler.ClusterTapListener is not { } tap)
            return;

        cluster.Appearance.SetIcon(images.Cluster(view.ClusterColor, cluster.Size));
        cluster.Appearance.ZIndex = 100;
        cluster.AddClusterTapListener(tap);
    }
}

internal sealed class ClusterTapListener(YandexMapViewHandler handler) : YMKClusterTapListener
{
    public override bool OnClusterTap(YMKCluster cluster)
    {
        if (handler.VirtualView is not { } view)
            return false;

        var pins = cluster.Placemarks
            .Select(p => (p.UserData as PinTag)?.Pin)
            .OfType<MapPin>()
            .ToList();
        view.OnPlatformClusterClicked(pins, YandexMapViewHandler.ToGeoPoint(cluster.Appearance.Geometry));
        return true;
    }
}

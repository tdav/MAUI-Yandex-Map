using Com.Yandex.Mapkit;
using Com.Yandex.Mapkit.Map;
using Com.Yandex.Mapkit.UserLocation;
using WeakReference = Java.Lang.Ref.WeakReference;
using AMapView = Com.Yandex.Mapkit.Mapview.MapView;
using YAnimation = Com.Yandex.Mapkit.Animation;
using YCameraPosition = Com.Yandex.Mapkit.Map.CameraPosition;
using IMap = Com.Yandex.Mapkit.Map.IMap;
using YPoint = Com.Yandex.Mapkit.Geometry.Point;

namespace Yandex.MapKit.Maui;

public partial class YandexMapViewHandler
{
    private const float AnimationDuration = 0.4f;

    // MapKit keeps listeners in WeakReferences: the handler must hold them strongly or they are collected.
    private InputListener? _inputListener;
    private CameraListener? _cameraListener;
    private IUserLocationLayer? _userLocationLayer;
    private IMapObjectCollection? _elementsCollection;
    private IMapObjectCollection? _pinsCollection;
    private PinAdapter? _pinAdapter;
    private CollectionSynchronizer<MapPin>? _pinSync;
    private CollectionSynchronizer<MapElement>? _elementSync;

    internal MapObjectTapListener? MapObjectTapListener { get; private set; }
    internal ClusterListener? ClusterListener { get; private set; }
    internal MarkerImages? Images { get; private set; }

    private IMap? Map => PlatformView?.MapWindow?.Map;

    protected override AMapView CreatePlatformView()
    {
        YandexMapKitLifecycle.EnsureInitialized(Context);
        return new AMapView(Context);
    }

    protected override void ConnectHandler(AMapView platformView)
    {
        base.ConnectHandler(platformView);

        var map = platformView.MapWindow!.Map;
        Images = new MarkerImages(Context, MauiContext!);

        _inputListener = new InputListener(this);
        _cameraListener = new CameraListener(this);
        MapObjectTapListener = new MapObjectTapListener(this);
        ClusterListener = new ClusterListener(this);
        map.AddInputListener(new WeakReference(_inputListener));
        map.AddCameraListener(new WeakReference(_cameraListener));

        // Shapes below pins: collections are drawn in insertion order.
        _elementsCollection = map.MapObjects.AddCollection();
        _pinsCollection = map.MapObjects.AddCollection();
        _elementSync = new CollectionSynchronizer<MapElement>(VirtualView.MapElements, new ElementAdapter(this, _elementsCollection));
        _pinAdapter = new PinAdapter(this, _pinsCollection, VirtualView.IsClusteringEnabled);
        _pinSync = new CollectionSynchronizer<MapPin>(VirtualView.Pins, _pinAdapter);

        YandexMapKitLifecycle.Attach(platformView);
    }

    protected override void DisconnectHandler(AMapView platformView)
    {
        _pinSync?.Dispose();
        _elementSync?.Dispose();
        _pinAdapter?.Dispose();
        _pinSync = null;
        _elementSync = null;
        _pinAdapter = null;

        var map = platformView.MapWindow?.Map;
        if (map is not null)
        {
            if (_inputListener is not null)
                map.RemoveInputListener(new WeakReference(_inputListener));
            if (_cameraListener is not null)
                map.RemoveCameraListener(new WeakReference(_cameraListener));
            map.MapObjects.Clear();
        }

        if (_userLocationLayer is { IsValid: true })
            _userLocationLayer.Visible = false;
        _userLocationLayer = null;

        YandexMapKitLifecycle.Detach(platformView);

        _inputListener = null;
        _cameraListener = null;
        MapObjectTapListener = null;
        ClusterListener = null;
        Images = null;
        _elementsCollection = null;
        _pinsCollection = null;

        base.DisconnectHandler(platformView);
    }

    private partial void MoveCamera(MapCameraPosition position, bool animated)
    {
        if (Map is not { } map)
            return;

        var camera = new YCameraPosition(
            new YPoint(position.Target.Latitude, position.Target.Longitude),
            (float)position.Zoom, (float)position.Azimuth, (float)position.Tilt);

        if (animated)
            map.Move(camera, new YAnimation(YAnimation.Type.Smooth!, AnimationDuration), null);
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
            _userLocationLayer ??= MapKitFactory.Instance!.CreateUserLocationLayer(window);
            _userLocationLayer.Visible = true;
        }
        else if (_userLocationLayer is not null)
        {
            _userLocationLayer.Visible = false;
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

    internal static GeoPoint ToGeoPoint(YPoint point) => new(point.Latitude, point.Longitude);

    internal sealed class InputListener(YandexMapViewHandler handler) : Java.Lang.Object, IInputListener
    {
        public void OnMapTap(IMap map, YPoint point) => handler.VirtualView?.OnPlatformMapClicked(ToGeoPoint(point));

        public void OnMapLongTap(IMap map, YPoint point) => handler.VirtualView?.OnPlatformMapLongClicked(ToGeoPoint(point));
    }

    internal sealed class CameraListener(YandexMapViewHandler handler) : Java.Lang.Object, ICameraListener
    {
        public void OnCameraPositionChanged(IMap map, YCameraPosition position, CameraUpdateReason reason, bool finished)
        {
            var target = position.Target;
            handler.VirtualView?.OnPlatformCameraChanged(
                new MapCameraPosition(ToGeoPoint(target), position.Zoom, position.Azimuth, position.Tilt),
                reason == CameraUpdateReason.Gestures ? CameraChangeReason.Gestures : CameraChangeReason.Application,
                finished);
        }
    }
}

internal sealed class MapObjectTapListener(YandexMapViewHandler handler) : Java.Lang.Object, IMapObjectTapListener
{
    public bool OnMapObjectTap(IMapObject mapObject, YPoint point) =>
        handler.VirtualView switch
        {
            { } view when mapObject.UserData is PinTag pin => view.OnPlatformPinClicked(pin.Pin),
            { } view when mapObject.UserData is ElementTag element =>
                view.OnPlatformElementClicked(element.Element, YandexMapViewHandler.ToGeoPoint(point)),
            _ => false,
        };
}

internal sealed class ClusterListener(YandexMapViewHandler handler) : Java.Lang.Object, IClusterListener, IClusterTapListener
{
    public void OnClusterAdded(ICluster cluster)
    {
        if (handler.VirtualView is not { } view || handler.Images is not { } images)
            return;

        cluster.Appearance.SetIcon(images.Cluster(view.ClusterColor, cluster.Size));
        cluster.Appearance.ZIndex = 100;
        cluster.AddClusterTapListener(new WeakReference(this));
    }

    public bool OnClusterTap(ICluster cluster)
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

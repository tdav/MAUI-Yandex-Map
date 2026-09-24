using PointF = Android.Graphics.PointF;
using Com.Yandex.Mapkit.Map;
using WeakReference = Java.Lang.Ref.WeakReference;
using YPoint = Com.Yandex.Mapkit.Geometry.Point;

namespace Yandex.MapKit.Maui;

/// <summary>Keeps <see cref="MapPin"/>s in sync with placemarks, either in a plain or a clusterized collection.</summary>
internal sealed class PinAdapter : IMapObjectAdapter<MapPin>, IDisposable
{
    private readonly YandexMapViewHandler _handler;
    private readonly IMapObjectCollection _parent;
    private readonly IMapObjectCollection? _plain;
    private readonly IClusterizedPlacemarkCollection? _clusterized;
    private readonly Dictionary<MapPin, IPlacemarkMapObject> _placemarks = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<MapPin, int> _iconVersions = new(ReferenceEqualityComparer.Instance);
    private readonly Android.OS.Handler _mainThread = new(Android.OS.Looper.MainLooper!);
    private bool _reclusterPending;
    private bool _disposed;

    public PinAdapter(YandexMapViewHandler handler, IMapObjectCollection parent, bool clustered)
    {
        _handler = handler;
        _parent = parent;
        if (clustered)
            _clusterized = parent.AddClusterizedPlacemarkCollection(new WeakReference(handler.ClusterListener));
        else
            _plain = parent.AddCollection();
    }

    public bool IsClustered => _clusterized is not null;

    public void Add(MapPin pin)
    {
        var placemark = _clusterized?.AddPlacemark() ?? _plain!.AddPlacemark();
        placemark.Geometry = ToPoint(pin.Location);
        placemark.UserData = new PinTag(pin);
        placemark.AddTapListener(new WeakReference(_handler.MapObjectTapListener));
        _placemarks[pin] = placemark;

        ApplyIcon(pin, placemark);
        ApplyLabel(pin, placemark);
        placemark.ZIndex = (float)pin.ZIndex;
        placemark.Visible = pin.IsVisible;
        ScheduleRecluster();
    }

    public void Update(MapPin pin, string? propertyName)
    {
        if (!_placemarks.TryGetValue(pin, out var placemark))
            return;

        switch (propertyName)
        {
            case nameof(MapPin.Location):
                placemark.Geometry = ToPoint(pin.Location);
                ScheduleRecluster();
                break;
            case nameof(MapPin.Label):
                ApplyLabel(pin, placemark);
                break;
            case nameof(MapPin.Icon) or nameof(MapPin.Color) or nameof(MapPin.Anchor) or nameof(MapPin.IconScale):
                ApplyIcon(pin, placemark);
                break;
            case nameof(MapPin.ZIndex):
                placemark.ZIndex = (float)pin.ZIndex;
                break;
            case nameof(MapPin.IsVisible):
                placemark.Visible = pin.IsVisible;
                ScheduleRecluster();
                break;
        }
    }

    public void Remove(MapPin pin)
    {
        if (!_placemarks.Remove(pin, out var placemark))
            return;

        _iconVersions.Remove(pin);
        if (placemark.IsValid)
        {
            if (_clusterized is not null)
                _clusterized.Remove(placemark);
            else
                _plain!.Remove(placemark);
        }

        ScheduleRecluster();
    }

    public void Clear()
    {
        _placemarks.Clear();
        _iconVersions.Clear();
        if (_clusterized is not null)
            _clusterized.Clear();
        else
            _plain!.Clear();
    }

    /// <summary>Re-runs clustering, e.g. after <see cref="YandexMapView.ClusterRadius"/> or the cluster color changed.</summary>
    public void Recluster()
    {
        _reclusterPending = false;
        if (_disposed || _clusterized is null || !_clusterized.IsValid)
            return;

        var view = _handler.VirtualView;
        if (view is not null)
            _clusterized.ClusterPlacemarks(view.ClusterRadius, view.ClusterMinZoom);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _placemarks.Clear();
        IMapObject? container = (IMapObject?)_clusterized ?? _plain;
        if (_parent.IsValid && container is not null)
            _parent.Remove(container);
    }

    // Batch many Add/Remove calls (e.g. loading a list) into a single clusterPlacemarks.
    private void ScheduleRecluster()
    {
        if (_clusterized is null || _reclusterPending)
            return;

        _reclusterPending = true;
        _mainThread.Post(Recluster);
    }

    private void ApplyLabel(MapPin pin, IPlacemarkMapObject placemark)
    {
        var style = new TextStyle()
            .SetSize(12)!
            .SetColor(Android.Graphics.Color.Black.ToArgb())!
            .SetOutlineColor(Android.Graphics.Color.White.ToArgb())!
            .SetPlacement(TextStyle.Placement.Bottom!)!;
        placemark.SetText(pin.Label ?? string.Empty, style);
    }

    private async void ApplyIcon(MapPin pin, IPlacemarkMapObject placemark)
    {
        var version = _iconVersions[pin] = _iconVersions.GetValueOrDefault(pin) + 1;
        var style = new IconStyle()
            .SetAnchor(new PointF((float)pin.Anchor.X, (float)pin.Anchor.Y))!
            .SetScale(Java.Lang.Float.ValueOf((float)pin.IconScale))!;

        if (pin.Icon is null)
        {
            placemark.SetIcon(_handler.Images!.DefaultPin(pin.Color), style);
            return;
        }

        var provider = await _handler.Images!.Icon(pin.Icon);

        // The pin may have been removed or its icon changed while the image was loading.
        if (_disposed || _iconVersions.GetValueOrDefault(pin) != version || !placemark.IsValid)
            return;

        placemark.SetIcon(provider ?? _handler.Images.DefaultPin(pin.Color), style);
    }

    internal static YPoint ToPoint(GeoPoint point) => new(point.Latitude, point.Longitude);
}

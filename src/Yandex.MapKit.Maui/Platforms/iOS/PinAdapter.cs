using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;
using YandexMapsMobile;

namespace Yandex.MapKit.Maui;

/// <summary>Keeps <see cref="MapPin"/>s in sync with placemarks, either in a plain or a clusterized collection.</summary>
internal sealed class PinAdapter : IMapObjectAdapter<MapPin>, IDisposable
{
    private readonly YandexMapViewHandler _handler;
    private readonly YMKMapObjectCollection _parent;
    private readonly YMKMapObjectCollection? _plain;
    private readonly YMKClusterizedPlacemarkCollection? _clusterized;
    private readonly Dictionary<MapPin, YMKPlacemarkMapObject> _placemarks = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<MapPin, int> _iconVersions = new(ReferenceEqualityComparer.Instance);
    private bool _reclusterPending;
    private bool _disposed;

    public PinAdapter(YandexMapViewHandler handler, YMKMapObjectCollection parent, bool clustered)
    {
        _handler = handler;
        _parent = parent;
        if (clustered)
            _clusterized = parent.AddClusterizedPlacemarkCollection(handler.ClusterListener!);
        else
            _plain = parent.AddCollection();
    }

    public bool IsClustered => _clusterized is not null;

    public void Add(MapPin pin)
    {
        var placemark = _clusterized?.AddPlacemark() ?? _plain!.AddPlacemark();
        placemark.Geometry = ToPoint(pin.Location);
        placemark.UserData = new PinTag(pin);
        placemark.AddTapListener(_handler.MapObjectTapListener!);
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
            _clusterized.ClusterPlacemarks(view.ClusterRadius, (nuint)Math.Max(0, view.ClusterMinZoom));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _placemarks.Clear();
        YMKMapObject? container = (YMKMapObject?)_clusterized ?? _plain;
        if (_parent.IsValid && container is not null)
            _parent.Remove(container);
    }

    // Batch many Add/Remove calls (e.g. loading a list) into a single clusterPlacemarks.
    private void ScheduleRecluster()
    {
        if (_clusterized is null || _reclusterPending)
            return;

        _reclusterPending = true;
        DispatchQueue.MainQueue.DispatchAsync(Recluster);
    }

    private static void ApplyLabel(MapPin pin, YMKPlacemarkMapObject placemark)
    {
        var style = new YMKTextStyle
        {
            Size = 12,
            Color = UIColor.Black,
            OutlineColor = UIColor.White,
            OutlineWidth = 1,
            Placement = YMKTextStylePlacement.Bottom,
        };
        placemark.SetText(pin.Label ?? string.Empty, style);
    }

    private async void ApplyIcon(MapPin pin, YMKPlacemarkMapObject placemark)
    {
        var version = _iconVersions[pin] = _iconVersions.GetValueOrDefault(pin) + 1;
        var style = new YMKIconStyle
        {
            Anchor = NSValue.FromCGPoint(new CGPoint(pin.Anchor.X, pin.Anchor.Y)),
            Scale = NSNumber.FromFloat((float)pin.IconScale),
        };

        if (pin.Icon is null)
        {
            placemark.SetIcon(_handler.Images!.DefaultPin(pin.Color), style);
            return;
        }

        var image = await _handler.Images!.Icon(pin.Icon);

        // The pin may have been removed or its icon changed while the image was loading.
        if (_disposed || _iconVersions.GetValueOrDefault(pin) != version || !placemark.IsValid)
            return;

        placemark.SetIcon(image ?? _handler.Images.DefaultPin(pin.Color), style);
    }

    internal static YMKPoint ToPoint(GeoPoint point) => YMKPoint.Create(point.Latitude, point.Longitude);
}

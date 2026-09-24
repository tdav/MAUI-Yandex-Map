using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Yandex.MapKit.Maui;

/// <summary>Yandex map (MapKit Lite) for .NET MAUI.</summary>
/// <remarks>
/// The Yandex logo and the "Open in Yandex Maps" link are drawn by the SDK in the bottom corners of the map.
/// The MapKit license requires them to stay visible — do not overlay them with your own controls.
/// </remarks>
public class YandexMapView : View
{
    /// <summary>Default camera target: Moscow, Red Square.</summary>
    public static readonly GeoPoint DefaultCenter = new(55.753544, 37.621202);

    public const double MinZoom = 0;
    public const double MaxZoom = 21;

    #region Camera

    public static readonly BindableProperty CenterProperty = BindableProperty.Create(
        nameof(Center), typeof(GeoPoint), typeof(YandexMapView), DefaultCenter, BindingMode.TwoWay);

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(double), typeof(YandexMapView), 10.0, BindingMode.TwoWay,
        coerceValue: (_, value) => Math.Clamp((double)value, MinZoom, MaxZoom));

    public static readonly BindableProperty AzimuthProperty = BindableProperty.Create(
        nameof(Azimuth), typeof(double), typeof(YandexMapView), 0.0, BindingMode.TwoWay);

    public static readonly BindableProperty TiltProperty = BindableProperty.Create(
        nameof(Tilt), typeof(double), typeof(YandexMapView), 0.0, BindingMode.TwoWay);

    /// <summary>Point in the center of the map. Updated when the user moves the map.</summary>
    public GeoPoint Center
    {
        get => (GeoPoint)GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    /// <summary>Zoom level, <see cref="MinZoom"/>…<see cref="MaxZoom"/>. Updated when the user zooms.</summary>
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>Map rotation in degrees, clockwise from north.</summary>
    public double Azimuth
    {
        get => (double)GetValue(AzimuthProperty);
        set => SetValue(AzimuthProperty, value);
    }

    /// <summary>Camera tilt in degrees.</summary>
    public double Tilt
    {
        get => (double)GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    public MapCameraPosition CameraPosition => new(Center, Zoom, Azimuth, Tilt);

    #endregion

    #region Appearance and gestures

    public static readonly BindableProperty IsNightModeEnabledProperty = BindableProperty.Create(
        nameof(IsNightModeEnabled), typeof(bool), typeof(YandexMapView), false);

    public static readonly BindableProperty IsShowingUserProperty = BindableProperty.Create(
        nameof(IsShowingUser), typeof(bool), typeof(YandexMapView), false);

    public static readonly BindableProperty IsZoomEnabledProperty = BindableProperty.Create(
        nameof(IsZoomEnabled), typeof(bool), typeof(YandexMapView), true);

    public static readonly BindableProperty IsScrollEnabledProperty = BindableProperty.Create(
        nameof(IsScrollEnabled), typeof(bool), typeof(YandexMapView), true);

    public static readonly BindableProperty IsRotateEnabledProperty = BindableProperty.Create(
        nameof(IsRotateEnabled), typeof(bool), typeof(YandexMapView), true);

    public static readonly BindableProperty IsTiltEnabledProperty = BindableProperty.Create(
        nameof(IsTiltEnabled), typeof(bool), typeof(YandexMapView), true);

    public bool IsNightModeEnabled
    {
        get => (bool)GetValue(IsNightModeEnabledProperty);
        set => SetValue(IsNightModeEnabledProperty, value);
    }

    /// <summary>
    /// Shows the user location layer. The app must obtain the location permission itself
    /// (e.g. <c>Permissions.RequestAsync&lt;Permissions.LocationWhenInUse&gt;()</c>).
    /// </summary>
    public bool IsShowingUser
    {
        get => (bool)GetValue(IsShowingUserProperty);
        set => SetValue(IsShowingUserProperty, value);
    }

    public bool IsZoomEnabled
    {
        get => (bool)GetValue(IsZoomEnabledProperty);
        set => SetValue(IsZoomEnabledProperty, value);
    }

    public bool IsScrollEnabled
    {
        get => (bool)GetValue(IsScrollEnabledProperty);
        set => SetValue(IsScrollEnabledProperty, value);
    }

    public bool IsRotateEnabled
    {
        get => (bool)GetValue(IsRotateEnabledProperty);
        set => SetValue(IsRotateEnabledProperty, value);
    }

    public bool IsTiltEnabled
    {
        get => (bool)GetValue(IsTiltEnabledProperty);
        set => SetValue(IsTiltEnabledProperty, value);
    }

    #endregion

    #region Clustering

    public static readonly BindableProperty IsClusteringEnabledProperty = BindableProperty.Create(
        nameof(IsClusteringEnabled), typeof(bool), typeof(YandexMapView), false);

    public static readonly BindableProperty ClusterRadiusProperty = BindableProperty.Create(
        nameof(ClusterRadius), typeof(double), typeof(YandexMapView), 60.0);

    public static readonly BindableProperty ClusterMinZoomProperty = BindableProperty.Create(
        nameof(ClusterMinZoom), typeof(int), typeof(YandexMapView), 15);

    public static readonly BindableProperty ClusterColorProperty = BindableProperty.Create(
        nameof(ClusterColor), typeof(Color), typeof(YandexMapView), Color.FromArgb("#FF1E88E5"));

    /// <summary>Groups nearby <see cref="Pins"/> into clusters.</summary>
    public bool IsClusteringEnabled
    {
        get => (bool)GetValue(IsClusteringEnabledProperty);
        set => SetValue(IsClusteringEnabledProperty, value);
    }

    /// <summary>Minimal distance in device-independent units between pins that stay separate.</summary>
    public double ClusterRadius
    {
        get => (double)GetValue(ClusterRadiusProperty);
        set => SetValue(ClusterRadiusProperty, value);
    }

    /// <summary>Zoom level from which pins are never clustered.</summary>
    public int ClusterMinZoom
    {
        get => (int)GetValue(ClusterMinZoomProperty);
        set => SetValue(ClusterMinZoomProperty, value);
    }

    /// <summary>Fill color of the cluster badge.</summary>
    public Color ClusterColor
    {
        get => (Color)GetValue(ClusterColorProperty);
        set => SetValue(ClusterColorProperty, value);
    }

    #endregion

    #region Pins and elements

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(YandexMapView),
        propertyChanged: (b, o, n) => ((YandexMapView)b).OnItemsSourceChanged((IEnumerable?)o, (IEnumerable?)n));

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(YandexMapView),
        propertyChanged: (b, _, _) => ((YandexMapView)b).RecreateItemPins());

    public static readonly BindableProperty ItemTemplateSelectorProperty = BindableProperty.Create(
        nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(YandexMapView),
        propertyChanged: (b, _, _) => ((YandexMapView)b).RecreateItemPins());

    private readonly List<(object Item, MapPin Pin)> _itemPins = [];

    /// <summary>Pins shown on the map. Pins created from <see cref="ItemsSource"/> are added here too.</summary>
    public ObservableCollection<MapPin> Pins { get; } = [];

    /// <summary>Polylines, polygons and circles.</summary>
    public ObservableCollection<MapElement> MapElements { get; } = [];

    /// <summary>Items turned into pins with <see cref="ItemTemplate"/> / <see cref="ItemTemplateSelector"/>.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Template whose root is a <see cref="MapPin"/>; its BindingContext is the item.</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplateSelector? ItemTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ItemTemplateSelectorProperty);
        set => SetValue(ItemTemplateSelectorProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<MapClickedEventArgs>? MapClicked;
    public event EventHandler<MapClickedEventArgs>? MapLongClicked;

    /// <summary>Raised after <see cref="MapPin.MarkerClicked"/> for any pin.</summary>
    public event EventHandler<PinClickedEventArgs>? PinClicked;

    public event EventHandler<ClusterClickedEventArgs>? ClusterClicked;
    public event EventHandler<CameraChangedEventArgs>? CameraChanged;

    #endregion

    /// <summary>
    /// Moves the camera. Unspecified values keep their current value.
    /// <see cref="Center"/>, <see cref="Zoom"/>, <see cref="Azimuth"/> and <see cref="Tilt"/> are updated immediately.
    /// </summary>
    public void MoveTo(GeoPoint center, double? zoom = null, bool animated = true, double? azimuth = null, double? tilt = null)
    {
        using (SuppressCameraMapping())
        {
            Center = center;
            if (zoom is { } z)
                Zoom = z;
            if (azimuth is { } a)
                Azimuth = a;
            if (tilt is { } t)
                Tilt = t;
        }

        Handler?.Invoke(nameof(MoveTo), new MoveToRequest(CameraPosition, animated));
    }

    #region Handler plumbing

    private int _cameraMappingSuppressed;

    /// <summary>
    /// <see langword="true"/> while camera properties are written by <see cref="MoveTo"/> or by the platform
    /// (user gestures): the handler must not move the native camera in response.
    /// </summary>
    internal bool IsCameraMappingSuppressed => _cameraMappingSuppressed > 0;

    internal IDisposable SuppressCameraMapping()
    {
        _cameraMappingSuppressed++;
        return new Releaser(this);
    }

    /// <summary>Called by the handler when the native camera moved.</summary>
    internal void OnPlatformCameraChanged(MapCameraPosition position, CameraChangeReason reason, bool finished)
    {
        using (SuppressCameraMapping())
        {
            Center = position.Target;
            Zoom = position.Zoom;
            Azimuth = position.Azimuth;
            Tilt = position.Tilt;
        }

        CameraChanged?.Invoke(this, new CameraChangedEventArgs(position, reason, finished));
    }

    internal void OnPlatformMapClicked(GeoPoint location) => MapClicked?.Invoke(this, new MapClickedEventArgs(location));

    internal void OnPlatformMapLongClicked(GeoPoint location) => MapLongClicked?.Invoke(this, new MapClickedEventArgs(location));

    /// <returns><see langword="true"/> if the tap was handled and must not propagate to the map.</returns>
    internal bool OnPlatformPinClicked(MapPin pin)
    {
        var args = new PinClickedEventArgs(pin);
        pin.SendMarkerClicked(args);
        PinClicked?.Invoke(this, args);
        return args.Handled;
    }

    internal bool OnPlatformElementClicked(MapElement element, GeoPoint location)
    {
        var args = new MapElementClickedEventArgs(element, location);
        element.SendClicked(args);
        return args.Handled;
    }

    /// <summary>Raises <see cref="ClusterClicked"/>; unless handled, zooms in on the cluster.</summary>
    internal void OnPlatformClusterClicked(IReadOnlyList<MapPin> pins, GeoPoint location)
    {
        var args = new ClusterClickedEventArgs(pins, location);
        ClusterClicked?.Invoke(this, args);
        if (!args.Handled)
            MoveTo(location, GetClusterZoom(Zoom, ClusterMinZoom));
    }

    /// <summary>Zoom used when expanding a cluster: two levels in, but at least to the level where clustering stops.</summary>
    internal static double GetClusterZoom(double currentZoom, int clusterMinZoom) =>
        Math.Clamp(Math.Max(currentZoom + 2, Math.Min(currentZoom + 4, clusterMinZoom)), MinZoom, MaxZoom);

    private sealed class Releaser(YandexMapView view) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                view._cameraMappingSuppressed--;
            }
        }
    }

    #endregion

    #region ItemsSource

    private void OnItemsSourceChanged(IEnumerable? oldSource, IEnumerable? newSource)
    {
        if (oldSource is INotifyCollectionChanged oldObservable)
            oldObservable.CollectionChanged -= OnItemsSourceCollectionChanged;
        if (newSource is INotifyCollectionChanged newObservable)
            newObservable.CollectionChanged += OnItemsSourceCollectionChanged;

        RecreateItemPins();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                foreach (var item in e.NewItems)
                    AddItemPin(item);
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                foreach (var item in e.OldItems)
                    RemoveItemPin(item);
                break;

            case NotifyCollectionChangedAction.Replace when e.OldItems is not null && e.NewItems is not null:
                foreach (var item in e.OldItems)
                    RemoveItemPin(item);
                foreach (var item in e.NewItems)
                    AddItemPin(item);
                break;

            case NotifyCollectionChangedAction.Move:
                break;

            default:
                RecreateItemPins();
                break;
        }
    }

    private void RecreateItemPins()
    {
        foreach (var (_, pin) in _itemPins)
            Pins.Remove(pin);
        _itemPins.Clear();

        if (ItemsSource is null)
            return;

        foreach (var item in ItemsSource)
            AddItemPin(item);
    }

    private void AddItemPin(object? item)
    {
        if (item is null)
            return;

        var template = ItemTemplateSelector?.SelectTemplate(item, this) ?? ItemTemplate;
        if (template?.CreateContent() is not MapPin pin)
            return;

        pin.BindingContext = item;
        _itemPins.Add((item, pin));
        Pins.Add(pin);
    }

    private void RemoveItemPin(object? item)
    {
        var index = _itemPins.FindIndex(p => Equals(p.Item, item));
        if (index < 0)
            return;

        Pins.Remove(_itemPins[index].Pin);
        _itemPins.RemoveAt(index);
    }

    #endregion
}

/// <summary>Argument of the <c>MoveTo</c> handler command.</summary>
public sealed record MoveToRequest(MapCameraPosition Position, bool Animated);

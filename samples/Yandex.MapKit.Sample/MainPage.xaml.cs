using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Sample;

public partial class MainPage : ContentPage
{
    private static readonly Color[] PinColors =
        [Color.FromArgb("#E53935"), Color.FromArgb("#1E88E5"), Color.FromArgb("#43A047"), Color.FromArgb("#FB8C00"), Color.FromArgb("#8E24AA")];

    private readonly MainViewModel _viewModel;
    private readonly List<MapPin> _addedPins = [];
    private int _pinCounter;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        if (MauiProgram.ApiKey is null)
        {
            Dispatcher.Dispatch(async () => await DisplayAlertAsync(
                "Нет API-ключа",
                "Соберите пример с -p:YandexMapKitApiKey=<ключ> или переменной YANDEX_MAPKIT_API_KEY.",
                "OK"));
        }
    }

    private void OnAddPinsClicked(object? sender, EventArgs e)
    {
        var center = Map.Center;
        for (var i = 0; i < 100; i++)
        {
            var pin = new MapPin
            {
                Label = $"Пин {++_pinCounter}",
                Location = new GeoPoint(
                    center.Latitude + (Random.Shared.NextDouble() - 0.5) * 0.08,
                    center.Longitude + (Random.Shared.NextDouble() - 0.5) * 0.14),
                Color = PinColors[Random.Shared.Next(PinColors.Length)],
            };
            _addedPins.Add(pin);
            Map.Pins.Add(pin);
        }
    }

    private void OnToggleClusteringClicked(object? sender, EventArgs e) => _viewModel.IsClustering = !_viewModel.IsClustering;

    private void OnToggleNightClicked(object? sender, EventArgs e) => Map.IsNightModeEnabled = !Map.IsNightModeEnabled;

    private void OnAddShapesClicked(object? sender, EventArgs e)
    {
        if (Map.MapElements.Count > 0)
            return;

        var route = new MapPolyline { StrokeColor = Color.FromArgb("#FC3F1D"), StrokeWidth = 5 };
        foreach (var point in new GeoPoint[]
                 {
                     new(55.760186, 37.618711), new(55.757590, 37.615780), new(55.755337, 37.617744),
                     new(55.753930, 37.620795), new(55.750550, 37.625100), new(55.746500, 37.626500),
                 })
        {
            route.Geopath.Add(point);
        }

        var kremlin = new MapPolygon { StrokeColor = Color.FromArgb("#43A047"), FillColor = Color.FromArgb("#4043A047"), StrokeWidth = 2 };
        foreach (var point in new GeoPoint[]
                 {
                     new(55.755290, 37.613070), new(55.753950, 37.620620), new(55.750140, 37.620920),
                     new(55.748720, 37.617370), new(55.749980, 37.611570), new(55.752820, 37.609490),
                 })
        {
            kremlin.Geopath.Add(point);
        }

        var circle = new MapCircle
        {
            Center = new GeoPoint(55.731482, 37.603527),
            Radius = 500,
            StrokeColor = Color.FromArgb("#1E88E5"),
            FillColor = Color.FromArgb("#401E88E5"),
        };

        foreach (var element in new MapElement[] { route, kremlin, circle })
        {
            element.Clicked += OnElementClicked;
            Map.MapElements.Add(element);
        }

        Map.MoveTo(new GeoPoint(55.745, 37.615), zoom: 13.5);
    }

    private async void OnMyLocationClicked(object? sender, EventArgs e)
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Геолокация", "Нет разрешения на доступ к геопозиции.", "OK");
            return;
        }

        Map.IsShowingUser = true;
        var location = await Geolocation.GetLastKnownLocationAsync()
                       ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        if (location is not null)
            Map.MoveTo(new GeoPoint(location.Latitude, location.Longitude), zoom: 16);
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        foreach (var pin in _addedPins)
            Map.Pins.Remove(pin);
        _addedPins.Clear();
        Map.MapElements.Clear();
    }

    private void OnMapLongClicked(object? sender, MapClickedEventArgs e)
    {
        var pin = new MapPin { Label = $"Точка {++_pinCounter}", Location = e.Location, Color = Color.FromArgb("#FB8C00") };
        _addedPins.Add(pin);
        Map.Pins.Add(pin);
    }

    private async void OnPinClicked(object? sender, PinClickedEventArgs e) =>
        await DisplayAlertAsync(e.Pin.Label ?? "Пин", e.Pin.Location.ToString(), "OK");

    private async void OnElementClicked(object? sender, MapElementClickedEventArgs e) =>
        await DisplayAlertAsync(e.Element.GetType().Name, $"Тап в точке {e.Location}", "OK");
}

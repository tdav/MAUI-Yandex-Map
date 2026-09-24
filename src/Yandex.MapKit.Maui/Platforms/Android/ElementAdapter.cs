using Com.Yandex.Mapkit.Geometry;
using Com.Yandex.Mapkit.Map;
using WeakReference = Java.Lang.Ref.WeakReference;
using Point = Com.Yandex.Mapkit.Geometry.Point;

namespace Yandex.MapKit.Maui;

/// <summary>Keeps <see cref="MapElement"/>s in sync with polylines, polygons and circles.</summary>
internal sealed class ElementAdapter(YandexMapViewHandler handler, IMapObjectCollection collection) : IMapObjectAdapter<MapElement>
{
    // A shape without enough points has no native object until points are added.
    private readonly Dictionary<MapElement, IMapObject?> _objects = new(ReferenceEqualityComparer.Instance);

    public void Add(MapElement element)
    {
        _objects[element] = null;
        Update(element, null);
    }

    public void Update(MapElement element, string? propertyName)
    {
        if (!_objects.TryGetValue(element, out var native))
            return;

        if (native is null || propertyName is nameof(GeopathElement.Geopath) or nameof(MapCircle.Center) or nameof(MapCircle.Radius))
        {
            var created = UpdateGeometry(element, ref native);
            _objects[element] = native;

            // A freshly created object needs every style applied; otherwise only the geometry changed.
            if (!created)
                return;
        }

        if (native is null)
            return;

        switch (element, native)
        {
            case (MapPolyline line, IPolylineMapObject polyline):
                polyline.SetStrokeColor(line.StrokeColor.ToInt());
                polyline.StrokeWidth = (float)line.StrokeWidth;
                break;
            case (MapPolygon shape, IPolygonMapObject polygon):
                polygon.StrokeColor = shape.StrokeColor.ToInt();
                polygon.FillColor = shape.FillColor.ToInt();
                polygon.StrokeWidth = (float)shape.StrokeWidth;
                break;
            case (MapCircle shape, ICircleMapObject circle):
                circle.StrokeColor = shape.StrokeColor.ToInt();
                circle.FillColor = shape.FillColor.ToInt();
                circle.StrokeWidth = (float)shape.StrokeWidth;
                break;
        }

        native.ZIndex = (float)element.ZIndex;
        native.Visible = element.IsVisible;
    }

    public void Remove(MapElement element)
    {
        if (_objects.Remove(element, out var native) && native is { IsValid: true })
            collection.Remove(native);
    }

    public void Clear()
    {
        _objects.Clear();
        if (collection.IsValid)
            collection.Clear();
    }

    /// <summary>Creates, updates or removes the native object for the current geometry.</summary>
    /// <returns><see langword="true"/> if the object was just created (all styles must be applied).</returns>
    private bool UpdateGeometry(MapElement element, ref IMapObject? native)
    {
        switch (element)
        {
            case MapPolyline line when line.Geopath.Count >= 2:
                var polyline = new Polyline(ToPoints(line.Geopath));
                if (native is IPolylineMapObject existingLine)
                {
                    existingLine.Geometry = polyline;
                    return false;
                }

                native = Attach(collection.AddPolyline(polyline), element);
                return true;

            case MapPolygon shape when shape.Geopath.Count >= 3:
                var polygon = new Polygon(new LinearRing(ToPoints(shape.Geopath)), new List<LinearRing>());
                if (native is IPolygonMapObject existingPolygon)
                {
                    existingPolygon.Geometry = polygon;
                    return false;
                }

                native = Attach(collection.AddPolygon(polygon), element);
                return true;

            case MapCircle shape:
                var circle = new Circle(PinAdapter.ToPoint(shape.Center), (float)shape.Radius);
                if (native is ICircleMapObject existingCircle)
                {
                    existingCircle.Geometry = circle;
                    return false;
                }

                native = Attach(collection.AddCircle(circle), element);
                return true;

            default:
                // Not enough points any more.
                if (native is { IsValid: true })
                    collection.Remove(native);
                native = null;
                return false;
        }
    }

    private IMapObject Attach(IMapObject native, MapElement element)
    {
        native.UserData = new ElementTag(element);
        native.AddTapListener(new WeakReference(handler.MapObjectTapListener));
        return native;
    }

    private static List<Point> ToPoints(IList<GeoPoint> points) => points.Select(PinAdapter.ToPoint).ToList();
}

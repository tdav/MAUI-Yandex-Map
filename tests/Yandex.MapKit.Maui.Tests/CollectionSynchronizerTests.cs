using System.Collections.ObjectModel;
using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Maui.Tests;

public class CollectionSynchronizerTests
{
    private static MapPin Pin(string label) => new() { Label = label };

    [Fact]
    public void Adds_existing_items_on_start()
    {
        var source = new ObservableCollection<MapPin> { Pin("a"), Pin("b") };
        var adapter = new FakeAdapter<MapPin>();

        using var sync = new CollectionSynchronizer<MapPin>(source, adapter);

        Assert.Equal(["add a", "add b"], adapter.Log);
    }

    [Fact]
    public void Mirrors_add_remove_replace()
    {
        var a = Pin("a");
        var source = new ObservableCollection<MapPin> { a };
        var adapter = new FakeAdapter<MapPin>();
        using var sync = new CollectionSynchronizer<MapPin>(source, adapter);
        adapter.Log.Clear();

        var b = Pin("b");
        source.Add(b);
        source.Remove(a);
        source[0] = Pin("c");

        Assert.Equal(["add b", "remove a", "remove b", "add c"], adapter.Log);
        Assert.Single(adapter.Items);
    }

    [Fact]
    public void Forwards_property_changes_of_tracked_items_only()
    {
        var a = Pin("a");
        var source = new ObservableCollection<MapPin> { a };
        var adapter = new FakeAdapter<MapPin>();
        using var sync = new CollectionSynchronizer<MapPin>(source, adapter);
        adapter.Log.Clear();

        a.Location = new GeoPoint(1, 2);
        source.Remove(a);
        a.Location = new GeoPoint(3, 4);

        Assert.Equal(["update a Location", "remove a"], adapter.Log);
    }

    [Fact]
    public void Ignores_binding_context_changes()
    {
        var a = Pin("a");
        var adapter = new FakeAdapter<MapPin>();
        using var sync = new CollectionSynchronizer<MapPin>([a], adapter);
        adapter.Log.Clear();

        a.BindingContext = new object();

        Assert.Empty(adapter.Log);
    }

    [Fact]
    public void Clear_on_collection_resets_adapter()
    {
        var source = new ObservableCollection<MapPin> { Pin("a"), Pin("b") };
        var adapter = new FakeAdapter<MapPin>();
        using var sync = new CollectionSynchronizer<MapPin>(source, adapter);
        adapter.Log.Clear();

        source.Clear();

        Assert.Equal(["clear"], adapter.Log);
        Assert.Empty(sync.Tracked);
    }

    [Fact]
    public void Reset_moves_items_to_new_adapter()
    {
        var source = new ObservableCollection<MapPin> { Pin("a") };
        var first = new FakeAdapter<MapPin>();
        using var sync = new CollectionSynchronizer<MapPin>(source, first);
        var second = new FakeAdapter<MapPin>();

        sync.Reset(second);
        source[0].Label = "renamed";

        Assert.Equal(["add a", "clear"], first.Log);
        Assert.Equal(["add a", "update renamed Label"], second.Log);
    }

    [Fact]
    public void Dispose_stops_tracking_without_clearing_native_objects()
    {
        var a = Pin("a");
        var source = new ObservableCollection<MapPin> { a };
        var adapter = new FakeAdapter<MapPin>();
        var sync = new CollectionSynchronizer<MapPin>(source, adapter);
        adapter.Log.Clear();

        sync.Dispose();
        a.Label = "x";
        source.Add(Pin("b"));

        Assert.Empty(adapter.Log);
    }

    [Fact]
    public void Geopath_changes_are_reported_as_property_changes()
    {
        var line = new MapPolyline();
        var adapter = new FakeAdapter<MapElement>();
        using var sync = new CollectionSynchronizer<MapElement>([line], adapter);
        adapter.Log.Clear();

        line.Geopath.Add(new GeoPoint(1, 1));

        Assert.Equal(["update MapPolyline Geopath"], adapter.Log);
    }
}

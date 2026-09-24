// Hand-written binding for the subset of YandexMapsMobile (MapKit Lite) used by Yandex.MapKit.Maui.
// Selectors follow the SDK's code-generated Objective-C API (see the official Swift samples in
// yandex/mapkit-ios-demo). tools/verify-ios-selectors.py checks every [Export]/[Bind] and type name
// against the real xcframework headers in CI.
using System;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace YandexMapsMobile;

#region MapKit

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKMapKit
{
    [Static, Export("sharedInstance")]
    YMKMapKit SharedInstance { get; }

    [Static, Export("setApiKey:")]
    void SetApiKey([NullAllowed] string apiKey);

    [Static, Export("setLocale:")]
    void SetLocale([NullAllowed] string locale);

    [Export("onStart")]
    void OnStart();

    [Export("onStop")]
    void OnStop();

    [Export("version")]
    string Version { get; }

    [Export("createUserLocationLayerWithMapWindow:")]
    YMKUserLocationLayer CreateUserLocationLayer(YMKMapWindow mapWindow);
}

#endregion

#region View

[BaseType(typeof(UIView))]
interface YMKMapView
{
    [Export("initWithFrame:")]
    NativeHandle Constructor(CGRect frame);

    // vulkanPreferred must be true on the iOS simulator (no OpenGL on Apple silicon).
    [Export("initWithFrame:vulkanPreferred:transparencySupport:")]
    NativeHandle Constructor(CGRect frame, bool vulkanPreferred, bool transparencySupport);

    [Export("mapWindow")]
    YMKMapWindow MapWindow { get; }
}

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKMapWindow
{
    [Export("map")]
    YMKMap Map { get; }
}

#endregion

#region Geometry

[BaseType(typeof(NSObject))]
interface YMKPoint
{
    [Static, Export("pointWithLatitude:longitude:")]
    YMKPoint Create(double latitude, double longitude);

    [Export("latitude")]
    double Latitude { get; }

    [Export("longitude")]
    double Longitude { get; }
}

[BaseType(typeof(NSObject))]
interface YMKPolyline
{
    [Static, Export("polylineWithPoints:")]
    YMKPolyline Create(YMKPoint[] points);

    [Export("points")]
    YMKPoint[] Points { get; }
}

[BaseType(typeof(NSObject))]
interface YMKLinearRing
{
    [Static, Export("linearRingWithPoints:")]
    YMKLinearRing Create(YMKPoint[] points);

    [Export("points")]
    YMKPoint[] Points { get; }
}

[BaseType(typeof(NSObject))]
interface YMKPolygon
{
    [Static, Export("polygonWithOuterRing:innerRings:")]
    YMKPolygon Create(YMKLinearRing outerRing, YMKLinearRing[] innerRings);

    [Export("outerRing")]
    YMKLinearRing OuterRing { get; }
}

[BaseType(typeof(NSObject))]
interface YMKCircle
{
    [Static, Export("circleWithCenter:radius:")]
    YMKCircle Create(YMKPoint center, float radius);

    [Export("center")]
    YMKPoint Center { get; }

    [Export("radius")]
    float Radius { get; }
}

#endregion

#region Map and camera

[BaseType(typeof(NSObject))]
interface YMKCameraPosition
{
    [Static, Export("cameraPositionWithTarget:zoom:azimuth:tilt:")]
    YMKCameraPosition Create(YMKPoint target, float zoom, float azimuth, float tilt);

    [Export("target")]
    YMKPoint Target { get; }

    [Export("zoom")]
    float Zoom { get; }

    [Export("azimuth")]
    float Azimuth { get; }

    [Export("tilt")]
    float Tilt { get; }
}

[BaseType(typeof(NSObject))]
interface YMKAnimation
{
    [Static, Export("animationWithType:duration:")]
    YMKAnimation Create(YMKAnimationType type, float duration);
}

delegate void YMKMapCameraCallback(bool completed);

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKMap
{
    [Export("cameraPosition")]
    YMKCameraPosition CameraPosition { get; }

    [Export("moveWithCameraPosition:")]
    void Move(YMKCameraPosition cameraPosition);

    [Export("moveWithCameraPosition:animation:cameraCallback:")]
    void Move(YMKCameraPosition cameraPosition, YMKAnimation animation, [NullAllowed] YMKMapCameraCallback cameraCallback);

    [Export("nightModeEnabled")]
    bool NightModeEnabled { [Bind("isNightModeEnabled")] get; set; }

    [Export("zoomGesturesEnabled")]
    bool ZoomGesturesEnabled { [Bind("isZoomGesturesEnabled")] get; set; }

    [Export("scrollGesturesEnabled")]
    bool ScrollGesturesEnabled { [Bind("isScrollGesturesEnabled")] get; set; }

    [Export("tiltGesturesEnabled")]
    bool TiltGesturesEnabled { [Bind("isTiltGesturesEnabled")] get; set; }

    [Export("rotateGesturesEnabled")]
    bool RotateGesturesEnabled { [Bind("isRotateGesturesEnabled")] get; set; }

    [Export("mapObjects")]
    YMKMapObjectCollection MapObjects { get; }

    // MapKit keeps listeners weakly (__weak since 4.41): callers must keep a strong reference.
    [Export("addInputListenerWithInputListener:")]
    void AddInputListener(IYMKMapInputListener inputListener);

    [Export("removeInputListenerWithInputListener:")]
    void RemoveInputListener(IYMKMapInputListener inputListener);

    [Export("addCameraListenerWithCameraListener:")]
    void AddCameraListener(IYMKMapCameraListener cameraListener);

    [Export("removeCameraListenerWithCameraListener:")]
    void RemoveCameraListener(IYMKMapCameraListener cameraListener);

    [Export("isValid")]
    bool IsValid { get; }
}

interface IYMKMapInputListener { }

[Protocol, Model]
[BaseType(typeof(NSObject))]
interface YMKMapInputListener
{
    [Abstract, Export("onMapTapWithMap:point:")]
    void OnMapTap(YMKMap map, YMKPoint point);

    [Abstract, Export("onMapLongTapWithMap:point:")]
    void OnMapLongTap(YMKMap map, YMKPoint point);
}

interface IYMKMapCameraListener { }

[Protocol, Model]
[BaseType(typeof(NSObject))]
interface YMKMapCameraListener
{
    [Abstract, Export("onCameraPositionChangedWithMap:cameraPosition:cameraUpdateReason:finished:")]
    void OnCameraPositionChanged(YMKMap map, YMKCameraPosition cameraPosition, YMKCameraUpdateReason cameraUpdateReason, bool finished);
}

#endregion

#region Map objects

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKMapObject
{
    [Export("visible")]
    bool Visible { [Bind("isVisible")] get; set; }

    [Export("zIndex")]
    float ZIndex { get; set; }

    [NullAllowed, Export("userData", ArgumentSemantic.Strong)]
    NSObject UserData { get; set; }

    [Export("addTapListenerWithTapListener:")]
    void AddTapListener(IYMKMapObjectTapListener tapListener);

    [Export("removeTapListenerWithTapListener:")]
    void RemoveTapListener(IYMKMapObjectTapListener tapListener);

    [Export("isValid")]
    bool IsValid { get; }
}

interface IYMKMapObjectTapListener { }

[Protocol, Model]
[BaseType(typeof(NSObject))]
interface YMKMapObjectTapListener
{
    [Abstract, Export("onMapObjectTapWithMapObject:point:")]
    bool OnMapObjectTap(YMKMapObject mapObject, YMKPoint point);
}

[BaseType(typeof(YMKMapObject))]
[DisableDefaultCtor]
interface YMKBaseMapObjectCollection
{
    [Export("removeWithMapObject:")]
    void Remove(YMKMapObject mapObject);

    [Export("clear")]
    void Clear();
}

[BaseType(typeof(YMKBaseMapObjectCollection))]
[DisableDefaultCtor]
interface YMKMapObjectCollection
{
    [Export("addPlacemark")]
    YMKPlacemarkMapObject AddPlacemark();

    [Export("addPolylineWithPolyline:")]
    YMKPolylineMapObject AddPolyline(YMKPolyline polyline);

    [Export("addPolygonWithPolygon:")]
    YMKPolygonMapObject AddPolygon(YMKPolygon polygon);

    [Export("addCircleWithCircle:")]
    YMKCircleMapObject AddCircle(YMKCircle circle);

    [Export("addCollection")]
    YMKMapObjectCollection AddCollection();

    [Export("addClusterizedPlacemarkCollectionWithClusterListener:")]
    YMKClusterizedPlacemarkCollection AddClusterizedPlacemarkCollection(IYMKClusterListener clusterListener);
}

[BaseType(typeof(YMKBaseMapObjectCollection))]
[DisableDefaultCtor]
interface YMKClusterizedPlacemarkCollection
{
    [Export("addPlacemark")]
    YMKPlacemarkMapObject AddPlacemark();

    [Export("clusterPlacemarksWithClusterRadius:minZoom:")]
    void ClusterPlacemarks(double clusterRadius, nuint minZoom);
}

[BaseType(typeof(YMKMapObject))]
[DisableDefaultCtor]
interface YMKPlacemarkMapObject
{
    [Export("geometry", ArgumentSemantic.Strong)]
    YMKPoint Geometry { get; set; }

    [Export("opacity")]
    float Opacity { get; set; }

    [Export("setIconWithImage:")]
    void SetIcon(UIImage image);

    [Export("setIconWithImage:style:")]
    void SetIcon(UIImage image, YMKIconStyle style);

    [Export("setTextWithText:")]
    void SetText(string text);

    [Export("setTextWithText:style:")]
    void SetText(string text, YMKTextStyle style);
}

[BaseType(typeof(NSObject))]
interface YMKIconStyle
{
    /// <summary>CGPoint in unit coordinates.</summary>
    [NullAllowed, Export("anchor", ArgumentSemantic.Copy)]
    NSValue Anchor { get; set; }

    [NullAllowed, Export("zIndex", ArgumentSemantic.Copy)]
    NSNumber ZIndex { get; set; }

    [NullAllowed, Export("flat", ArgumentSemantic.Copy)]
    NSNumber Flat { get; set; }

    [NullAllowed, Export("visible", ArgumentSemantic.Copy)]
    NSNumber Visible { get; set; }

    [NullAllowed, Export("scale", ArgumentSemantic.Copy)]
    NSNumber Scale { get; set; }
}

[BaseType(typeof(NSObject))]
interface YMKTextStyle
{
    [Export("size")]
    float Size { get; set; }

    [NullAllowed, Export("color", ArgumentSemantic.Strong)]
    UIColor Color { get; set; }

    [Export("outlineWidth")]
    float OutlineWidth { get; set; }

    [NullAllowed, Export("outlineColor", ArgumentSemantic.Strong)]
    UIColor OutlineColor { get; set; }

    [Export("placement", ArgumentSemantic.Assign)]
    YMKTextStylePlacement Placement { get; set; }

    [Export("offset")]
    float Offset { get; set; }
}

[BaseType(typeof(YMKMapObject))]
[DisableDefaultCtor]
interface YMKPolylineMapObject
{
    [Export("geometry", ArgumentSemantic.Strong)]
    YMKPolyline Geometry { get; set; }

    [Export("strokeWidth")]
    float StrokeWidth { get; set; }

    [Export("setStrokeColorWithColor:")]
    void SetStrokeColor(UIColor color);
}

[BaseType(typeof(YMKMapObject))]
[DisableDefaultCtor]
interface YMKPolygonMapObject
{
    [Export("geometry", ArgumentSemantic.Strong)]
    YMKPolygon Geometry { get; set; }

    [Export("strokeColor", ArgumentSemantic.Strong)]
    UIColor StrokeColor { get; set; }

    [Export("strokeWidth")]
    float StrokeWidth { get; set; }

    [Export("fillColor", ArgumentSemantic.Strong)]
    UIColor FillColor { get; set; }
}

[BaseType(typeof(YMKMapObject))]
[DisableDefaultCtor]
interface YMKCircleMapObject
{
    [Export("geometry", ArgumentSemantic.Strong)]
    YMKCircle Geometry { get; set; }

    [Export("strokeColor", ArgumentSemantic.Strong)]
    UIColor StrokeColor { get; set; }

    [Export("strokeWidth")]
    float StrokeWidth { get; set; }

    [Export("fillColor", ArgumentSemantic.Strong)]
    UIColor FillColor { get; set; }
}

#endregion

#region Clusters

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKCluster
{
    [Export("placemarks")]
    YMKPlacemarkMapObject[] Placemarks { get; }

    [Export("size")]
    nuint Size { get; }

    [Export("appearance")]
    YMKPlacemarkMapObject Appearance { get; }

    [Export("addClusterTapListenerWithClusterTapListener:")]
    void AddClusterTapListener(IYMKClusterTapListener clusterTapListener);

    [Export("isValid")]
    bool IsValid { get; }
}

interface IYMKClusterListener { }

[Protocol, Model]
[BaseType(typeof(NSObject))]
interface YMKClusterListener
{
    [Abstract, Export("onClusterAddedWithCluster:")]
    void OnClusterAdded(YMKCluster cluster);
}

interface IYMKClusterTapListener { }

[Protocol, Model]
[BaseType(typeof(NSObject))]
interface YMKClusterTapListener
{
    [Abstract, Export("onClusterTapWithCluster:")]
    bool OnClusterTap(YMKCluster cluster);
}

#endregion

#region User location

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKUserLocationLayer
{
    [Export("setVisibleWithOn:")]
    void SetVisible(bool on);

    [Export("isValid")]
    bool IsValid { get; }
}

#endregion

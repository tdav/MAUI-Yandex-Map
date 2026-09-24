# Yandex MapKit Lite для .NET MAUI — дизайн

Дата: 2026-09-24 · Основа: `Yandex MapKit Lite в .NET MAUI.md` (технический отчёт в корне репозитория)

## 1. Цель

MAUI-компонент `YandexMapView` поверх Yandex MapKit **4.45.0-lite** для Android и iOS на **.NET 10**:
собственные биндинги двух нативных SDK + кросс-платформенный контрол с API в стиле `Microsoft.Maui.Controls.Maps`.

## 2. Принятые решения (brainstorming)

| Вопрос | Решение |
|---|---|
| Объём итерации | Android binding + iOS binding + MAUI-слой + sample + CI |
| Возможности сверх базовых | Полилинии / полигоны / круги; свои иконки пинов; кластеризация пинов |
| API пинов | Как в MAUI.Maps: `Pins` — `ObservableCollection<MapPin>`, `MapPin : BindableObject`; плюс `ItemsSource` / `ItemTemplate` |
| Упаковка | Три NuGet-пакета + GitHub Actions (Android — ubuntu, iOS — macOS) |
| Target | Только `net10.0-*` (без .NET 9) |

## 3. Структура решения

```
Yandex.MapKit.slnx
├─ Directory.Build.props / Directory.Packages.props (CPM) / .editorconfig / global.json
├─ src/
│  ├─ Yandex.MapKit.Android.Binding/   net10.0-android   AndroidMavenLibrary 4.45.0-lite, Transforms/Metadata.xml
│  ├─ Yandex.MapKit.iOS.Binding/       net10.0-ios       ApiDefinition.cs, StructsAndEnums.cs, native/ (скачивается скриптом)
│  └─ Yandex.MapKit.Maui/              net10.0;net10.0-android;net10.0-ios
├─ tests/Yandex.MapKit.Maui.Tests/     net10.0 (xUnit) — логика без платформ
├─ samples/Yandex.MapKit.Sample/       MAUI-приложение
├─ tools/fetch-ios-sdk.sh              curl zip + проверка SHA-256 из Package.swift
├─ tools/verify-ios-selectors.sh       сверка [Export]-селекторов с заголовками xcframework
└─ .github/workflows/ci.yml
```

`net10.0` в MAUI-библиотеке нужен, чтобы общий код и тесты собирались без мобильных workload'ов
(handler там — заглушка на `System.Object`, как принято в MAUI).

## 4. Биндинги

### Android
- `AndroidMavenLibrary com.yandex.android:maps.mobile 4.45.0-lite`. Зависимости из реального POM 4.45.0-lite
  (play-services-location 21.0.1, play integrity 1.1.0, androidx annotation/core/lifecycle-common/work-runtime 2.11.1)
  закрываются NuGet-пакетами Xamarin.*.
- Java-API сверено с `classes.jar` из AAR. Важное: с 4.41 все листенеры передаются как
  `java.lang.ref.WeakReference<T>` → в C# `new Java.Lang.Ref.WeakReference(listener)`; handler держит сильную ссылку.
- Namespace'ы оставляем сгенерированными (`Com.Yandex.Mapkit.*`, `Com.Yandex.Runtime.*`) — это низкоуровневый слой,
  переименование создаёт коллизии (`Map.Map`) и не даёт выгоды. Пакеты `*.internal` и неиспользуемые подсистемы
  (sensors, attestation, offline_cache internal…) удаляются через `Metadata.xml` там, где они ломают генерацию.

### iOS
- xcframework скачивается `tools/fetch-ios-sdk.sh` по URL и checksum из официального `yandex/mapkit-ios-lite/Package.swift`
  (не коммитится в git). `NativeReference` c `ForceLoad`, `IsCxx`, системными фреймворками из того же Package.swift
  (включая `NetworkExtension`).
- `ApiDefinition.cs` — вручную написанное подмножество (~40 типов): YMKMapKit, YMKMapView, YMKMapWindow, YMKMap,
  YMKCameraPosition, YMKPoint, YMKAnimation, YMKMapObject(Collection), YMKPlacemarkMapObject, YMKIconStyle,
  YMKPolyline(MapObject), YMKPolygon(MapObject), YMKLinearRing, YMKCircle(MapObject), YMKClusterizedPlacemarkCollection,
  YMKCluster, YMKUserLocationLayer, протоколы-листенеры.
- Заголовков в этом окружении нет (S3 Yandex недоступен), поэтому селекторы выводятся из Java-API + Swift-демо и
  **проверяются в CI** скриптом `verify-ios-selectors.sh` по реальным заголовкам — расхождения валят сборку.

## 5. MAUI API

```csharp
public sealed class YandexMapView : View
{
    // Камера
    GeoPoint Center; double Zoom; double Azimuth; double Tilt;          // TwoWay, обновляются жестами
    void MoveTo(GeoPoint center, double? zoom = null, bool animated = true);

    // Отображение
    bool IsNightModeEnabled; bool IsShowingUser;
    bool IsZoomEnabled; bool IsScrollEnabled; bool IsRotateEnabled; bool IsTiltEnabled;

    // Пины
    ObservableCollection<MapPin> Pins { get; }                          // как Map.Pins
    IEnumerable? ItemsSource; DataTemplate? ItemTemplate; DataTemplateSelector? ItemTemplateSelector;
    bool IsClusteringEnabled; double ClusterRadius = 60; int ClusterMinZoom = 15;

    // Фигуры
    ObservableCollection<MapElement> MapElements { get; }               // MapPolyline, MapPolygon, MapCircle

    // События
    event MapClicked, MapLongClicked, CameraChanged (с IsFinished, Reason), ClusterClicked;
}

public class MapPin : BindableObject      // Location, Label, ImageSource? Icon, Color, Anchor, ZIndex, BindingContext
    event MarkerClicked;                  // EventArgs.Handled как в MAUI.Maps
public abstract class MapElement : BindableObject { Color StrokeColor; float StrokeWidth; float ZIndex; bool IsVisible; }
public sealed class MapPolyline : MapElement { IList<GeoPoint> Geopath; }
public sealed class MapPolygon  : MapElement { IList<GeoPoint> Geopath; Color FillColor; }
public sealed class MapCircle   : MapElement { GeoPoint Center; double RadiusMeters; Color FillColor; }
```

- `GeoPoint` — `readonly record struct`. Типы MAUI.Maps (`Location`) не используем, чтобы не тянуть пакет Maps.
- Иконки пинов: любой `ImageSource` → нативное изображение через `IImageSourceService` MAUI
  (Android: `Drawable` → `Bitmap` → `ImageProvider.FromBitmap`; iOS: `UIImage` → `YMKImageProvider`/`setIconWithImage:`).
  Если иконки нет — рисуем стандартный маркер кодом (кружок цвета `MapPin.Color` с обводкой), без ресурсов.
- Кластеры: иконка кластера — рисуемый кодом круг с числом; тап по кластеру → `ClusterClicked` + по умолчанию zoom-in.

## 6. Handler и жизненный цикл

- `YandexMapViewHandler : ViewHandler<YandexMapView, PlatformView>` с `PropertyMapper` и `CommandMapper` (`MoveTo`).
- Пины и фигуры синхронизирует платформенно-независимый `MapObjectSynchronizer<TModel, TNative>`:
  подписка на `INotifyCollectionChanged` коллекции и `PropertyChanged` элементов → вызовы абстрактного
  адаптера (`Add/Update/Remove/Clear`). Эта логика покрыта unit-тестами на `net10.0` с фейковым адаптером.
- Обратная синхронизация камеры (жесты → `Center`/`Zoom`) под флагом, чтобы не зациклить `Move`.
- Android: `MapKitFactory.SetApiKey` вызывается внутри `UseYandexMapKit(apiKey)` до первого `Initialize`
  (ленивая инициализация в `CreatePlatformView`), `OnStart/OnStop` MapKit и всех живых MapView — по lifecycle Activity.
- iOS: `SetApiKey` + `SharedInstance` в `FinishedLaunching`; `vulkanPreferred` на симуляторе.
- Лицензионное: не перекрывать логотип Yandex; README напоминает про ссылку на условия в «О приложении».

## 7. Тестирование и CI

- `dotnet test` на ubuntu: синхронизатор коллекций, `GeoPoint`, расчёты (zoom-in по кластеру, конвертация цвета).
- CI `android`: `dotnet workload install maui-android` → build binding, Maui, sample (APK).
- CI `ios` (macos): `fetch-ios-sdk.sh` → `verify-ios-selectors.sh` → build binding, Maui, sample (simulator).
- CI `pack`: `dotnet pack` трёх пакетов, артефакты.
- API-ключ sample'а — из MSBuild-свойства `YandexMapKitApiKey` / переменной окружения → генерируемый `BuildSecrets`, не в git.

## 8. Вне объёма

Поиск/маршруты (нет в Lite), офлайн-карты, слой пробок, NaviKit, Windows/macCatalyst, NativeAOT.

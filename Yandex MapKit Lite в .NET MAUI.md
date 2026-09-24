# Yandex MapKit Lite в .NET MAUI (.NET 9/10): биндинги Android/iOS и кросс-платформенный контрол — технический отчёт

Живых биндингов MapKit 4.x для .NET нет, а Yandex в FAQ прямо пишет: «Xamarin apps are not supported at the moment». Реалистичный путь из трёх частей:
- собственный Android binding через `AndroidMavenLibrary` (`com.yandex.android:maps.mobile:4.45.0-lite`);
- собственный iOS binding поверх Objective-C xcframework `YandexMapsMobile` (4.45.0-lite);
- тонкий MAUI-handler поверх обоих.

Главный риск не технический, а лицензионный: бесплатная лицензия запрещает закрытые приложения, мониторинг/диспетчеризацию и офлайн-режим.

> Процессное примечание: обязательный шаг `enrich_draft` не выполнен — черновик трижды не прошёл лимит в 50 000 символов, затем закончились ходы. Поэтому формулировки в отчёте не проходили автоматическую проверку на расплывчатость и точность источников.

## TL;DR
- **Технически задача решаема: MVP займёт примерно 3,5–6 недель одного разработчика (17–29 рабочих дней, см. §7).**
  - Android SDK — AAR на Maven Central (4.45.0 от 17.09.2026, minSdk 26) с небольшим набором зависимостей AndroidX и Google Play Services.
  - iOS SDK — фреймворк на Objective-C (префиксы YMK/YRT), биндится через Objective Sharpie без Swift-прослойки.
  - Биндить стоит не весь SDK, а нужное подмножество: MapKitFactory/YMKMapKit, MapView, Map, камера, MapObjects/Placemark, UserLocationLayer, листенеры.
- **Готовых решений нет.** Найденное мертво или относится к MapKit 2.x: Xamarin.YandexMaps.iOS, NuGet `Yandex.Maps` для Windows Phone 7 (2012). Официально поддерживаются только нативные Android/iOS и Flutter. Список типов, входящих в Lite, удобно смотреть в KMP-обёртке `yandex-mapkit-kmp`.
- **Для государственного/корпоративного сценария бесплатный тариф почти наверняка не подходит.** Он требует «projects with open access», запрещает «monitoring and dispatching» и хранение данных, а офлайн-карты есть «in the paid version only». MapKit всегда обращается к серверам Yandex, развёртывания у себя (on-premise) нет. Коммерческую лицензию нужно заложить в план до начала разработки.

## Key Findings

| Вопрос | Ответ | Уверенность |
|---|---|---|
| Текущая версия | 4.45.0 (17.09.2026); предыдущая — 4.42.0 (21.07.2026) | Подтверждено (changelog Yandex) |
| Android-артефакт | `com.yandex.android:maps.mobile:4.45.0-lite`. Вариант задаётся **суффиксом версии**, а не classifier'ом | Подтверждено (документация + Maven Central) |
| iOS-артефакт | CocoaPods `pod 'YandexMapsMobile', '4.45.0-lite'`; SPM: `github.com/yandex/mapkit-ios-lite` (SPM с 4.12.0, в 4.45.0 исправлены ошибки сборки SPM) | Подтверждено |
| Язык iOS API | Objective-C (YMK*/YRT*); с 4.41.0 листенеры передаются через `__weak` pointer | Высокая (косвенные признаки) |
| Тип iOS-бинаря | Статический фреймворк внутри xcframework (xcframework с 4.1.0) | Средняя: судя по ошибкам линковщика, внутри архива лежат `.mm.o` |
| Android minSdk | 26 (с 4.8.0) | Подтверждено |
| Минимальная iOS | 12 (13 для M1-симулятора) по записи 4.3.1; для 4.45 не перепроверено | Средняя |
| Bitcode | Удалён в 4.4.0 | Подтверждено |
| Поддержка Xamarin/.NET | «Xamarin apps are not supported at the moment» (FAQ Yandex) | Подтверждено |
| Бесплатный лимит | 25K MAU | Подтверждено (страница продукта) |
| Офлайн-карты | Только в платной версии | Подтверждено |
| Типы карт | «Only "road map" and custom map layers are available» — спутника нет | Подтверждено (FAQ) |

---

## 1. Обзор MapKit Lite

### 1.1 Lite vs Full vs NaviKit

| Возможность | Lite | Full | NaviKit |
|---|---|---|---|
| Карта (MapView, камера, жесты, стили) | ✅ | ✅ | ✅ |
| MapObjects: placemark, polyline, polygon, circle, кластеры | ✅ | ✅ | ✅ |
| Слой пробок | ✅ | ✅ | ✅ |
| LocationManager, UserLocationLayer | ✅ | ✅ | ✅ |
| Офлайн-карты (OfflineCacheManager) | ✅ (только платная лицензия) | ✅ (платная) | ✅ |
| Поиск, подсказки (suggest), геокодер | ❌ | ✅ | ✅ |
| Маршрутизация (авто, вело, пешеход, общественный транспорт) | ❌ | ✅ | ✅ |
| Панорамы | ❌ | ✅ | ✅ |
| Навигация (ведение по маршруту) | ❌ | ❌ | ✅ |

Официальный комментарий в Gradle/Podfile: «The lite library only contains the map, traffic layer, LocationManager, and UserLocationLayer and lets you download offline maps (in the paid version only).»

### 1.2 Координаты артефактов

| Платформа | Канал | Координаты (4.45.0) |
|---|---|---|
| Android | Maven Central | `com.yandex.android:maps.mobile:4.45.0-lite` (packaging `aar`). В том же `artifactId` лежат `-full`, `-navikit`, `-lite-flutter` и другие |
| iOS | CocoaPods | `pod 'YandexMapsMobile', '4.45.0-lite'` (spec публикует аккаунт @YandexMapKit) |
| iOS | SPM | `https://github.com/yandex/mapkit-ios-lite` (Full: `github.com/yandex/mapkit-ios`) |
| iOS | Прямая загрузка | URL из поля `source` в podspec. Шаблон `https://maps-ios-pods-public.s3.yandex.net/YandexMapsMobile-<ver>-lite.framework.zip` выведен по аналогии с 4.4.0; для 4.45.0 не проверен |

### 1.3 Транзитивные зависимости Android

Источник — POM 4.23.0-lite. У 4.45.0 состав, по имеющимся данным, тот же; выросла версия work-runtime.

| Maven-зависимость | Версия в POM | NuGet-пакет (актуальную версию проверить) |
|---|---|---|
| `com.google.android.gms:play-services-location` | 21.0.1 | `Xamarin.GooglePlayServices.Location` |
| `com.google.android.play:integrity` | 1.1.0 | `Xamarin.Google.Android.Play.Integrity` |
| `androidx.annotation:annotation` | 1.1.0 | `Xamarin.AndroidX.Annotation` |
| `androidx.core:core` | 1.9.0 | `Xamarin.AndroidX.Core` |
| `androidx.lifecycle:lifecycle-common` | 2.2.0 | `Xamarin.AndroidX.Lifecycle.Common` |
| `androidx.work:work-runtime` | 2.8.1 (4.23) → 2.11.1 (4.45 navikit-flutter) | `Xamarin.AndroidX.Work.Runtime` |

Kotlin stdlib, OkHttp и protobuf в POM **нет**. Ядро MapKit — нативная C++-библиотека (`.so`), Java-слой — тонкая сгенерированная обёртка. Для биндинга это хорошо: проблем, специфичных для Kotlin, практически не будет.

### 1.4 Системные зависимости iOS

Список взят из podspec 4.10.1-lite в пересказе surfstudio; для 4.45 не перепроверен.

- Фреймворки: `CoreFoundation Foundation CoreLocation UIKit OpenGLES SystemConfiguration CoreGraphics QuartzCore Security CoreTelephony CoreMotion DeviceCheck`.
- Библиотеки: `resolv`, `c++`.
- Флаг `-ObjC` (официально: «Recommended linking flag: `-ObjC`»).

### 1.5 Размер
Абсолютный размер Lite не найден ни для одной платформы. Известно:
- в 4.3.0 — «Significantly reduced the size of the full and lite MapKit versions»;
- в 4.29.0 — «SDK size for Android reduced by 10%»;
- AAR `4.16.0-beta-navikit-flutter` весит около 60,4 МБ; Lite должен быть заметно меньше.

Точный размер лучше измерить самостоятельно (`repo1.maven.org/.../4.45.0-lite/`).

### 1.6 API-ключ
1. Открыть Developer Dashboard (`developer.tech.yandex.ru/services/`) и войти под **Yandex ID**. Корпоративная почта без Yandex ID не подходит.
2. Выбрать «Connect APIs» → «MapKit Mobile SDK», указать проект и выбрать тариф.
3. Ключ появится в «API Interfaces → MapKit Mobile SDK». Активация занимает около 15 минут.

### 1.7 Лицензия и условия, критичные для государственного/корпоративного проекта

| Условие бесплатного использования | Что это значит для закрытого корпоративного приложения |
|---|---|
| «The API may only be used in projects with open access»; регистрация должна быть открыта всем | Внутреннее приложение с ограниченным доступом **нарушает условие** → нужна коммерческая лицензия |
| «may not be used for monitoring and dispatching» | Трекинг сотрудников и транспорта без платной лицензии запрещён |
| Нельзя хранить или изменять данные, полученные через API | Кешировать и сохранять результаты нельзя |
| Логотип, копирайты и кнопка «Open in Maps» всегда видны: **«This condition applies even with a paid license»** | Нижние углы карты нельзя перекрывать своими контролами |
| Ссылка на `https://yandex.ru/legal/maps_api/en` в разделе «About» | Обязательный пункт экрана «О приложении» |
| Дневные лимиты; при регулярном превышении доступ «permanently blocked» | Нужен мониторинг в Dashboard |
| Бесплатно до 25K MAU, дальше тарифы Basic/Advanced с оплатой по MAU | Учесть в бюджете |

**Сеть и on-premise.** MapKit загружает тайлы и стили с серверов Yandex. Локального тайл-сервера или режима on-premise в SDK нет. Офлайн возможен только через OfflineCacheManager и только с платной лицензией: регионы скачиваются заранее. В изолированной сети без выхода в интернет MapKit работать не будет.

---

## 2. Android binding

### 2.1 Проект `Yandex.MapKit.Android.Binding.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-android</TargetFramework> <!-- или net9.0-android: AndroidMavenLibrary доступен с .NET 9 -->
    <SupportedOSPlatformVersion>26</SupportedOSPlatformVersion> <!-- minSdk MapKit ≥ 4.8.0 -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsTrimmable>true</IsTrimmable>
    <PackageId>Yandex.MapKit.Lite.Android</PackageId>
    <Version>4.45.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <!-- Сам SDK: скачивается из Maven Central + проверка зависимостей по POM -->
    <AndroidMavenLibrary Include="com.yandex.android:maps.mobile" Version="4.45.0-lite" Bind="true" />
  </ItemGroup>

  <ItemGroup>
    <!-- Транзитивные зависимости закрываем официальными NuGet-биндингами (версии — через CPM) -->
    <PackageReference Include="Xamarin.GooglePlayServices.Location" />
    <PackageReference Include="Xamarin.Google.Android.Play.Integrity" />
    <PackageReference Include="Xamarin.AndroidX.Core" />
    <PackageReference Include="Xamarin.AndroidX.Annotation" />
    <PackageReference Include="Xamarin.AndroidX.Lifecycle.Common" />
    <PackageReference Include="Xamarin.AndroidX.Work.Runtime" />
  </ItemGroup>

  <ItemGroup>
    <TransformFile Include="Transforms\Metadata.xml" />
  </ItemGroup>
</Project>
```

Как это работает (по Microsoft Learn):
1. `AndroidMavenLibrary` скачивает артефакт и `.pom` в локальный кеш и подключает их как `AndroidLibrary`.
2. По POM выполняется Java Dependency Verification: незакрытая зависимость даёт ошибку `XA4241`/`XA4242`.
3. Ошибка исчезает, если зависимость покрыта NuGet-пакетом с корректными метаданными.
4. Если такого пакета нет — добавить `AndroidMavenLibrary ... Bind="false"` или, в крайнем случае, явно проигнорировать зависимость (например, если она содержит только аннотации).

### 2.2 Известные проблемы генерации и `Metadata.xml`

| Проблема | Причина | Решение |
|---|---|---|
| Namespace вида `Com.Yandex.Mapkit.*` | Автоматическое преобразование имён Java-пакетов в PascalCase | `managedName` для пакетов |
| `XA4241/XA4242` | Незакрытые зависимости из POM | NuGet-биндинги или `Bind="false"` |
| Дубли членов, «не реализует член интерфейса» | Внутренние классы. Исторический пример — issue #143 (2019) с `JobPreconditions` из evernote android-job; в 4.6.1 эта библиотека заменена на WorkManager | `remove-node` для внутренних пакетов; биндить только публичный API |
| Интерфейсы-листенеры | Java-интерфейс превращается в C# `I*Listener`; реализация обязана наследовать `Java.Lang.Object` | Штатная ситуация, см. код ниже |
| `WeakRef` в сигнатурах (с 4.41.0) и `BridgedStruct` | «listeners … are now passed to methods explicitly through `WeakRef`» | Проверить, как генератор отобразил тип; при необходимости добавить extension-хелпер |

```xml
<!-- Transforms/Metadata.xml — стартовый шаблон; точные пути сверить с obj/Debug/.../api.xml -->
<metadata>
  <attr path="/api/package[@name='com.yandex.mapkit']" name="managedName">Yandex.MapKit</attr>
  <attr path="/api/package[@name='com.yandex.mapkit.map']" name="managedName">Yandex.MapKit.Map</attr>
  <attr path="/api/package[@name='com.yandex.mapkit.mapview']" name="managedName">Yandex.MapKit.MapView</attr>
  <attr path="/api/package[@name='com.yandex.mapkit.geometry']" name="managedName">Yandex.MapKit.Geometry</attr>
  <attr path="/api/package[@name='com.yandex.mapkit.user_location']" name="managedName">Yandex.MapKit.UserLocation</attr>
  <attr path="/api/package[@name='com.yandex.runtime.image']" name="managedName">Yandex.Runtime.Image</attr>
  <!-- Выкидываем всё внутреннее: меньше ошибок компиляции и размер сборки -->
  <remove-node path="/api/package[contains(@name,'.internal')]" />
</metadata>
```

Рабочий цикл: `dotnet build` → смотрим `obj/Debug/net10.0-android/api.xml` и ошибки компилятора CS → точечно правим `Metadata.xml`. Если ошибок слишком много, можно поступить радикальнее: биндить только нужные пакеты, а остальные удалить через `remove-node` по `package`.

### 2.3 Инициализация и жизненный цикл (порядок важен)

| Шаг | Где | Вызов |
|---|---|---|
| 1 | `MainApplication.OnCreate` | `MapKitFactory.SetApiKey(key)` (и при желании `SetLocale`). Строго **до** `initialize`, иначе ошибка «setApiKey() should be called before initialize()!» |
| 2 | `Activity.OnCreate` или перед первым MapView | `MapKitFactory.Initialize(context)` — загружает нативные `.so` |
| 3 | `Activity.OnStart` | `MapKitFactory.Instance.OnStart()` + `mapView.OnStart()` |
| 4 | `Activity.OnStop` | `mapView.OnStop()` + `MapKitFactory.Instance.OnStop()` |
| 5 | Завершение | `onTerminate` (появился в 4.14.0) |

Без `onStart` вместо карты будет пустая сетка. FAQ: «Why is an empty grid displayed instead of a map? … calling the `MapKit.onStart()` method».

```csharp
// Platforms/Android/MainApplication.cs
namespace Contoso.App;

[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : MauiApplication(handle, ownership)
{
    public override void OnCreate()
    {
        Yandex.MapKit.MapKitFactory.SetApiKey(BuildSecrets.YandexMapKitApiKey);
        base.OnCreate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

Базовые операции (эквиваленты официальных Kotlin-примеров):
- **камера:** `map.Move(new CameraPosition(new Point(lat, lon), zoom, 0f, 0f), new Animation(Animation.Type.Smooth, 0.3f), null)`;
- **пин:** `map.MapObjects.AddPlacemark()` (вариант без параметров, с 4.4.0) + `Geometry` + `SetIcon(ImageProvider.FromResource(ctx, resId))`. Только PNG, векторные иконки не поддерживаются;
- **геопозиция:** `MapKitFactory.Instance.CreateUserLocationLayer(mapView.MapWindow)` с `Visible = true`.

Имена C#-членов здесь — ожидаемый результат генератора; сверьте их с фактическим `api.xml`.

---

## 3. iOS binding

### 3.1 Как получить xcframework

| Способ | Плюсы | Минусы | Рекомендация |
|---|---|---|---|
| CocoaPods: временный Xcode-проект + `pod install`, забрать `Pods/YandexMapsMobile/*.xcframework` | Официальный путь; в podspec точный список фреймворков | Нужен Ruby/CocoaPods на Mac | ✅ для первичного исследования |
| Прямой zip из поля `source` podspec (официально описано в FAQ) | Воспроизводимо в CI (`curl`) | URL нужно брать из podspec каждой версии | ✅ **для CI** |
| SPM `yandex/mapkit-ios-lite` (`binaryTarget`) | Официальный, есть checksum | .NET не потребляет SPM напрямую, xcframework всё равно нужно извлекать | Для проверки checksum |

Внутри архива фреймворк может называться `YandexMapsMobileLite.xcframework`: surfstudio советует «Rename … to `YandexMapsMobile.xcframework` if needed». Модуль и umbrella-header при этом называются `YandexMapsMobile`.

### 3.2 Objective-C или Swift?
Публичный API написан на **Objective-C**: классы `YMKMapKit`, `YMKMapView`, `YMKPoint`, `YRTI18nManagerFactory`, `__weak` в сигнатурах с 4.41.0. Swift-обёртка (Native Library Interop) **не нужна**: Objective Sharpie работает прямо по заголовкам. Внутри — C++, поэтому нужны `libc++` и `IsCxx`.

### 3.3 Objective Sharpie

```bash
# На Mac, с установленным Xcode
sharpie xcode -sdks                                  # выбрать iphoneosXX.X
cd YandexMapsMobile.xcframework/ios-arm64
sharpie bind -sdk iphoneos18.0 \
  -framework ./YandexMapsMobile.framework \
  -namespace Yandex.MapKit.iOS \
  -output ../../sharpie-out
# Результат: ApiDefinitions.cs + StructsAndEnums.cs; проверить все [Verify], удалить неиспользуемое
```

Сгенерированный API будет большим. Практичнее оставить в `ApiDefinition.cs` 30–60 нужных типов, чем чинить сотни `[Verify]`.

### 3.4 `Yandex.MapKit.iOS.Binding.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-ios</TargetFramework>
    <IsBindingProject>true</IsBindingProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>true</ImplicitUsings>
    <PackageId>Yandex.MapKit.Lite.iOS</PackageId>
    <Version>4.45.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <ObjcBindingApiDefinition Include="ApiDefinition.cs" />
    <ObjcBindingCoreSource Include="StructsAndEnums.cs" />
  </ItemGroup>

  <ItemGroup>
    <NativeReference Include="native/YandexMapsMobile.xcframework">
      <Kind>Framework</Kind>
      <ForceLoad>True</ForceLoad>          <!-- статический фреймворк + категории ObjC -->
      <IsCxx>True</IsCxx>                  <!-- C++-ядро → libc++ -->
      <SmartLink>False</SmartLink>
      <Frameworks>CoreFoundation Foundation CoreLocation UIKit OpenGLES SystemConfiguration CoreGraphics QuartzCore Security CoreTelephony CoreMotion DeviceCheck</Frameworks>
      <LinkerFlags>-ObjC -lresolv -lc++</LinkerFlags>
    </NativeReference>
  </ItemGroup>
</Project>
```

`Kind`, `ForceLoad`, `Frameworks` и `LinkerFlags` — стандартные метаданные `NativeReference` в .NET for iOS. Без `ForceLoad` линковщик может выбросить нужные символы, и первый же вызов SDK упадёт в рантайме.

### 3.5 `ApiDefinition.cs` — минимальное подмножество

```csharp
namespace Yandex.MapKit.iOS;

using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface YMKMapKit
{
    [Static, Export("setApiKey:")] void SetApiKey(string apiKey);
    [Static, Export("setLocale:")] void SetLocale([NullAllowed] string locale);
    [Static, Export("sharedInstance")] YMKMapKit SharedInstance { get; }
    [Export("onStart")] void OnStart();
    [Export("onStop")] void OnStop();
    [Export("createUserLocationLayerWithMapWindow:")]
    YMKUserLocationLayer CreateUserLocationLayer(YMKMapWindow mapWindow);
}

[BaseType(typeof(UIView))]
interface YMKMapView
{
    [Export("initWithFrame:vulkanPreferred:")]
    NativeHandle Constructor(CGRect frame, bool vulkanPreferred); // true только для arm64-симулятора
    [Export("mapWindow")] YMKMapWindow MapWindow { get; }
}

[BaseType(typeof(NSObject))] interface YMKMapWindow { [Export("map")] YMKMap Map { get; } }

[BaseType(typeof(NSObject))]
interface YMKMap
{
    [Export("moveWithCameraPosition:")] void Move(YMKCameraPosition position);
    [Export("moveWithCameraPosition:animation:cameraCallback:")]
    void Move(YMKCameraPosition position, YMKAnimation animation, [NullAllowed] Action<bool> callback);
    [Export("mapObjects")] YMKMapObjectCollection MapObjects { get; }
    [Export("addInputListenerWithInputListener:")] void AddInputListener(IYMKMapInputListener listener);
    [Export("removeInputListenerWithInputListener:")] void RemoveInputListener(IYMKMapInputListener listener);
}

[BaseType(typeof(NSObject))]
interface YMKPoint
{
    [Static, Export("pointWithLatitude:longitude:")] YMKPoint Create(double latitude, double longitude);
    [Export("latitude")] double Latitude { get; }
    [Export("longitude")] double Longitude { get; }
}

[BaseType(typeof(NSObject))]
interface YMKCameraPosition
{
    [Static, Export("cameraPositionWithTarget:zoom:azimuth:tilt:")]
    YMKCameraPosition Create(YMKPoint target, float zoom, float azimuth, float tilt);
}

interface IYMKMapInputListener { }
[Protocol, Model, BaseType(typeof(NSObject))]
interface YMKMapInputListener
{
    [Abstract, Export("onMapTapWithMap:point:")] void OnMapTap(YMKMap map, YMKPoint point);
    [Abstract, Export("onMapLongTapWithMap:point:")] void OnMapLongTap(YMKMap map, YMKPoint point);
}
// + IYMKMapCameraListener, YMKMapObjectCollection (addPlacemark), YMKPlacemarkMapObject,
//   YMKAnimation, YMKUserLocationLayer, enums в StructsAndEnums.cs
```

Селекторы восстановлены по Swift-сигнатурам из официальной документации (`move(with:animation:cameraCallback:)`, `addCameraListener(with:)`, `onMapObjectTap(with:point:)`) и **требуют сверки** с выводом Sharpie.

### 3.6 Подводные камни iOS

| Тема | Суть | Действие |
|---|---|---|
| Инициализация | `setApiKey` + `sharedInstance` в `didFinishLaunching`. Если инициализировать в другом месте, нужен явный `onStart()` | См. `UseYandexMapKit` в §4.4 |
| Bitcode | Удалён в 4.4.0 | Ничего делать не нужно: .NET для iOS bitcode не использует |
| Симулятор на Apple Silicon | OpenGL не поддерживается, нужен `vulkanPreferred: true`; для M1-симулятора минимум iOS 13 | Передавать `vulkanPreferred` при `Runtime.Arch == Arch.SIMULATOR` |
| Слайсы | xcframework (с 4.1.0) содержит слайсы для устройства и симулятора | Проверить `Info.plist` xcframework на `ios-arm64_x86_64-simulator` |
| Листенеры | «All Listener objects must be inherited from the `NSObject` class»; MapKit держит на них только слабые ссылки | Хранить сильную ссылку в handler'е, иначе GC соберёт листенер и колбэки молча прекратятся |
| Потоки | Колбэки приходят в main thread | UI можно трогать сразу |
| Privacy manifest | Для SDK из списка Apple манифест обязателен; есть ли `PrivacyInfo.xcprivacy` внутри YandexMapsMobile, выяснить не удалось | В приложении обязателен `Platforms/iOS/PrivacyInfo.xcprivacy` (MAUI требует минимум три категории: FileTimestamp C617.1, SystemBootTime 35F9.1, DiskSpace E174.1). Содержимое xcframework проверить |
| Info.plist | Доступ к геолокации | `NSLocationWhenInUseUsageDescription` |

---

## 4. MAUI-слой

### 4.1 Архитектура

- `YandexMapView : View` — общий контрол с bindable-свойствами (Center, Zoom, Pins, NightMode, IsUserLocationVisible) и событиями.
- `YandexMapViewHandler : ViewHandler<YandexMapView, PlatformView>` — handler с partial-реализациями `.Android.cs` (→ `Yandex.MapKit.MapView.MapView`) и `.iOS.cs` (→ `YMKMapView`).
- `YandexMapKitLifecycle` — статический класс, один на процесс: Initialize и OnStart/OnStop по событиям жизненного цикла.

Решения, продиктованные ограничениями SDK:
- **Не делать `MapType` (Satellite/Hybrid).** В MapKit есть только «road map» и кастомные слои. Вместо этого — `NightMode` и стили.
- **Пины — только растровые иконки.** Векторные не поддерживаются. В MAUI-слое храните имя ресурса и конвертируйте его в `ImageProvider`/`UIImage`.
- **Не перекрывать логотип и «Open in Maps».** Это лицензионное требование, оно действует и при платной лицензии.
- **Кнопки зума и прочие контролы — свои.** FAQ: «Controls should be implemented in the application».

### 4.2 Контрол

```csharp
namespace Yandex.MapKit.Maui;

public readonly record struct GeoPoint(double Latitude, double Longitude);

public sealed record MapPin(string Id, GeoPoint Location, string? Title = null, string? IconFile = null);

public sealed class MapTappedEventArgs(GeoPoint point) : EventArgs
{
    public GeoPoint Point { get; } = point;
}

public sealed class YandexMapView : View
{
    public static readonly BindableProperty CenterProperty = BindableProperty.Create(
        nameof(Center), typeof(GeoPoint), typeof(YandexMapView), new GeoPoint(55.751225, 37.62954));

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(float), typeof(YandexMapView), 12f);

    public static readonly BindableProperty PinsProperty = BindableProperty.Create(
        nameof(Pins), typeof(IReadOnlyList<MapPin>), typeof(YandexMapView), Array.Empty<MapPin>());

    public static readonly BindableProperty NightModeProperty = BindableProperty.Create(
        nameof(NightMode), typeof(bool), typeof(YandexMapView), false);

    public static readonly BindableProperty IsUserLocationVisibleProperty = BindableProperty.Create(
        nameof(IsUserLocationVisible), typeof(bool), typeof(YandexMapView), false);

    public GeoPoint Center { get => (GeoPoint)GetValue(CenterProperty); set => SetValue(CenterProperty, value); }
    public float Zoom { get => (float)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public IReadOnlyList<MapPin> Pins { get => (IReadOnlyList<MapPin>)GetValue(PinsProperty); set => SetValue(PinsProperty, value); }
    public bool NightMode { get => (bool)GetValue(NightModeProperty); set => SetValue(NightModeProperty, value); }
    public bool IsUserLocationVisible { get => (bool)GetValue(IsUserLocationVisibleProperty); set => SetValue(IsUserLocationVisibleProperty, value); }

    public event EventHandler<MapTappedEventArgs>? MapTapped;
    public event EventHandler<string>? PinTapped;
    public event EventHandler? CameraIdle;

    internal void RaiseMapTapped(GeoPoint p) => MapTapped?.Invoke(this, new MapTappedEventArgs(p));
    internal void RaisePinTapped(string id) => PinTapped?.Invoke(this, id);
    internal void RaiseCameraIdle() => CameraIdle?.Invoke(this, EventArgs.Empty);
}
```

### 4.3 Handler (общая часть + Android + iOS)

```csharp
// YandexMapViewHandler.cs
namespace Yandex.MapKit.Maui;

using Microsoft.Maui.Handlers;
#if ANDROID
using PlatformView = Yandex.MapKit.MapView.MapView;
#elif IOS
using PlatformView = Yandex.MapKit.iOS.YMKMapView;
#else
using PlatformView = System.Object;
#endif

public sealed partial class YandexMapViewHandler() : ViewHandler<YandexMapView, PlatformView>(Mapper)
{
    public static readonly IPropertyMapper<YandexMapView, YandexMapViewHandler> Mapper =
        new PropertyMapper<YandexMapView, YandexMapViewHandler>(ViewMapper)
        {
            [nameof(YandexMapView.Center)] = MapCamera,
            [nameof(YandexMapView.Zoom)] = MapCamera,
            [nameof(YandexMapView.Pins)] = MapPins,
            [nameof(YandexMapView.NightMode)] = MapNightMode,
            [nameof(YandexMapView.IsUserLocationVisible)] = MapUserLocation,
        };

    private bool _suppressCameraSync; // true, пока камеру двигает пользователь (не зацикливаем Move)

    private static partial void MapCamera(YandexMapViewHandler h, YandexMapView v);
    private static partial void MapPins(YandexMapViewHandler h, YandexMapView v);
    private static partial void MapNightMode(YandexMapViewHandler h, YandexMapView v);
    private static partial void MapUserLocation(YandexMapViewHandler h, YandexMapView v);
}
```

```csharp
// Platforms/Android/YandexMapViewHandler.Android.cs
namespace Yandex.MapKit.Maui;

using Yandex.MapKit.Geometry;
using Yandex.MapKit.Map;
using AMapView = Yandex.MapKit.MapView.MapView;

public sealed partial class YandexMapViewHandler
{
    // Сильные ссылки на листенеры и объекты — MapKit держит их слабо
    private TapListener? _tapListener;
    private CameraListener? _cameraListener;
    private UserLocationLayer? _userLocation;
    private readonly Dictionary<string, PlacemarkMapObject> _placemarks = [];

    protected override AMapView CreatePlatformView()
    {
        YandexMapKitLifecycle.EnsureInitialized(Context);
        return new AMapView(Context);
    }

    protected override void ConnectHandler(AMapView platformView)
    {
        base.ConnectHandler(platformView);
        var map = platformView.MapWindow.Map;
        _tapListener = new TapListener(p => VirtualView.RaiseMapTapped(p));
        _cameraListener = new CameraListener(this);
        map.AddInputListener(_tapListener);
        map.AddCameraListener(_cameraListener);
        platformView.OnStart();
        YandexMapKitLifecycle.Attach(platformView);   // дальше onStart/onStop по Activity
    }

    protected override void DisconnectHandler(AMapView platformView)
    {
        var map = platformView.MapWindow.Map;
        if (_tapListener is not null) map.RemoveInputListener(_tapListener);
        if (_cameraListener is not null) map.RemoveCameraListener(_cameraListener);
        YandexMapKitLifecycle.Detach(platformView);
        platformView.OnStop();
        _placemarks.Clear();
        _tapListener = null; _cameraListener = null; _userLocation = null;
        base.DisconnectHandler(platformView);
    }

    private static partial void MapCamera(YandexMapViewHandler h, YandexMapView v)
    {
        if (h._suppressCameraSync) return;
        var pos = new CameraPosition(new Point(v.Center.Latitude, v.Center.Longitude), v.Zoom, 0f, 0f);
        h.PlatformView.MapWindow.Map.Move(pos);
    }

    private static partial void MapPins(YandexMapViewHandler h, YandexMapView v) { /* diff по Id: add/update/remove placemark */ }
    private static partial void MapNightMode(YandexMapViewHandler h, YandexMapView v) => h.PlatformView.MapWindow.Map.NightModeEnabled = v.NightMode;

    private static partial void MapUserLocation(YandexMapViewHandler h, YandexMapView v)
    {
        h._userLocation ??= MapKitFactory.Instance.CreateUserLocationLayer(h.PlatformView.MapWindow);
        h._userLocation.Visible = v.IsUserLocationVisible;
    }

    private sealed class TapListener(Action<GeoPoint> onTap) : Java.Lang.Object, IInputListener
    {
        public void OnMapTap(Map map, Point p) => onTap(new GeoPoint(p.Latitude, p.Longitude));
        public void OnMapLongTap(Map map, Point p) { }
    }

    private sealed class CameraListener(YandexMapViewHandler handler) : Java.Lang.Object, ICameraListener
    {
        public void OnCameraPositionChanged(Map map, CameraPosition pos, CameraUpdateReason reason, bool finished)
        {
            if (reason != CameraUpdateReason.Gestures) return;
            handler._suppressCameraSync = true;
            try
            {
                handler.VirtualView.Center = new GeoPoint(pos.Target.Latitude, pos.Target.Longitude);
                handler.VirtualView.Zoom = pos.Zoom;
                if (finished) handler.VirtualView.RaiseCameraIdle();
            }
            finally { handler._suppressCameraSync = false; }
        }
    }
}
```

```csharp
// Platforms/Android/YandexMapKitLifecycle.cs — единая точка lifecycle на процесс
namespace Yandex.MapKit.Maui;

using AMapView = Yandex.MapKit.MapView.MapView;

internal static class YandexMapKitLifecycle
{
    private static bool _initialized;
    private static readonly List<WeakReference<AMapView>> Views = [];

    public static void EnsureInitialized(Android.Content.Context ctx)
    {
        if (_initialized) return;
        MapKitFactory.Initialize(ctx);
        MapKitFactory.Instance.OnStart();
        _initialized = true;
    }

    public static void Attach(AMapView v) => Views.Add(new(v));
    public static void Detach(AMapView v) => Views.RemoveAll(w => !w.TryGetTarget(out var t) || t == v);

    public static void OnActivityStart()
    {
        if (!_initialized) return;
        MapKitFactory.Instance.OnStart();
        foreach (var w in Views) if (w.TryGetTarget(out var v)) v.OnStart();
    }

    public static void OnActivityStop()
    {
        if (!_initialized) return;
        foreach (var w in Views) if (w.TryGetTarget(out var v)) v.OnStop();
        MapKitFactory.Instance.OnStop();
    }
}
```

```csharp
// Platforms/iOS/YandexMapViewHandler.iOS.cs (сокращённо)
namespace Yandex.MapKit.Maui;

using Yandex.MapKit.iOS;

public sealed partial class YandexMapViewHandler
{
    private InputListener? _input;

    protected override YMKMapView CreatePlatformView()
        => new(CoreGraphics.CGRect.Empty, vulkanPreferred: ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR);

    protected override void ConnectHandler(YMKMapView platformView)
    {
        base.ConnectHandler(platformView);
        _input = new InputListener(p => VirtualView.RaiseMapTapped(p));
        platformView.MapWindow.Map.AddInputListener(_input);
    }

    protected override void DisconnectHandler(YMKMapView platformView)
    {
        if (_input is not null) platformView.MapWindow.Map.RemoveInputListener(_input);
        _input = null;
        base.DisconnectHandler(platformView);
    }

    private static partial void MapCamera(YandexMapViewHandler h, YandexMapView v)
    {
        if (h._suppressCameraSync) return;
        h.PlatformView.MapWindow.Map.Move(
            YMKCameraPosition.Create(YMKPoint.Create(v.Center.Latitude, v.Center.Longitude), v.Zoom, 0, 0));
    }

    private static partial void MapPins(YandexMapViewHandler h, YandexMapView v) { /* diff по Id */ }
    private static partial void MapNightMode(YandexMapViewHandler h, YandexMapView v) { /* map.nightModeEnabled */ }
    private static partial void MapUserLocation(YandexMapViewHandler h, YandexMapView v) { /* YMKUserLocationLayer */ }

    private sealed class InputListener(Action<GeoPoint> onTap) : YMKMapInputListener
    {
        public override void OnMapTap(YMKMap map, YMKPoint point) => onTap(new GeoPoint(point.Latitude, point.Longitude));
        public override void OnMapLongTap(YMKMap map, YMKPoint point) { }
    }
}
```

### 4.4 Регистрация и жизненный цикл

```csharp
namespace Yandex.MapKit.Maui;

using Microsoft.Maui.LifecycleEvents;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseYandexMapKit(this MauiAppBuilder builder, string apiKey)
    {
        builder.ConfigureMauiHandlers(h => h.AddHandler<YandexMapView, YandexMapViewHandler>());
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(a => a
                .OnStart(_ => YandexMapKitLifecycle.OnActivityStart())
                .OnStop(_ => YandexMapKitLifecycle.OnActivityStop()));
            // SetApiKey — в MainApplication.OnCreate (раньше любого initialize)
#elif IOS
            events.AddiOS(i => i.FinishedLaunching((_, _) =>
            {
                Yandex.MapKit.iOS.YMKMapKit.SetApiKey(apiKey);
                _ = Yandex.MapKit.iOS.YMKMapKit.SharedInstance;
                return true;
            }));
#endif
        });
        return builder;
    }
}
```

Важно: по документации Microsoft, `DisconnectHandler` «is intentionally not invoked by .NET MAUI». Вызывайте `handler.DisconnectHandler()` сами при уходе со страницы (`Unloaded`/`NavigatedFrom`), иначе листенеры и нативные объекты утекут.

---

## 5. Существующие решения

| Решение | Платформа | Версия / обновление | Состояние | Пригодность |
|---|---|---|---|---|
| NuGet `Yandex.Maps` (Yandex) | Windows Phone 7 | 1.2.4721.1342, 07.12.2012 | Мёртв | ❌ |
| `pocheshire/Xamarin.YandexMaps.iOS` | Xamarin.iOS, старый `yandexmapkit-ios` (2.x) | 0 звёзд, релизов нет | Заброшен | ❌ (только как пример) |
| `yandexmobile/yandexmapkit-android` | MapKit 2.5.4 (Java, Eclipse) | «Эта версия больше не поддерживается» | Архивный | ❌ |
| `yandex/yandex_maps_mapkit_lite` | **Официальный Flutter-плагин Lite** (Dart + codegen + android/ios) | 42 коммита, 12 звёзд | Поддерживается | ✅ референс по интеграции и составу API Lite |
| `yandex/mapkit-android-demo`, `yandex/mapkit-ios-demo` | Официальные примеры на Kotlin/Swift, `assembleLiteRelease` | Актуальны | Поддерживаются | ✅ референс кода |
| `yandex/mapkit-ios-lite` | Официальный SPM-пакет Lite | Обновлён 18.09.2026 | Поддерживается | ✅ источник xcframework и checksum |
| `c-villain/YandexMapsMobileLite` | Неофициальный SPM-пакет | Архивирован 14.03.2026 с пометкой «use the official SDK» | Мёртв | ❌ |
| `sulg-ik/yandex-mapkit-kmp` | Kotlin Multiplatform-обёртка **только для Lite** (+ Compose) | Актуальная | Сторонняя | ✅ чек-лист типов Lite (StorageManager, OfflineCacheManager, ImageProvider…) |

Итог: для .NET всё придётся делать самостоятельно. MAUI-пакета нет ни на NuGet, ни на GitHub.

---

## 6. Подводные камни

| # | Тема | Проблема | Решение |
|---|---|---|---|
| 1 | API-ключ | Ключ легко извлекается из APK/IPA | Не коммитить: генерировать `BuildSecrets.g.cs` из секрета CI или `dotnet user-secrets` через MSBuild. Следить за статистикой в Dashboard |
| 2 | Порядок инициализации (Android) | `setApiKey` после `initialize` → крэш | `SetApiKey` в `MainApplication.OnCreate` |
| 3 | Слабые ссылки на листенеры | GC может собрать Java-peer (Android) или NSObject (iOS); колбэки молча пропадают | Хранить в полях handler'а; `Remove*Listener` в DisconnectHandler |
| 4 | R8/ProGuard | Yandex: «MapKit has this rules embedded in .aar» | При `AndroidLinkTool=r8` проверить, что `proguard.txt` из AAR подхватился; при проблемах добавить `ProguardConfiguration` с `-keep class com.yandex.** { *; }` |
| 5 | Тримминг .NET | Биндинг-типы, которые вызываются только из Java (листенеры), могут быть вырезаны | Ссылаться на них явно из кода; при необходимости `[DynamicDependency]` |
| 6 | NativeAOT на iOS / Mono AOT | Протоколы-модели и блоки `Action<bool>` поддерживаются штатно; reflection в своём коде избегать | Прогнать Release+AOT на устройстве до сдачи MVP; NativeAOT (`PublishAot`) — после стабилизации |
| 7 | 64-bit / ABI и 16 KB | Google Play требует 64-bit; MAUI по умолчанию собирает 4 RID; страницы памяти 16 KB поддерживаются с 4.19.0 | `<RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>` → APK меньше |
| 8 | Устройства без Google Play Services | Зависимости `play-services-location` и `integrity`; без GMS геолокация может не работать | Тест на целевых устройствах; при необходимости свой `LocationManager` |
| 9 | Privacy manifest (iOS) | Без манифеста App Store отклонит приложение | `PrivacyInfo.xcprivacy` в `Platforms/iOS` как `BundleResource` |
| 10 | Язык карты | «The mapkit can only use one language at a time»; после старта сменить нельзя | `SetLocale` до инициализации |
| 11 | Доступность сервисов | Все данные идут с серверов Yandex (РФ). Возможны блокировки корпоративным прокси/файрволом и регуляторные ограничения в отдельных юрисдикциях | Согласовать список доменов с ИБ; для офлайна — платная лицензия + OfflineCacheManager |
| 12 | Обновления SDK | Частые релизы; ломающие изменения бывают и в минорных версиях (4.36.0 — enum `Action`, 4.41.0 — `WeakRef`) | Фиксировать версию; обновлять раз в квартал с регрессионной проверкой биндингов |

### NaviKit (вне скоупа)

| Аспект | Факт |
|---|---|
| Артефакт | Тот же `maps.mobile` / `YandexMapsMobile`, но вариант `-navikit` (например, `4.26.1-navikit`), плюс pod'ы `YMKStylingAutomotiveNavigation` и `YMKStylingRoadEvents` |
| Совместимость с Lite | **Нет**: это взаимоисключающий вариант того же артефакта (надмножество Full). Переход = замена AAR/xcframework и перегенерация биндингов. API Lite входит в NaviKit, поэтому MAUI-слой переход переживёт |
| Ключ | Нужна отдельная активация: «contact us at paid-api-maps@yandex-team.ru» |
| Тарификация | «NaviKit can be calculated based on MAU, the number of trips, or the number of vehicles» — только по договору |
| Рекомендация | Сразу заложить в биндинг-проекты параметр `$(YandexMapKitVariant)` = `lite|full|navikit` |

---

## 7. Структура решения и план

### 7.1 Структура решения

```
Yandex.MapKit.sln
├─ Directory.Build.props            # LangVersion latest, Nullable enable, TreatWarningsAsErrors
├─ Directory.Packages.props         # CPM: версии AndroidX/GMS NuGet
├─ .editorconfig                    # file-scoped namespaces, sealed по умолчанию
├─ src/
│  ├─ Yandex.MapKit.Android.Binding/     # net10.0-android; AndroidMavenLibrary 4.45.0-lite; Transforms/Metadata.xml
│  ├─ Yandex.MapKit.iOS.Binding/         # net10.0-ios; ApiDefinition.cs, StructsAndEnums.cs, native/*.xcframework
│  └─ Yandex.MapKit.Maui/                # net10.0-android;net10.0-ios; YandexMapView, Handler, Lifecycle, UseYandexMapKit()
├─ samples/Yandex.MapKit.Sample/         # MAUI-приложение: карта, пины, тап, камера, user location
└─ tools/fetch-ios-sdk.sh                # curl zip по URL из podspec + проверка checksum
```

Можно собирать сразу под `net9.0-*;net10.0-*`, но если поддержка .NET 9 не требуется, берите только .NET 10 — матрица CI будет меньше. `AndroidMavenLibrary` работает начиная с .NET 9.

### 7.2 План и оценка (один опытный .NET-разработчик, есть Mac)

| Этап | Результат | Оценка |
|---|---|---|
| 0. Лицензия и ИБ | Решение по тарифу, список доменов, ключ | 0,5–2 дня (+ время юристов) |
| 1. Первый прототип Android | `AndroidMavenLibrary`, закрытые `XA4241/42`, пустая карта в MAUI | 2–3 дня |
| 2. Android: нужное подмножество API | Metadata.xml, namespace, листенеры, placemark, камера, user location | 3–5 дней |
| 3. Первый прототип iOS | Скрипт загрузки xcframework, `NativeReference`, минимальный `ApiDefinition` → карта на устройстве и arm64-симуляторе | 2–3 дня |
| 4. iOS: нужное подмножество API | Sharpie → ручная чистка 30–60 типов, протоколы, enums | 4–6 дней |
| 5. MAUI-слой | `YandexMapView`, handler'ы, жизненный цикл, diff пинов, события | 3–5 дней |
| 6. Подготовка к релизу | R8, тримминг, AOT на устройствах, privacy manifest, размер, RID | 2–3 дня |
| 7. Упаковка | NuGet-пакеты, CI (macOS runner), README | 1–2 дня |
| **Итого MVP** | | **≈ 17–29 рабочих дней (3,5–6 недель)** |

Порядок: сначала Android — он дешевле и быстрее даёт обратную связь по API. На iOS первый прототип собирайте на **реальном устройстве**: проблемы линковки статического C++-фреймворка проявляются только там.

## Caveats
- **Не проверено по первоисточнику:**
  - содержимое podspec 4.45.0-lite (точные `frameworks`, `libraries`, URL в `source`);
  - `Package.swift` в `mapkit-ios-lite` (URL, checksum, минимальная iOS);
  - POM именно 4.45.0-lite (данные взяты из POM 4.23.0-lite и 4.45.0-navikit-flutter);
  - наличие `PrivacyInfo.xcprivacy` в xcframework;
  - абсолютный размер Lite.

  Всё это нужно проверить на шаге 3 плана.
- То, что iOS-фреймворк статический, выведено по косвенным признакам: ошибки линковщика в сторонних проектах, требование `-ObjC` и ручной линковки системных фреймворков.
- Селекторы в `ApiDefinition.cs` и имена C#-членов в Android-коде восстановлены по Swift/Kotlin-документации. Это шаблон, а не результат реальной генерации.
- Названия NuGet-пакетов для транзитивных зависимостей — стандартные биндинги Microsoft; версии нужно подобрать под POM.
- Лицензионные условия пересказаны по официальным страницам Yandex. Для государственного проекта окончательную трактовку должны дать юрист и менеджер Yandex Maps API.

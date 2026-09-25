# Yandex MapKit Lite для .NET MAUI

`YandexMapView` — контрол карты Яндекса для .NET MAUI (.NET 10, Android и iOS) поверх
Yandex MapKit **4.45.0-lite**. В репозитории: собственные биндинги нативных SDK, MAUI-контрол
с API в стиле `Microsoft.Maui.Controls.Maps`, пример приложения и CI.

| NuGet-пакет | Что внутри |
|---|---|
| `YandexMapKit.Maui.Community` | `YandexMapView`, пины, фигуры, кластеризация, `UseYandexMapKit()` |
| `YandexMapKit.Maui.Community.Binding.Android` | .NET-биндинг `com.yandex.android:maps.mobile:4.45.0-lite` (namespace `Com.Yandex.Mapkit.*`) |
| `YandexMapKit.Maui.Community.Binding.iOS` | .NET-биндинг `YandexMapsMobile.xcframework` (namespace `YandexMapsMobile`, подмножество API карты) |

Неофициальные community-пакеты, не связанные с Яндексом. Биндинги содержат Yandex MapKit SDK,
его использование регулируется [условиями Yandex Maps API](https://yandex.ru/legal/maps_api/).

```bash
dotnet add package YandexMapKit.Maui.Community --prerelease
```

> ⚠️ **Лицензия MapKit.** Бесплатный тариф — только для проектов с открытым доступом, без мониторинга
> и диспетчеризации, без офлайна; до 25K MAU. Для закрытых/корпоративных приложений нужна коммерческая
> лицензия. Логотип Яндекса и ссылки в нижних углах карты нельзя перекрывать (даже с платной лицензией),
> а в разделе «О приложении» нужна ссылка на https://yandex.ru/legal/maps_api/.
> Подробности — в [отчёте](Yandex%20MapKit%20Lite%20в%20.NET%20MAUI.md), §1.7.

## Возможности

- камера: `Center`, `Zoom`, `Azimuth`, `Tilt` (TwoWay — обновляются жестами), `MoveTo(...)` с анимацией, событие `CameraChanged`;
- пины: `Pins` (`ObservableCollection<MapPin>`) или `ItemsSource` + `ItemTemplate`/`ItemTemplateSelector`;
- иконки пинов из любого `ImageSource` (файл, ресурс, URI, поток, шрифт; SVG растрируется MAUI),
  без иконки — стандартный маркер цвета `MapPin.Color`; подпись `Label`, `Anchor`, `IconScale`, `ZIndex`;
- кластеризация: `IsClusteringEnabled`, `ClusterRadius`, `ClusterMinZoom`, `ClusterColor`, событие `ClusterClicked`
  (по умолчанию — приближение к кластеру);
- фигуры в `MapElements`: `MapPolyline`, `MapPolygon`, `MapCircle` (цвет/толщина линии, заливка, `Clicked`);
- ночной режим, слой геопозиции пользователя (`IsShowingUser`), включение/выключение жестов;
- события `MapClicked`, `MapLongClicked`, `PinClicked`, `MapPin.MarkerClicked`.

Спутникового слоя нет: в MapKit только схема и пользовательские слои. Поиска и маршрутов в Lite нет.

## Быстрый старт

1. Получите ключ **MapKit Mobile SDK** в [кабинете разработчика](https://developer.tech.yandex.ru/services/)
   (активация ~15 минут). Не храните ключ в git.

2. Подключите контрол в `MauiProgram.cs`:

   ```csharp
   builder
       .UseMauiApp<App>()
       .UseYandexMapKit(apiKey, locale: "ru_RU"); // язык карты задаётся один раз на процесс
   ```

3. Добавьте карту на страницу:

   ```xml
   <ContentPage xmlns:ym="clr-namespace:Yandex.MapKit.Maui;assembly=Yandex.MapKit.Maui" ...>
       <ym:YandexMapView x:Name="Map"
                         Center="55.751244, 37.618423"
                         Zoom="12"
                         ItemsSource="{Binding Places}"
                         IsClusteringEnabled="True"
                         PinClicked="OnPinClicked">
           <ym:YandexMapView.ItemTemplate>
               <DataTemplate x:DataType="local:Place">
                   <ym:MapPin Location="{Binding Location}" Label="{Binding Name}" Icon="landmark.png" />
               </DataTemplate>
           </ym:YandexMapView.ItemTemplate>
       </ym:YandexMapView>
   </ContentPage>
   ```

   ```csharp
   Map.Pins.Add(new MapPin { Location = new GeoPoint(55.76, 37.62), Label = "Большой театр", Color = Colors.Blue });

   var route = new MapPolyline { StrokeColor = Colors.Red, StrokeWidth = 5 };
   route.Geopath.Add(new GeoPoint(55.760, 37.618));
   route.Geopath.Add(new GeoPoint(55.753, 37.620));
   Map.MapElements.Add(route);

   Map.MapElements.Add(new MapCircle { Center = new GeoPoint(55.73, 37.60), Radius = 500 });

   Map.MoveTo(new GeoPoint(55.75, 37.61), zoom: 14);
   ```

4. Геопозиция: разрешение приложение запрашивает само
   (`await Permissions.RequestAsync<Permissions.LocationWhenInUse>()`), затем `Map.IsShowingUser = true`.
   - Android: `ACCESS_FINE_LOCATION`/`ACCESS_COARSE_LOCATION` в манифесте (FINE добавляет AAR MapKit).
   - iOS: `NSLocationWhenInUseUsageDescription` в `Info.plist`.

5. iOS: добавьте `Platforms/iOS/PrivacyInfo.xcprivacy` (см. пример) — без него App Store отклонит сборку.

## Сборка из исходников

Требования: .NET 10 SDK, workload'ы `maui-android` / `maui` (iOS — только на macOS с актуальным Xcode).

```bash
dotnet workload install maui-android            # или: maui (Android + iOS) на macOS

# Android: AAR скачивается из Maven Central автоматически (AndroidMavenLibrary)
dotnet build src/Yandex.MapKit.Android.Binding

# iOS: xcframework и ресурсы скачиваются скриптом (URL и SHA-256 из yandex/mapkit-ios-lite)
tools/fetch-ios-sdk.sh
python3 tools/verify-ios-selectors.py            # сверка селекторов биндинга с заголовками SDK
dotnet build src/Yandex.MapKit.iOS.Binding

# Тесты (без мобильных workload'ов)
dotnet test tests/Yandex.MapKit.Maui.Tests -p:YandexCoreOnly=true

# Пример: ключ передаётся при сборке и не попадает в git
dotnet build samples/Yandex.MapKit.Sample -f net10.0-android -p:YandexMapKitApiKey=<ключ>
```

Ключ для примера можно также задать переменной окружения `YANDEX_MAPKIT_API_KEY` или в файле
`samples/Yandex.MapKit.Sample/secrets.user.props` (шаблон — `secrets.user.props.example`, файл в `.gitignore`).

## Публикация

Workflow `Publish NuGet` (ручной запуск или тег `v*`) собирает три пакета на macOS и публикует их
на nuget.org; нужен секрет репозитория `NUGET_API_KEY`. Версии: `VersionPrefix` в проектах +
`VersionSuffix` (по умолчанию `preview.1`, для стабильного релиза — пустой).

## Структура

```
src/Yandex.MapKit.Android.Binding   AndroidMavenLibrary + Transforms/Metadata.xml
src/Yandex.MapKit.iOS.Binding       ApiDefinition.cs, StructsAndEnums.cs, native/ (скачивается)
src/Yandex.MapKit.Maui              Core/ (общий API), Handlers/, Platforms/Android|iOS/ (handler'ы)
tests/Yandex.MapKit.Maui.Tests      xUnit: синхронизация коллекций, ItemsSource, камера, кластеры
samples/Yandex.MapKit.Sample        демо-приложение
tools/                              загрузка iOS SDK, проверка селекторов
docs/design/                        дизайн-документ
```

## Как это устроено

- **Жизненный цикл.** `UseYandexMapKit` сохраняет ключ и подписывается на lifecycle-события.
  Android: `MapKitFactory.SetApiKey/SetLocale/Initialize` вызываются лениво при создании первой карты,
  `onStart/onStop` MapKit и всех живых `MapView` — по `OnStart/OnStop` Activity (иначе вместо карты пустая сетка).
  iOS: инициализация в `WillFinishLaunching`, до создания окна.
- **Листенеры.** С 4.41 MapKit хранит листенеры слабо (`WeakReference` / `__weak`); handler держит на них
  сильные ссылки и отписывается в `DisconnectHandler` (в .NET 10 MAUI вызывает его автоматически при уходе со страницы).
- **Синхронизация.** `CollectionSynchronizer<T>` переводит изменения `Pins`/`MapElements` и свойств элементов
  в вызовы платформенных адаптеров; кластеризация пересчитывается одним вызовом на пачку изменений.
- **Биндинги.** Android-биндинг генерируется из AAR; `Metadata.xml` убирает `*.internal`, сериализационные
  хелперы и приводит имена пакетов к PascalCase. iOS-биндинг написан вручную (≈30 типов); так как
  ошибочный селектор проявляется только в рантайме, CI сверяет каждый `[Export]` с заголовками SDK.

## Известные ограничения

- Без Google Play Services геопозиция на Android может не работать (зависимость MapKit).
- MapKit поддерживает только растровые иконки; SVG из `Resources/Images` MAUI превращает в PNG при сборке.
- Google Play Services тянут AndroidX новее, чем закреплены в MAUI (Fragment 1.9, Collection 1.6 и т.д.);
  Android-биндинг явно поднимает устаревшие `*-ktx` пакеты, иначе D8 падает с «Type … is defined multiple times».
- Windows / Mac Catalyst не поддерживаются (нет MapKit SDK).

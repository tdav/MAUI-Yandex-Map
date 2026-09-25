#!/usr/bin/env bash
# Downloads Yandex MapKit iOS SDK (xcframework + resource bundle) into src/Yandex.MapKit.iOS.Binding/native.
# URL and SHA-256 are the ones published in https://github.com/yandex/mapkit-ios-lite (Package.swift) /
# the YandexMapsMobile podspec. Update both together with YandexMapKitVersion in Directory.Build.props.
set -euo pipefail

VERSION="${YANDEX_MAPKIT_VERSION:-4.45.0}"
VARIANT="${YANDEX_MAPKIT_VARIANT:-lite}"
SHA256="${YANDEX_MAPKIT_SHA256:-0b0478a73284d210904bdb3f32c283d7c0de8ebd745d9f396b51ddca2ae74c8e}"
URL="https://maps-ios-pods-public.s3.yandex.net/YandexMapsMobile-${VERSION}-${VARIANT}.framework.zip"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="$ROOT/src/Yandex.MapKit.iOS.Binding/native"
STAMP="$DEST/.version"

if [[ -f "$STAMP" && "$(cat "$STAMP")" == "$VERSION-$VARIANT" && -d "$DEST/YandexMapsMobile.xcframework" ]]; then
  echo "Yandex MapKit iOS $VERSION-$VARIANT is already in $DEST"
  exit 0
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Downloading $URL"
curl -fL --retry 4 --retry-delay 2 -o "$TMP/sdk.zip" "$URL"

ACTUAL="$(shasum -a 256 "$TMP/sdk.zip" 2>/dev/null | cut -d' ' -f1 || sha256sum "$TMP/sdk.zip" | cut -d' ' -f1)"
if [[ "$ACTUAL" != "$SHA256" ]]; then
  echo "SHA-256 mismatch: expected $SHA256, got $ACTUAL" >&2
  exit 1
fi

unzip -q "$TMP/sdk.zip" -d "$TMP/sdk"

XCFRAMEWORK="$(find "$TMP/sdk" -maxdepth 3 -type d -name '*.xcframework' | head -n 1)"
if [[ -z "$XCFRAMEWORK" ]]; then
  echo "No .xcframework found in the archive" >&2
  exit 1
fi
BUNDLE="$(find "$TMP/sdk" -maxdepth 3 -type d -name 'YandexMapsMobile.bundle' | head -n 1)"

rm -rf "$DEST"
mkdir -p "$DEST"
# The podspec renames YandexMapsMobileLite.xcframework → YandexMapsMobile.xcframework; do the same.
mv "$XCFRAMEWORK" "$DEST/YandexMapsMobile.xcframework"
if [[ -n "$BUNDLE" ]]; then
  mv "$BUNDLE" "$DEST/YandexMapsMobile.bundle"
else
  echo "warning: YandexMapsMobile.bundle not found in the archive" >&2
fi
echo "$VERSION-$VARIANT" > "$STAMP"

echo "Installed into $DEST:"
ls "$DEST" "$DEST/YandexMapsMobile.xcframework"

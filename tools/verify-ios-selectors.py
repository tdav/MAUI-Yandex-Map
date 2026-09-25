#!/usr/bin/env python3
"""Checks the hand-written iOS binding against the YandexMapsMobile headers.

A wrong selector in ApiDefinition.cs compiles fine and only fails at run time with
"unrecognized selector". This script parses the Objective-C headers of the downloaded
xcframework and verifies that:

* every class/protocol bound in ApiDefinition.cs exists (and has the right kind);
* every [Export]/[Bind] selector exists in the headers as a method or property accessor
  (class hierarchy is not followed — a selector declared anywhere is accepted);
* every [Native] enum in StructsAndEnums.cs matches the header's members, order and signedness.

Usage: verify-ios-selectors.py [path/to/YandexMapsMobile.xcframework]
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
BINDING = ROOT / "src" / "Yandex.MapKit.iOS.Binding"
DEFAULT_XCFRAMEWORK = BINDING / "native" / "YandexMapsMobile.xcframework"


def strip_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    return re.sub(r"//[^\n]*", " ", text)


def parse_headers(xcframework: pathlib.Path):
    headers = sorted(xcframework.glob("ios-arm64/*.framework/Headers/**/*.h"))
    if not headers:
        headers = sorted(xcframework.glob("**/Headers/**/*.h"))
    if not headers:
        sys.exit(f"No headers found under {xcframework}")

    classes: set[str] = set()
    protocols: set[str] = set()
    selectors: set[str] = set()
    enums: dict[str, tuple[str, list[str]]] = {}

    for header in headers:
        text = strip_comments(header.read_text(errors="replace"))
        text = re.sub(r"__attribute__\s*\(\(.*?\)\)", " ", text, flags=re.S)

        classes.update(re.findall(r"@interface\s+(\w+)\s*(?::|\()", text))
        classes.update(re.findall(r"@interface\s+(\w+)\s*$", text, flags=re.M))
        protocols.update(re.findall(r"@protocol\s+(\w+)\s*[<\n{]", text))

        for m in re.finditer(r"typedef\s+NS_(?:ENUM|OPTIONS)\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)\s*\{(.*?)\}", text, flags=re.S):
            underlying, name, body = m.group(1), m.group(2), m.group(3)
            members = [re.split(r"[\s=]", item.strip())[0] for item in body.split(",") if item.strip()]
            enums[name] = (underlying, members)

        # Methods: "- (type)part1:(type)arg part2:(type)arg ... ;"
        for m in re.finditer(r"^\s*[-+]\s*\([^;{]*?\)\s*([^;{]+?)\s*(?:NS_\w+(?:\([^)]*\))?\s*)*;", text, flags=re.M | re.S):
            decl = " ".join(m.group(1).split())
            parts = re.findall(r"(\w+)\s*:", decl)
            if parts:
                selectors.add("".join(p + ":" for p in parts))
            else:
                selectors.add(re.match(r"\w+", decl).group(0))

        # Properties: "@property (attrs) type *name;"
        for m in re.finditer(r"@property\s*(\(([^)]*)\))?\s*([^;]+);", text):
            attrs = m.group(2) or ""
            name = re.findall(r"(\w+)\s*(?:NS_\w+(?:\([^)]*\))?\s*)*$", m.group(3).strip())
            if not name:
                continue
            name = name[0]
            getter = re.search(r"getter\s*=\s*(\w+)", attrs)
            setter = re.search(r"setter\s*=\s*(\w+:)", attrs)
            selectors.add(getter.group(1) if getter else name)
            if "readonly" not in attrs:
                selectors.add(setter.group(1) if setter else f"set{name[0].upper()}{name[1:]}:")

    return classes, protocols, selectors, enums


def parse_binding():
    text = strip_comments((BINDING / "ApiDefinition.cs").read_text())
    types: list[tuple[str, bool]] = []  # (name, is_protocol)
    for m in re.finditer(r"((?:\[[^\]]*\]\s*)*)interface\s+(\w+)", text):
        attrs, name = m.group(1), m.group(2)
        if "BaseType" not in attrs:
            continue  # IYMK* protocol stubs
        explicit = re.search(r'Name\s*=\s*"(\w+)"', attrs)
        types.append((explicit.group(1) if explicit else name, "Protocol" in attrs))

    exports = []
    for m in re.finditer(r'\[(?:[^\]]*?\b)?(Export|Bind)\s*\(\s*"([^"]+)"', text):
        exports.append((m.group(1), m.group(2)))

    # A read-write [Export("name")] property also needs "setName:" (unless it only has a getter).
    for m in re.finditer(r'Export\s*\(\s*"(\w+)"[^\]]*\]\s*\n\s*[\w\[\]<>?]+\s+\w+\s*\{([^}]*)\}', text):
        name, accessors = m.group(1), m.group(2)
        if re.search(r"\bset\s*;", accessors):
            exports.append(("Export(setter)", f"set{name[0].upper()}{name[1:]}:"))
        if re.search(r"\[Bind", accessors):
            # getter is bound separately; the plain name is not a selector then
            exports.remove(("Export", name))

    enums: dict[str, tuple[str, list[str]]] = {}
    enums_text = strip_comments((BINDING / "StructsAndEnums.cs").read_text())
    for m in re.finditer(r"\[Native\]\s*public\s+enum\s+(\w+)\s*:\s*(\w+)\s*\{(.*?)\}", enums_text, flags=re.S):
        name, underlying, body = m.groups()
        members = [item.strip().split("=")[0].strip() for item in body.split(",") if item.strip()]
        enums[name] = (underlying, members)

    return types, exports, enums


def main() -> int:
    xcframework = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_XCFRAMEWORK
    classes, protocols, selectors, header_enums = parse_headers(xcframework)
    types, exports, binding_enums = parse_binding()

    errors: list[str] = []
    for name, is_protocol in types:
        if is_protocol and name not in protocols:
            errors.append(f"protocol {name} not found in headers")
        elif not is_protocol and name not in classes:
            errors.append(f"class {name} not found in headers")

    for kind, selector in exports:
        if selector not in selectors:
            errors.append(f"{kind} selector '{selector}' not found in headers")

    for name, (underlying, members) in binding_enums.items():
        if name not in header_enums:
            errors.append(f"enum {name} not found in headers")
            continue
        header_underlying, header_members = header_enums[name]
        expected = [f"{name}{member}" for member in members]
        if header_members != expected:
            errors.append(f"enum {name}: binding {expected} != header {header_members}")
        signed = header_underlying in ("NSInteger", "int", "long")
        if signed != (underlying == "long"):
            errors.append(f"enum {name}: header underlying type {header_underlying}, binding {underlying}")

    print(f"Checked {len(types)} types, {len(exports)} selectors, {len(binding_enums)} enums "
          f"against {len(classes)} classes / {len(protocols)} protocols / {len(selectors)} selectors in headers.")
    for error in errors:
        print(f"error: {error}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())

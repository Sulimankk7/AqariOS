# AqariOS Mobile Design System

This directory contains the Flutter-only AqariOS design-system foundation and its internal `/design-system` playground. It implements the approved `docs/AqariOS_Mobile_Design_System_Source_of_Truth.md`; it contains no business features, API integration, or production navigation.

## Decisions

- **Icons:** Flutter's bundled outlined Material Symbols are used for the first foundation. The repository had no Flutter dependencies, and adding an external package without visual/licensing validation would create an unapproved permanent dependency. All icon use is wrapped by AqariOS sizing, color, semantics and directional behavior. A Lucide-compatible package remains a pre-production decision.
- **Fonts:** Official Google Fonts assets are vendored for Tajawal 400/500/700 and variable Roboto Flex. Their OFL files are in `assets/fonts`. Numeric typography uses one replaceable `AqariTypography.numericFamily` seam; `monospace` is a temporary fallback, not a permanent family decision.
- **Platforms:** The repository did not contain a Flutter project and this host had no Flutter/Dart SDK. The source foundation was created manually. Generate platform runner folders with the project's approved Flutter SDK before device execution; do not overwrite `lib`, `test`, `assets`, or `pubspec.yaml`.

## Playground

The only route is `/design-system`. It validates theme, direction, four device widths, colors, typography, tokens, buttons, forms, flat surfaces versus cards, lists, statuses, feedback, navigation primitives, overlays and accessibility stress cases.

## Validation commands

Run once an approved Flutter SDK is available:

```text
flutter pub get
flutter analyze
flutter test
flutter run
```


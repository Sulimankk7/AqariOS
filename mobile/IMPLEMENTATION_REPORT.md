# AqariOS Flutter Design System — Implementation Report

## Production client increment

The app entry point now targets the production client rather than the playground. The internal playground remains available at `/design-system`.

Implemented in the first backend-backed increment:

- Runtime API configuration through `AQARIOS_API_BASE_URL`, with no embedded environment secrets.
- Typed native HTTP client with Bearer injection, RFC 7807-style error normalization, timeouts, a single concurrent refresh operation, and one retry after refresh.
- Access and refresh credentials stored only through platform secure storage; non-persistent sessions are discarded on the next process bootstrap.
- Login, `/auth/me` restoration, refresh-token rotation from `Set-Cookie`, logout, and exact Web-compatible role resolution.
- Arabic-first RTL login, bootstrap/error gates, unsupported-role handling, and a role/permission-aware bottom-navigation shell.
- Real `GET /api/v1/dashboard/summary` company dashboard with typed models, five-minute cache, loading/error/retry states, and pull-to-refresh.
- A route/API/permission delivery matrix at `docs/PRODUCTION_IMPLEMENTATION_MATRIX.md`.

Tenant and system-administrator destinations remain explicit unavailable states; they do not display mock data. Buildings/units are the recommended next vertical slice.

## A. Files created or modified

- Project: `pubspec.yaml`, `analysis_options.yaml`, `.gitignore`, `README.md`.
- Entry and route: `lib/main.dart`, `lib/app.dart`.
- Theme: all files under `lib/core/design_system/theme/`.
- Localization: all files under `lib/core/design_system/localization/`.
- Components: all files under `lib/core/design_system/components/` and the `design_system.dart` barrel.
- Playground: `lib/playground/design_system_playground.dart` and `lib/playground/sections/*`.
- Fonts/licenses: `assets/fonts/Tajawal-{Regular,Medium,Bold}.ttf`, `RobotoFlex-Variable.ttf`, and both OFL license files.
- Tests: `test/theme_tokens_test.dart`, `test/playground_smoke_test.dart`.

No Web, backend, database, API, migration, Docker or business-feature file was modified.

## B. Foundation implementation

- Exact brand primitives and light/dark semantic colors are implemented through `AqariBrand` and the `AqariColors` `ThemeExtension`.
- `AqariTheme` populates Flutter `ColorScheme` for interoperability while keeping `AqariColors` authoritative.
- Runtime light/dark/system and Arabic/English switching are controlled at app level.
- The exact Web-derived typography scale is implemented with locale-aware Tajawal and Roboto Flex.
- Spacing (4–64), shape (4/8/16/28/full), explicit e0–e4 shadows, and 75/150/200/300ms motion tokens are centralized.
- Layout components use logical directional APIs. `AqariLtrContent` isolates email, phone, URL, ID and technical content. `AqariDirectionalIcon` mirrors directional icons only.

## C. Components implemented

- Actions: `AqariButton`, `AqariIconButton`.
- Forms: `AqariTextField`, `AqariSearchField`, `AqariSelect`, `AqariCheckbox`, `AqariRadio`, `AqariSwitch`.
- Information: `AqariStatusBadge`, `AqariAvatar`, `AqariKpi`, `AqariSectionHeader`, `AqariDivider`, `AqariCard`, `AqariSurface`.
- Status: `AqariDomainStatus`, `AqariStatusRegistry`, semantic visual variants.
- Lists: `AqariListRow` with compact/standard/rich density, optional leading/status/trailing/chevron/divider and disabled/pressed behavior.
- Feedback: `AqariLoadingState`, `AqariSkeleton`, `AqariEmptyState`, `AqariErrorState`, `AqariSnackbar`.
- Navigation: `AqariAppBar`, `AqariBottomNavigation`, `AqariTabs`, `AqariPopupMenu`.
- Overlays: `AqariDialog`, `AqariBottomSheet`.

## D. Icon decision

Flutter's bundled outlined Material Symbols are used temporarily. The repository had no mobile dependencies and the host could not execute Flutter package validation. This avoids silently selecting a permanent third-party package. AqariOS wrappers control size, color, touch target, semantics and RTL mirroring, limiting migration cost. Selecting a Lucide-compatible package remains required before production.

## E. Font decision

Official Google Fonts assets are vendored: Tajawal 400/500/700 and variable Roboto Flex, with OFL licenses. No Tajawal 600 is requested. Numeric typography references the single replaceable `AqariTypography.numericFamily` seam; generic monospace is explicitly temporary because the product has not selected a permanent family.

## F. Playground

The internal `/design-system` route includes:

- Live light/dark/system and Arabic RTL/English LTR controls.
- 320, 390, 480 and 768 logical-pixel preview frames.
- Brand/semantic/surface/text/border/status colors with names and HEX.
- Every typography style, mixed content and KPI numerics.
- Spacing, radius, elevation and motion tokens.
- Button variants and default/loading/disabled examples.
- Forms in default, focused, filled, error, disabled and loading states.
- Flat sections, one-surface list groups, meaningful cards and elevated cards.
- Domain-realistic flat list rows without implementing business features.
- Complete currently specified status set, including long Arabic labels.
- Loading, skeleton, empty, error and snackbar feedback.
- App bar, five-item bottom navigation, tabs and popup menu.
- Dialog and bottom-sheet demonstrations.
- Long Arabic/English, mixed direction, technical LTR, icon-only and error accessibility stress cases.

## G. Known limitations

1. Flutter and Dart are not installed on this host; `flutter analyze`, `flutter test`, device rendering and screenshot inspection could not run.
2. Platform runner folders do not exist because the repository had no Flutter project and no SDK was available to generate them safely.
3. The final Lucide-compatible Flutter icon package is unresolved.
4. The permanent numeric monospace family is unresolved.
5. Exact light pressed colors and cross-platform e0–e4 shadow rendering require device visual approval.
6. Persistence of theme/locale preferences is not implemented; the playground switches them for the current process only.
7. Maintenance, utility, subscription and marketplace domain-status registries still require exhaustive module decisions before those modules are built.

## H. Design deviations

| Specification | Implementation | Reason |
|---|---|---|
| Closest validated Lucide-compatible icon package | Bundled outlined Material Symbols behind AqariOS wrappers | No existing project/dependency or Flutter SDK was available for package and visual validation; permanent choice remains open |
| Permanent KPI monospace | Replaceable generic `monospace` seam | Specification explicitly forbids inventing the family |
| Visual inspection on four device classes | Adaptive preview frames implemented; runtime inspection not executed | Flutter SDK/device renderer unavailable on host |
| Generated Android/iOS Flutter project | Source-first `mobile/` project without runners | Cannot safely generate runner files without Flutter tooling |

Static checks confirmed balanced delimiters, no raw colors outside theme/foundation files, no physical left/right layout APIs in design-system Dart source, all required font assets present, and all playground sections split into focused files. Runtime validation commands are recorded in `README.md` for the first environment with an approved Flutter SDK.

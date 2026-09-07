# AqariOS Mobile Design System — Source of Truth

**Status:** design specification only; no Flutter implementation exists or was created.  
**Authority order:** live Web implementation → `AqariOS_Current_Web_Design_System_Audit.md` → this mobile translation. Where the sources disagree, the live Web code wins.  
**Core principle:** **AqariOS Mobile is not a new visual identity. It is AqariOS Web's existing design language translated into a clean, touch-first, mobile-native experience.**

## 1. Executive Summary

AqariOS Mobile preserves the Web product's warm cream surfaces, muted olive primary, earth-brown supporting palette, restrained semantic colors, Tajawal/Roboto typography, moderate enterprise density, and first-class Arabic/RTL behavior. It changes interaction models—not identity: sidebars become mobile navigation, tables become prioritized lists and detail views, filter bars become sheets, hover becomes press, and dense desktop actions become one clear primary action plus overflow.

The mobile system is information-first and deliberately low-noise. Typography, spacing, dividers, and surface hierarchy do most of the work. Cards are reserved for meaningful grouping or interaction boundaries. Flutter's Material widgets may provide behavior and accessibility internally, but every visible value and state must be explicitly themed; no default Material visual styling is authoritative.

## 2. AqariOS Mobile Design Philosophy

1. **Clarity before decoration.** The first viewport communicates identity, status, and the next useful action.
2. **Hierarchy before containers.** Use type, spacing, alignment, and dividers before adding a card.
3. **One dominant action.** A screen or sheet exposes one primary action; secondary actions use text/outlined treatment or overflow.
4. **Progressive disclosure.** Lists show recognition data; detail screens hold the full record; sheets handle short contextual tasks.
5. **Semantic color only.** Brand and status colors communicate action, selection, or meaning—not ornament.
6. **Touch and reach.** Interactive targets are at least 48×48 logical pixels even when the visible icon or field is smaller.
7. **RTL from one layout.** Logical start/end behavior is mandatory; Arabic and English do not use duplicated screens.
8. **Calm density.** Mobile adds touch room without turning enterprise data into oversized promotional UI.

## 3. Existing Web Identity

The implemented Web identity is warm-neutral and property-operations oriented:

- Cream/off-white light surfaces and deep neutral dark surfaces.
- Olive green for primary actions, selection and operational emphasis.
- Earth brown for secondary brand character and warning/tertiary roles.
- Strong but quiet borders; subtle green-tinted shadows.
- Pill buttons and status badges; 8px fields/menus; 16px standard cards.
- Tajawal for Arabic and Roboto Flex/Roboto for Latin.
- Grouped enterprise navigation, dense lists/tables, restrained animation.

The older `docs/AqariOS_Design_System.md` describes an obsolete black/OKLCH/Inter system and is not authoritative.

### Raw color disposition

| Color/group | Web location | Classification | Flutter policy |
|---|---|---|---|
| Approved warm palette | Foundation theme and brand tokens | **KEEP / PROMOTE TO TOKEN** | Preserve as brand primitives; expose semantic roles where used |
| Surface, text, border, semantic light/dark values | `styles/theme.css` | **PROMOTE TO TOKEN** | Required semantic theme tokens |
| `#FFD98A` | Auth architectural illustration, lit windows, dark auth links | **FEATURE-LOCAL** | Do not add to core `AqariColors`; auth/illustration-only constant if that artwork is ported |
| `#111827`, `#374151`, `#6B7280`, `#9CA3AF`, `#E5E7EB` | Auth forms/OTP and pasted design reference | **LEGACY / REVIEW LATER** | Replace by existing semantic text/border roles during mobile design; do not tokenize |
| Raw emerald utilities | Tenant account/payment success fragments | **FEATURE-LOCAL / REVIEW LATER** | Use core success semantics unless a later product requirement proves a distinct meaning |
| `#CCC`, `#FAFAF7`, `#F4F1EA` and ad hoc white/grays | Isolated feature/local use | **LEGACY** | Do not promote; map to semantic surfaces/borders after visual verification |
| Accessibility contrast colors | Foundation text/on-colors | **KEEP** | Preserve exact role pairs; validate contrast in Flutter |

This classification is a mobile translation decision. It does not retroactively change Web behavior.

## 4. Color System

### Brand primitives

These values preserve brand identity but should rarely be consumed directly by components.

| Flutter primitive | HEX | Existing meaning |
|---|---:|---|
| `AqariBrand.brown900` | `#582F0E` | Deepest brown; also light on-error-container |
| `AqariBrand.brown700` | `#7F4F24` | Tertiary and light warning |
| `AqariBrand.brown600` | `#936639` | Light brand secondary |
| `AqariBrand.brown400` | `#A68A64` | Brand primitive only |
| `AqariBrand.brown200` | `#B6AD90` | Light secondary container |
| `AqariBrand.green900` | `#333D29` | Deep green text/container role |
| `AqariBrand.green800` | `#414833` | Light variant text, dark primary |
| `AqariBrand.green600` | `#656D4A` | Light primary |
| `AqariBrand.green400` | `#A4AC86` | Light primary container |
| `AqariBrand.green200` | `#C2C5AA` | Light tertiary container |

### Semantic theme colors

| Flutter token | Light | Dark | Meaning and typical use |
|---|---:|---:|---|
| `primary` | `#656D4A` | `#414833` | Primary actions, selection rail, focus, emphasized icons |
| `onPrimary` | `#FFFFFF` | `#F2F3F0` | Content on primary |
| `primaryContainer` | `#A4AC86` | `#414833` | Selected navigation, tonal actions |
| `onPrimaryContainer` | `#263020` | `#F2F3F0` | Content on primary container |
| `secondary` | `#936639` | `#8D745C` | Actual brand-secondary role; never use Web's neutral `bg-secondary` alias as this token |
| `onSecondary` | `#FFFFFF` | `#F2F3F0` | Content on brand secondary |
| `secondaryContainer` | `#B6AD90` | `#2D2925` | Secondary tonal/status content |
| `onSecondaryContainer` | `#333D29` | `#D6D2CC` | Content on secondary container |
| `tertiary` | `#7F4F24` | `#7E878F` | Tertiary brand/data role |
| `onTertiary` | `#FFFFFF` | `#0E1116` | Content on tertiary |
| `tertiaryContainer` | `#C2C5AA` | `#252B32` | Tertiary tonal content |
| `onTertiaryContainer` | `#333D29` | `#F2F3F0` | Content on tertiary container |
| `background` | `#F7F4EA` | `#0E1116` | Screen background |
| `onBackground` | `#263020` | `#F2F3F0` | Primary screen text |
| `surface` | `#FFFEFA` | `#0E1116` | Base surface |
| `onSurface` | `#263020` | `#F2F3F0` | Primary content |
| `surfaceVariant` | `#E5DECD` | `#181D23` | Distinct neutral grouping |
| `onSurfaceVariant` | `#414833` | `#AEB3AD` | Secondary content |
| `surfaceDim` | `#DDD5C1` | `#0A0D11` | Dim surface role |
| `surfaceBright` | `#FFFEFA` | `#20262D` | Bright/elevated surface role |
| `surfaceLowest` | `#FFFEFA` | `#0B0E12` | Lowest container |
| `surfaceLow` | `#F4F0E3` | `#14191F` | Subtle grouping, app bars/cards |
| `surfaceContainer` | `#ECE7D8` | `#161B21` | Neutral control/section surface |
| `surfaceHigh` | `#E5DECD` | `#181D23` | Menus, stronger grouping |
| `surfaceHighest` | `#DDD4C0` | `#1D232A` | Highest neutral container |
| `textPrimary` | `#263020` | `#F2F3F0` | Alias of on-surface for app-facing API |
| `textSecondary` | `#414833` | `#AEB3AD` | Supporting information |
| `textMuted` | `#59614A` | `#6F756F` | Metadata/placeholders; contrast must be checked at small sizes |
| `textDisabled` | `#747968` | `#6F756F` | Disabled content only |
| `outline` | `#897154` | `#252B32` | Strong border/input outline |
| `outlineVariant` | `#C3B89B` | `#252B32` | Default border/divider |
| `borderMuted` | `#D2C8AE` | `#1D2329` | Subtle separators |
| `focus` | `#656D4A` | `#414833` | Keyboard/accessibility focus indication |
| `disabledContainer` | `#E7E2D5` | `#181D23` | Disabled control fill |
| `scrim` | `#000000` | `#000000` | Modal overlay; apply context opacity, Web baseline 40–55% |

## 5. Semantic Color System

| Flutter token | Light foreground / background | Dark foreground / background | Use |
|---|---|---|---|
| `success` / `successContainer` | `#526341` / `#E1E8D7` | `#9BAE91` / `#1C2820` | Completed, paid, active-positive outcomes |
| `warning` / `warningContainer` | `#7F4F24` / `#F2E3D2` | `#D0AA76` / `#2B241B` | Late, partial, attention needed |
| `error` / `errorContainer` | `#8A3B2E` / `#F4DAD2` | `#E6A29A` / `#3A2424` | Error, destructive, overdue/terminated |
| `info` / `infoContainer` | `#59636F` / `#E1E6EB` | `#A8B5C2` / `#1B252E` | Pending verification, neutral information |

Foreground content on solid semantic colors follows Web: white in light mode; dark `#0E1116` for success/warning/info and error's `onError`. Container badges use the semantic foreground, not solid-role on-color. Status must also include a label and optionally dot/icon; color alone is insufficient.

Components consume `AqariColorScheme` semantic roles, not brand primitives or raw HEX. A Flutter `ColorScheme` may be generated as an interoperability layer, but `AqariColorScheme` remains authoritative because Flutter's stock role set does not express all AqariOS surface, status and text roles.

## 6. Typography

### Families

- Arabic: Tajawal. Web-loaded weights: 400, 500, 700.
- Latin: Roboto Flex, fallback Roboto. Weights: 400, 500, 700.
- Numeric/KPI: the Web uses platform monospace. **Mobile Design Decision Required:** choose and license/package the exact Flutter mono family before implementation; do not silently use the device default.

### Exact scale

| Aqari style | Size / line height | Weight | Letter spacing | Flutter mapping |
|---|---:|---:|---:|---|
| displayLarge | 57 / 64 | 400 | -0.25 | overridden `displayLarge` |
| displayMedium | 45 / 52 | 400 | 0 | overridden `displayMedium` |
| displaySmall | 36 / 44 | 400 | 0 | overridden `displaySmall` |
| headlineLarge | 32 / 40 | 400 | 0 | overridden `headlineLarge` |
| headlineMedium | 28 / 36 | 400 | 0 | overridden `headlineMedium` |
| headlineSmall | 24 / 32 | 400 | 0 | overridden `headlineSmall` |
| titleLarge | 22 / 28 | 400 | 0 | overridden `titleLarge` |
| titleMedium | 16 / 24 | 500 | 0.15 | overridden `titleMedium` |
| titleSmall | 14 / 20 | 500 | 0.10 | overridden `titleSmall` |
| bodyLarge | 16 / 24 | 400 | 0.50 | overridden `bodyLarge` |
| bodyMedium | 14 / 20 | 400 | 0.25 | overridden `bodyMedium` |
| bodySmall | 12 / 16 | 400 | 0.40 | overridden `bodySmall` |
| labelLarge | 14 / 20 | 500 | 0.10 | overridden `labelLarge` |
| labelMedium | 12 / 16 | 500 | 0.50 | overridden `labelMedium` |
| labelSmall | 11 / 16 | 500 | 0.50 | overridden `labelSmall` |

Arabic should use 500 for display/headline/title-large and 700—not unavailable 600—for labels/titles requiring stronger emphasis, pending visual validation. Large display styles are rare on operational mobile screens; their preservation does not imply routine use. KPI values normally use headline-small or title-large with numeric style and tabular figures where supported.

## 7. Spacing

Canonical `AqariSpacing`: 4, 8, 12, 16, 20, 24, 32, 40, 48, 64 logical pixels; zero is implicit.

| Context | Rule |
|---|---|
| Phone screen horizontal padding | 16; 20 on large phones when content remains comfortably readable |
| Tablet content gutter | 24; content max width decision per screen |
| Major section separation | 24 or 32 |
| Section title to content | 12 |
| List row internal padding | 16 horizontal, 12 vertical; minimum target height still applies |
| Form field gap | 16; label to field content handled inside field component |
| Form section gap | 24–32 |
| Meaningful card padding | 16 standard, 20 for prominent summary; never default to Web's 24 everywhere |
| Sheet/dialog horizontal padding | 20 phone, 24 tablet |
| Bottom navigation internal spacing | 8–12 within its safe-area container |

Any non-scale value requires a component-level rationale (e.g., optical icon alignment), not casual screen code.

## 8. Shapes

| Token | Value | Mobile use |
|---|---:|---|
| `xs` | 4 | Tiny indicators, compact internal geometry |
| `sm` | 8 | Inputs, menus, navigation items, dialogs, standard interactive containers |
| `md` | 16 | Meaningful cards, section surfaces, selected large controls |
| `lg` | 28 | Bottom-sheet top corners or rare prominent container |
| `full` | 9999 | Buttons, badges, avatars, circular icon buttons |

Cards do not automatically receive `md`; flat sections and rows have no outer radius. Bottom navigation shape is a platform/layout decision, not a floating pill by default.

## 9. Borders

- Standard width: 1 logical pixel; emphasized focus/selection: 2.
- Inputs use `outline`; cards/dialogs use `outlineVariant`; dividers use `borderMuted` or `outlineVariant` according to required strength.
- Selected list rows should prefer tonal background and a logical-start 2px primary rail only when persistent selection is meaningful.
- Avoid nesting bordered containers. A list group uses one boundary or internal dividers, not borders on every row.
- Flutter hairline widths are not the default because device-dependent rendering can weaken the Web identity.

## 10. Elevation

Preserve e0–e4 semantically. Flutter should use explicit `BoxShadow` recipes modeled on the Web shadows rather than arbitrary stock elevation.

| Token | Intended mobile level | Typical use |
|---|---|---|
| e0 | none | Screens, flat sections, most cards/list groups |
| e1 | subtle | Sticky app bar, interactive elevated card |
| e2 | low overlay | Popup menu, autocomplete/select menu |
| e3 | modal | Dialog, modal bottom sheet |
| e4 | highest reserved | Exceptional transient overlay only |

Web shadow tint is `rgb(51 61 41)` at roughly 5–8% per layer. Exact Flutter blur/spread/offset conversion requires rendered cross-platform comparison and is a **Mobile Design Decision Required**. Never substitute `elevation: 1..4` without visual matching.

## 11. Icons

- Use one Flutter icon family visually compatible with Lucide: outlined, rounded joins, consistent stroke. **Mobile Design Decision Required:** select the package and verify licensing, glyph coverage and RTL mirroring before implementation.
- Standard visible sizes: 16 inline, 18–20 navigation/actions, 24 state illustrations; touch wrapper minimum 48.
- Icon color follows adjacent text or semantic role. Unscoped dark icons must not inherit Web's broad `.lucide #B6AD90` hack; use explicit semantic roles.
- Mirror direction-bearing icons (back/forward, chevrons, undo/redo where directional). Do not mirror universal icons such as search, settings, home, calendar, check, or phone.
- Preserve custom AqariOS logo. Auth architectural artwork may preserve `#FFD98A` locally if it is deliberately ported.

## 12. Motion

| Token | Duration | Use |
|---|---:|---|
| fast | 75ms | Press feedback, color/opacity response |
| normal | 150ms | Small control state, menu, short expand/collapse |
| medium | 200ms | Dialog, sheet, navigation state; existing Web default |
| slow | 300ms | Larger page/content transition only when spatial continuity benefits |

Page transitions should be restrained and direction-aware; sheets slide from the physical bottom and need no RTL reversal. Expansion animates size and opacity. Success/error feedback uses a single entrance, no celebration by default. Respect reduced-motion/accessibility settings by removing nonessential movement.

## 13. RTL/LTR

- Locale drives `Directionality`: Arabic RTL, English LTR. One widget tree supports both.
- Use `EdgeInsetsDirectional`, `AlignmentDirectional`, `BorderDirectional`, `PositionedDirectional` and logical start/end naming.
- Arabic body/display uses Tajawal. Latin and mixed Latin fragments use Roboto Flex/Roboto without changing the screen direction.
- Email, phone, URLs, record IDs, account numbers, code and unlocalized numeric identifiers render LTR in an isolated directional wrapper. Keep their surrounding labels in screen direction.
- Currency and dates use locale-aware formatting; preserve number/currency as one semantic phrase for screen readers. Business preference for Western vs Arabic digits is a **Mobile Design Decision Required**.
- Back/forward and chevron icons mirror. Bottom-navigation item order follows locale direction through Flutter layout; destination meaning remains unchanged.
- Sheet/dialog actions use logical order. Destructive/primary action placement must be tested in both directions rather than hardcoded right/left.

## 14. Dark Mode

Dark mode uses the exact dark semantic table in Sections 4–5; it is not an inversion. Preserve distinct background, surface-low/container/high, text tiers, dark outlines, disabled values and semantic containers. Primary becomes deep olive `#414833`; brand secondary becomes `#8D745C`; status foregrounds brighten while containers darken.

Rules:

- Choose surfaces by hierarchy, not by applying opacity to white.
- Avoid pure black except the scrim; base is `#0E1116`.
- Borders are intentionally quiet (`#252B32`/`#1D2329`). Verify separation on OLED devices.
- Do not carry feature raw white/gray/emerald values into dark widgets.
- System/light/dark preference is persisted and supports OS-following, matching Web behavior.
- Shadow visibility must be validated; surface contrast and borders remain primary hierarchy tools.

## 15. Accessibility

- Minimum interactive target: 48×48 logical pixels; visible compact content may sit inside it.
- Minimum normal text should generally be 12px; 11px label-small is limited to noncritical metadata and must pass contrast/dynamic-text testing.
- Support text scaling without clipped labels, fixed-height multiline rows, or hidden actions. **Decision required:** set the tested scale range based on product/device support; do not disable scaling.
- Every icon-only action has a localized semantic label and state/value where applicable.
- Status always has text; dot/icon is supplementary. Errors appear beside the field and in an accessible summary when submission fails.
- Loading announces context without repeatedly interrupting. Skeletons are excluded from semantics.
- Focus indication remains visible for hardware keyboard/tablet usage; focus token uses primary with sufficient surrounding contrast.
- Preserve logical reading order in RTL and ensure isolated LTR fragments are announced intelligibly.

## 16. Mobile Navigation

The Web sidebar contains dashboard; buildings, apartments, parking; leases, tenants; payments, financial operations, utility bills; subscriptions; documents, notifications and settings. This is too broad for bottom navigation.

### Recommended information architecture

- **Bottom navigation candidates:** Dashboard, Properties, Leasing, Finance, More. “Properties” groups buildings/apartments/parking; “Leasing” groups leases/tenants; “Finance” groups payments/operations/bills.
- **More/drawer:** Documents, subscriptions, notifications, marketplace/maintenance when enabled, settings, language/theme, account/logout.
- **App bar:** title/context, back, notifications when relevant, search, and one overflow action.
- **Contextual destinations:** tabs or in-page section navigation inside a domain; avoid adding global bottom-nav items.
- **Global actions:** screen-specific primary action, preferably app-bar action or bottom safe-area action; no universal floating action button unless the product identifies one truly global creation task.

The five-destination proposal is a recommendation based on current Web grouping. Final destinations, role visibility and portal-specific shells are **Mobile Design Decisions Required** after route analytics and role/permission review.

## 17. Mobile Layout Rules

- Small phones: under 360 logical px; 16px gutter, single column, compact metadata, no side-by-side form fields.
- Normal phones: 360–431; 16px gutter, standard list/detail flow.
- Large phones: 432–599; 20px gutter; selective two-column key-value metrics only.
- Tablets: 600+; 24px gutter and adaptive master-detail or two-column layout where workflow benefits. Do not stretch text/forms to full width.
- Safe areas are mandatory. Persistent bottom actions sit above navigation/keyboard and do not cover content.
- Screen scroll is primary; avoid nested vertical scrolling. Horizontal scroll is reserved for intrinsically tabular comparisons/charts.
- Content priority, not viewport width alone, determines what moves to a detail screen.

Breakpoints are mobile recommendations because the Web's 640/768/1024 Tailwind breakpoints do not translate directly to Flutter device classes.

## 18. Component System

### Buttons

| Variant | Use / avoid | Mobile specification |
|---|---|---|
| Primary/filled | One main action; avoid multiple filled peers | 48px default height, full radius, 16–24 horizontal padding, label-large, 16–20 icon; primary/onPrimary |
| Tonal/secondary | Important secondary action | 48px, primaryContainer/onPrimaryContainer |
| Outlined | Secondary or cancel | 48px, 1px outline, transparent |
| Text/ghost | Tertiary and inline action | 48px target, transparent, primary text |
| Destructive | Confirm destructive action only | Error/onError; require explicit label |
| Link | Navigational text inside prose | Underline on interaction/accessibility need; not a substitute for primary button |
| Icon | Common recognizable action | 48px target, 20px icon; tooltip/semantic label |

Loading keeps size stable, disables repeated activation, and shows a 20px progress indicator with optional verb. Disabled uses disabledContainer/textDisabled and remains readable. Pressed state needs a darker/tinted semantic layer; exact light-mode active values are **Mobile Design Decision Required** because Web defines them inconsistently.

### Forms

Text, search, select, date/time, OTP, checkbox, radio and switch use semantic field components. Default field minimum height is 48px; multiline grows from 96px. Radius sm, outline border, 16px horizontal internal padding, body-large input text and label-medium/label-large supporting labels. Focus is a 2px primary border/ring; invalid is error with text. Select/date/time open a native-feeling modal sheet or picker while retaining Aqari styling around the trigger.

### Information and containers

- `AqariStatusBadge`: full pill, 12px label, 6px optional dot, semantic container/foreground, compact padding 8×4. Use in rows/detail headers, not as decoration.
- `AqariAvatar`: 32/40/48px; initials/image; full radius; semantic fallback.
- `AqariKpi`: label + value + optional trend. Flat in a section by default; card only if tappable or independently grouped.
- `AqariListRow`: canonical data-navigation primitive defined in Section 19.
- `AqariSectionHeader`: title-small/title-medium with optional text action; no decorative background.
- `AqariDivider`: 1px borderMuted/outlineVariant with directional inset matching text.
- `AqariCard`: only for a meaningful boundary; md radius, 1px outlineVariant or e1—not both unless the Web pattern requires it.
- `AqariSurface`/`AqariSection`: semantic layout primitives without assumed border/shadow.

### Feedback, navigation and overlays

- Loading: centered 24px indicator plus body-medium context for page loads; inline 16–20px for controls.
- Skeleton: shape follows final content; no shimmer that ignores reduced motion.
- Empty/error: 48px icon treatment, concise title/body, one next action; prefer flat placement unless the state belongs to a bounded panel.
- Success feedback: snackbar for transient confirmation; dedicated result screen only for consequential multi-step flows.
- Snackbar/toast: bottom-safe, semantic icon + text, optional action; never stack many.
- App bar: 56px content height plus safe area, flat or e1 when content scrolls beneath.
- Bottom navigation: at least 64px content plus safe area, max five top-level destinations; semantic selected state.
- Tabs: scrollable only when necessary; 48px target; selected indicator primary.
- Dialog: concise confirmation/decision; sm radius, e3, 20px padding, scrim around 40%.
- Bottom sheet: short contextual task/filter/select; lg top corners, e3, 20px padding, drag handle only if dismissible.
- Full-screen sheet/screen: long forms, multi-step tasks, complex details.
- Popup menu: short action list, e2, sm radius, 48px rows.

## 19. Lists

The standard list is the default mobile representation for tenants, properties, buildings, apartments, leases, payments, maintenance, documents, notifications and marketplace records.

### Row anatomy

- Minimum height 72px; compact operational row may be 64px, richer three-line row 88px+.
- Leading: optional 40px avatar/property thumbnail/semantic icon. Omit it when it adds no recognition value.
- Main title: title-small or title-medium, one line where possible.
- Supporting line: body-small/body-medium, most decision-relevant relationship or identifier.
- Metadata: one additional concise line or inline pair; move the rest to detail.
- Status: trailing badge when short; under title/support on narrow layouts or long Arabic labels.
- Trailing: chevron for navigation or one explicit action; overflow for multiple actions.
- Divider: logical inset aligned to title; no individual card around each row.
- Press: semantic surface tint; no hover dependency. Long press only for discoverable platform-conventional selection, never hidden critical actions.

Swipe actions are allowed only for frequent, reversible operations with an accessible non-swipe alternative. Destructive swipe requires confirmation or undo. Loading uses skeleton rows matching anatomy. Empty states explain what the list represents and offer one appropriate action.

## 20. Detail Screens

Canonical order: app bar → title/identity and status → primary facts → key metrics → sections → related records → actions.

- Identity/status remain visible near the top, not enclosed in a decorative card by default.
- Key-value facts use aligned rows and dividers. Two columns are limited to short metrics on large phones/tablets.
- Sections use 24–32px separation and section headers. Use a surface only when the section is interactive or materially distinct.
- Expandable sections suit optional verbose history/notes, not essential status or balance.
- Bottom sheets suit a quick contextual action or preview. Separate screens suit deep navigation, long related lists and editable records.
- Persistent bottom primary action is appropriate for a clear next step; secondary actions move to overflow. Read-only details need no forced action bar.

## 21. Forms

- Use a full screen for long/create/edit flows; sheet only for short, focused input.
- Group fields by task with section headers and 24–32px separation; no card around each section unless it is independently conditional/interactable.
- Labels remain visible; placeholders are examples, never the sole label.
- Field minimum 48px; 16px field gaps. Helper/error sits directly below with 4–8px spacing.
- Validate on blur/meaningful interaction and submit; do not show errors before the user can act. On failed submit, announce summary and scroll/focus the first invalid field.
- Keyboard action advances to next logical field; final action submits only when safe. Keep focused fields visible above keyboard.
- Phone/email/ID/URL are LTR-isolated with appropriate keyboards. Currency/number inputs separate localized display from canonical value and keep currency visible.
- Date/time use localized pickers; avoid free text where structured choice is safer.
- Arabic long text is RTL; mixed content uses explicit directional isolation. Text areas grow or provide clear scroll behavior.
- Required status is localized and accessible; do not communicate only with an asterisk.

## 22. Tables → Mobile

Transformation decision sequence:

1. Identify the row's recognition key, decision value, status and next action.
2. Put those in `AqariListRow`.
3. Move secondary columns to detail.
4. Use expansion only for a small, frequently checked subset.
5. Put filters/sort in a bottom sheet with active-filter count/chips.
6. Use horizontal table scrolling only when comparing the same values across columns is the task (e.g., financial reconciliation) and preserve a pinned identifier if feasible.

Payments prioritize tenant/property, amount, due date and payment status. Leases prioritize tenant/unit, term/end date and status. Maintenance prioritizes issue/property, urgency/status and age. Documents prioritize name/type, related entity and date/status. Bulk selection requires an explicit selection mode and top/bottom action bar.

## 23. Dashboard → Mobile

The dashboard should not stack every Web KPI card. Recommended hierarchy:

1. Contextual greeting/property portfolio scope and urgent exception count.
2. A compact KPI strip or 2×2 summary for occupancy, collected, outstanding and overdue—only metrics relevant to the signed-in role.
3. “Needs attention” prioritized list (overdue payments, expiring leases, maintenance issues).
4. Recent activity or upcoming dates as a flat list.
5. Quick actions limited to the most frequent 3–4; remainder in More.

Horizontal metric scrolling is acceptable when it preserves a compact first viewport and pagination/semantics are clear. Charts appear only when a trend changes a decision; otherwise a number plus comparison is clearer. Role-specific content and exact metric priority are **Mobile Design Decisions Required** based on real usage and permissions.

## 24. Status System

| Status | Semantic variant | Foreground/container | Treatment |
|---|---|---|---|
| Active | brand/default on Web; positive in some domains | primary/on primary-container or success pair depending existing domain mapping | Label + optional dot; domain mapping must remain explicit |
| Draft | neutral/secondary | onSecondaryContainer/secondaryContainer | Label, no success icon |
| Pending / Pending signature | neutral outline | textSecondary/transparent outline | Label + optional clock |
| Pending verification | info | info/infoContainer | Label + optional clock/info |
| Paid | success | success/successContainer | Label + check/dot |
| Partially paid | warning | warning/warningContainer | Label + partial/clock icon |
| Late | warning | warning/warningContainer | Label + clock |
| Overdue unpaid | error | error/errorContainer | Label + alert icon |
| Cancelled | neutral | textMuted/surfaceContainer | Label |
| Expired / Terminated | destructive | error/errorContainer | Label; meaning supplied by text |
| Renewed | secondary on Web | onSecondaryContainer/secondaryContainer | Label |
| Occupied | default/primary on Web | onPrimary/primary or tonal mobile badge after visual test | Label + dot |
| Vacant | secondary | onSecondaryContainer/secondaryContainer | Label + dot |
| Under maintenance | destructive on Web | error/errorContainer | Label + tool/alert optional |

Web inconsistency: “Active” is primary/default for lease and allocation contexts but success in other operational summaries. Flutter must use a domain-to-variant registry, not one global `Active → success` shortcut. Maintenance, utility, provider, subscription and tenant utility mappings must be exhaustively extracted before implementing their modules; this is a **Mobile Design Decision Required** rather than permission to invent colors.

## 25. Web → Flutter Mapping

| AqariOS Web | Flutter Mobile | Preserve | Change |
|---|---|---|---|
| CSS variables + typed tokens | `AqariColorScheme` + theme extensions | Exact semantic values/names | Single mobile token authority |
| Tailwind type utilities | `AqariTypography`/overridden TextTheme | Exact scale/families | Locale-aware font resolution |
| Sidebar | Bottom navigation + More/drawer | Grouping, active semantics | Destination prioritization and touch model |
| Topbar | Mobile app bar | Context, search, notification, account | One primary context and overflow |
| Breadcrumbs | Back navigation + concise hierarchy title | Navigation ancestry | Remove desktop breadcrumb trail |
| DataTable/Table | Prioritized list + detail/filter sheet | Data semantics/status | Column priority and interaction |
| Card | `AqariCard` or flat section | Surface/border/radius when meaningful | Cards become exceptional grouping |
| Button variants | Aqari button family | Colors, pill shape, typography/states | 48px touch height |
| Input/select/textarea | Aqari fields and picker triggers | Semantic states, 8px radius | 48px minimum, mobile keyboards/pickers |
| Checkbox/radio/switch | Aqari selection controls | Meaning/colors | 48px wrappers and platform input behavior |
| Tabs | Aqari tabs/segmented section nav | Selected semantics | Scroll/adapt to width |
| Dialog | Dialog or bottom sheet | Scrim, surface, hierarchy | Choose by task length/complexity |
| Side drawer | Bottom/full-screen sheet or detail screen | Content and actions | Bottom/stack navigation model |
| Dropdown/popover | Popup menu or selection sheet | Options/selection | Touch rows, no hover |
| Toast/Sonner | Snackbar/banner | Semantic feedback | Safe-area/mobile queue behavior |
| Loading/Skeleton | Mobile loading/skeleton | Visual language | Match final mobile layout |
| Empty/Error | Flat mobile state | Copy hierarchy/semantic colors | Avoid unnecessary containing card |
| StatusBadge | `AqariStatusBadge` | Domain semantics/colors/pill | Responsive placement and accessible icon/text |
| KPI/StatCard | Flat KPI or grouped metric | Numeric hierarchy/status | Fewer metrics, cards only when interactive |
| Filter bar | Filter bottom sheet + active summary | Filters/query meaning | Progressive disclosure |
| Hover | Press/focus/explicit action | State meaning | Touch feedback |
| Desktop multi-column form | Sectioned mobile form | Field semantics/order | Single column, keyboard and focus flow |
| ThemeProvider | Flutter theme controller | light/dark/system persistence | Flutter platform integration |
| I18nProvider | localization + Directionality | ar/en, RTL, formatting | Flutter locale architecture |

## 26. Flutter Design System Architecture

Architectural recommendation only:

```text
lib/
└── core/
    └── design_system/
        ├── theme/
        │   ├── aqari_brand.dart
        │   ├── aqari_color_scheme.dart
        │   ├── aqari_theme.dart
        │   ├── aqari_typography.dart
        │   ├── aqari_spacing.dart
        │   ├── aqari_radius.dart
        │   ├── aqari_elevation.dart
        │   └── aqari_motion.dart
        ├── components/
        │   ├── actions/
        │   ├── forms/
        │   ├── information/
        │   ├── lists/
        │   ├── navigation/
        │   ├── feedback/
        │   └── overlays/
        └── localization/
            ├── directional_content.dart
            └── locale_typography.dart
```

Use immutable tokens and `ThemeExtension`-style semantic access where stock Flutter theme types are insufficient. Components consume semantic roles only. Feature modules own domain status mapping but map into the finite shared visual variants. Avoid a generic “utils/constants” dumping ground and avoid one file per trivial wrapper.

## 27. Design System Playground Specification

The future playground is a debug/development route, never a production navigation destination. It must show:

- All brand and semantic colors in light/dark with token names and contrast pairings.
- Arabic and English typography at every style and supported scale factor.
- Buttons/fields/selection controls in default, pressed, focused, disabled, loading and error states.
- Flat sections versus justified card usage.
- List rows for each AqariOS domain, long Arabic labels, mixed IDs, status placement and loading/empty/error states.
- Status registry by domain.
- App bars, bottom navigation, tabs, popup menus, dialog, sheets and snackbar.
- Small-phone, normal-phone, large-phone and tablet frames.
- Runtime toggles for RTL/LTR, light/dark/system, text scale, reduced motion and high-contrast checks.
- Automated semantics labels and screenshot/golden-test targets once implementation begins.

## 28. Current Web Inconsistencies

1. Old design-system documentation conflicts with the live warm-neutral implementation.
2. CSS and TypeScript color definitions duplicate authority; CSS contains more roles.
3. Web `secondary` utility is a neutral surface while brand secondary is brown.
4. `app/components/ui` and `shared/ui` duplicate primitives.
5. Auth and features contain raw yellow, gray, emerald and white values outside semantic tokens.
6. Buttons, fields, radii, card padding and shadows vary at feature level.
7. e0–e4 coexists with Tailwind `shadow-xs` through `shadow-2xl`.
8. Business statuses are distributed across shared and feature-specific badge components.
9. RTL is strong in shared layout but older features use physical margins/alignment.
10. Arabic CSS requests weight 600 while the imported Tajawal set provides 400/500/700.
11. Dark button active states are explicit; equivalent light active-state values are not fully tokenized.

These are preserved as audit facts. Flutter should not reproduce accidental duplication, but any semantic consolidation needs a recorded decision and visual comparison.

## 29. Decisions Required Before Implementation

1. Confirm mobile app user roles/portals and final bottom-navigation destinations.
2. Confirm exact mobile route/module scope for the first release.
3. Approve which role-specific dashboard metrics and urgent items appear first.
4. Choose a Flutter icon package matching Lucide and verify RTL behavior.
5. Choose/package the numeric monospace family.
6. Resolve Tajawal 600 request: use 500 or 700 after visual validation.
7. Define exact pressed/active overlays for light buttons and list rows.
8. Validate e0–e4 Flutter shadow recipes on Android/iOS and light/dark.
9. Decide digit presentation and currency/date conventions for Arabic.
10. Complete domain status registry for maintenance, utilities, subscriptions, marketplace and verification flows.
11. Decide whether auth architectural artwork and feature-local `#FFD98A` are included in mobile.
12. Define supported text-scale range, target OS/device matrix and tablet max content widths.
13. Decide where platform-native pickers are visually wrapped versus fully custom-styled.
14. Confirm offline/network/permission-denied visual and recovery patterns; Web audit did not verify a complete unified set.
15. Validate every semantic text/background pair to WCAG targets before coding components.

## 30. Final Source-of-Truth Summary

The Flutter system must preserve:

- The exact semantic light/dark palette in this specification and the approved warm-neutral brand primitives.
- Tajawal Arabic and Roboto Flex/Roboto Latin typography with the exact AqariOS scale.
- The 4px spacing foundation, 4/8/16/28/full shape scale, 1px border language, e0–e4 restrained elevation and 75/150/200/300ms motion foundation.
- Domain-specific status meaning, first-class RTL, logical direction, and LTR isolation for technical content.
- AqariOS's calm, professional, property-management character.

The Flutter system must change:

- Desktop navigation into a role-aware, one-hand mobile shell.
- Tables into prioritized lists, detail screens and filter sheets.
- Hover and dense desktop actions into explicit touch interactions.
- Multi-column/card-heavy layouts into flat, sectioned information hierarchy.
- 40px desktop controls into accessible 48px mobile targets.

Unresolved behavior is marked **Mobile Design Decision Required** and must be decided before implementation, not filled with Material defaults. The intended result is unmistakably **AqariOS**—never a generic Flutter application wearing AqariOS colors.

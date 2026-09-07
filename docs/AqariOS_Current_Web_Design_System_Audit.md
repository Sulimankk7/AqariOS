# AqariOS Current Web Design System — Source-of-Truth Audit

**Scope:** inspected implementation only; no UI, CSS, component, theme, backend, database, or Flutter code was changed.  
**Audited source:** `frontend/src`, principally `styles/theme.css`, `styles/globals.css`, `shared/theme/*`, `shared/ui/*`, `shared/components/*`, layouts, and feature components.  
**Important:** this supersedes the factual claims in `docs/AqariOS_Design_System.md` where they disagree. That document still describes an old black/OKLCH/Inter system; the live source implements the warm-neutral system documented here.

## A. Current design system

### Architecture and token delivery

The web UI is React/Vite with Tailwind CSS v4, Radix primitives, CVA variants, Lucide icons, Sonner toasts, and Vaul drawers. It has:

- CSS custom-property roles in `src/styles/theme.css`, exposed to Tailwind through `@theme inline`.
- A parallel typed token representation in `src/shared/theme/colors.ts` and `tokens.ts`.
- Tailwind utilities and component-local classes. These are all used in the live application; raw colors also occur in feature code.
- A duplicate primitive folder at `src/app/components/ui`; the shared barrel exports `src/shared/ui`. The audited shared layer is the currently referenced system layer.

### Color system

#### Brand and semantic roles

| Role | Light | Dark | Meaning / implementation |
|---|---:|---:|---|
| Primary | `#656D4A` | `#414833` | Main action, focus, selected indicator, icon emphasis |
| On primary | `#FFFFFF` | `#F2F3F0` | Filled-button and avatar text |
| Primary container | `#A4AC86` | `#414833` | Tonal actions and selected navigation |
| On primary container | `#263020` | `#F2F3F0` | Text on primary container |
| Brand secondary | `#936639` | `#8D745C` | Brand role; Tailwind `secondary` intentionally maps to a neutral surface instead |
| Tertiary | `#7F4F24` | `#7E878F` | Brand tertiary / chart role |
| Success | `#526341`, bg `#E1E8D7` | `#9BAE91`, bg `#1C2820` | Positive status and status dot |
| Warning | `#7F4F24`, bg `#F2E3D2` | `#D0AA76`, bg `#2B241B` | Attention, late / partial statuses |
| Error/danger | `#8A3B2E`, bg `#F4DAD2` | `#E6A29A`, bg `#3A2424` | Destructive actions/errors |
| Info | `#59636F`, bg `#E1E6EB` | `#A8B5C2`, bg `#1B252E` | Informational/pending-verification status |

Source: `src/styles/theme.css`, `src/shared/theme/colors.ts`, and `src/shared/components/ui/StatusBadge.tsx`.

#### Surfaces, text, borders, and interaction roles

| Category | Light token → HEX | Dark token → HEX | Usage |
|---|---|---|---|
| Page / foreground | `background #F7F4EA`; `on-background #263020` | `#0E1116`; `#F2F3F0` | HTML/body/app shell |
| Surface | `surface #FFFEFA`; `surface-variant #E5DECD` | `#0E1116`; `#181D23` | General surface / variant |
| Surface ladder | lowest `#FFFEFA`, low `#F4F0E3`, container `#ECE7D8`, high `#E5DECD`, highest `#DDD4C0` | `#0B0E12`, `#14191F`, `#161B21`, `#181D23`, `#1D232A` | Cards, menus, table heads, hover layers |
| Card/popover/top bar | card `#FFFEFA`; popover `#FFFEFA`; topbar `#FFFEFA` | `#14191F`; `#181D23`; `#14191F` | Cards, overlays, sticky header |
| Sidebar | `#EFEADD`, border `#B9AD8F` | `#14191F`, border `#252B32` | Navigation background/boundary |
| Text | `on-surface #263020`; variant `#414833`; muted `#59614A`; disabled `#747968` | `#F2F3F0`; `#AEB3AD`; `#6F756F`; `#6F756F` | Default, supporting and disabled text |
| Border | outline `#897154`; variant `#C3B89B`; muted `#D2C8AE` | outline/variant `#252B32`; muted `#1D2329` | Inputs, cards, dividers; focus is primary |
| Scrim | `#000000` (normally alpha applied) | same | Dialog and mobile-nav overlay |

Every role above is a CSS variable and Tailwind color utility. The `colors.ts` light/dark objects contain the same core Material-style semantic palette; it does **not** contain every compatibility/status/button override defined in CSS.

#### Approved warm-neutral palette usage

| Approved value | Current token / use |
|---|---|
| `#582F0E` | `brand-brown-900`, `on-error-container`; defined and used in token sources |
| `#7F4F24` | brown-700, tertiary, light warning |
| `#936639` | brown-600, light brand secondary |
| `#A68A64` | brown-400 only |
| `#B6AD90` | brown-200, light secondary container |
| `#C2C5AA` | green-200, tertiary container |
| `#A4AC86` | green-400, primary container |
| `#656D4A` | green-600, light primary |
| `#414833` | green-800, on-surface variant; dark primary |

All approved values are defined. `#A68A64` has no semantic application beyond its brand token. The implementation also uses numerous non-approved surface, accessibility, semantic, and dark-mode values listed above, plus feature-local grays (`#111827`, `#374151`, `#6B7280`, `#9CA3AF`, `#E5E7EB`), `#CCC`, `#FAFAF7`, `#F4F1EA`, and raw emerald utility colors.

#### Raw-color audit

Raw HEX is present in theme/token sources **and** feature components. Regex frequency across `src` (definition and usage occurrences, not rendered instances) has the most frequent values: `#FFD98A` 29, `#414833` 23, `#A4AC86` 21, `#FFFFFF` 19, `#F2F3F0` 13, `#656D4A`/`#333D29` 12 each, `#0E1116`/`#C2C5AA` 11 each, `#7F4F24` 9, and `#936639`/`#111827` 8 each. `#FFD98A` is therefore a material feature-level accent outside the approved palette, not a foundation token. Exact declarations are searchable with `rg -n '#[0-9A-Fa-f]+' frontend/src`.

### Typography

Fonts are Google-loaded: Arabic `Tajawal` (400/500/700); English `Roboto Flex`, falling back to `Roboto` (400/500/700) then Arial. `html[lang]` selects the family. Arabic raises display/headline/title-large weight to 500 and title/label to 600; 600 is requested by CSS but is not included in the Tajawal import.

| Style | px / line-height | weight | letter spacing |
|---|---:|---:|---:|
| Display L/M/S | 57/64, 45/52, 36/44 | 400 | L: -0.25px; otherwise unspecified |
| Headline L/M/S | 32/40, 28/36, 24/32 | 400 | unspecified |
| Title L/M/S | 22/28, 16/24, 14/20 | 400/500/500 | M .15px, S .10px |
| Body L/M/S | 16/24, 14/20, 12/16 | 400 | .5px/.25px/.4px |
| Label L/M/S | 14/20, 12/16, 11/16 | 500 | .10px/.5px/.5px |

`type-*` classes in `styles/globals.css` are the actual named scale. Feature screens also use raw Tailwind sizes/weights (`text-xs` through `text-2xl`, semibold/bold, mono), so these named styles are not exclusive. Numeric KPI values use `font-mono` in `StatCard`.

### Spacing, shapes, borders, elevation and motion

- Token scale: 0, 4, 8, 12, 16, 20, 24, 32, 40, 48, 64px (`tokens.ts`). Actual feature use also includes 2, 6, 10, 14, 28, etc. through Tailwind utilities.
- App content: `p-4` mobile, `p-6` at `sm`, `p-8` at `lg`; `max-w-7xl`; vertical `space-y-4` (`AppShell.tsx`). Cards use `gap-6`, with 24px header/content/footer in the primitive; feature cards frequently use 12/16/20px.
- Shapes: xs 4px, sm 8px, md 16px, lg 28px, full 9999px. Buttons/badges are full; inputs/selects/sidebar/menu use sm (8px); standard cards use md (16px); dialogs use `rounded-lg` (Tailwind 8px). Feature code also uses `rounded-lg`, `rounded-xl` (12px), `rounded-2xl` (16px), and `rounded-md` (6px).
- Borders are normally 1px. Primitive card/dialog/select use `outline-variant`; inputs use `outline`; global base applies `border-border` to all elements. Focus: 2px primary/ring, usually with 2px offset; invalid inputs use destructive ring.
- Elevation tokens: e0 none; e1 `0 1px 2px rgb(51 61 41 / .08), 0 1px 3px 1px rgb(51 61 41 / .05)`; e2 `0 1px 2px …, 0 2px 6px 2px …`; e3 `0 1px 3px …, 0 4px 8px 3px …`; e4 `0 2px 3px …, 0 6px 10px 4px …`. Cards use e0/e1; select uses e2; dialog/menu use e3; sidebars/topbar use e1. Some feature drawers use Tailwind `shadow-2xl`, outside that hierarchy.
- Motion tokens: 75/150/200/300ms. Primitives predominantly use 150–200ms fades, zooms and directional slides. Reduced-motion media query collapses animations/transitions to .01ms.

### Icons

Primary icon library is `lucide-react`; standard sizes are 14–20px (often 16px), 18px navigation, 24px empty/error/loading, and 12px badge icons. CSS uses `currentColor`; a dark override initially colors unscoped Lucide icons `#B6AD90`, with button-specific current-color overrides. Custom `AqariOSLogo`, `GoogleLogo`, and architectural illustration components exist. MUI icons is installed but could not be verified as used. Directional Lucide chevrons/arrows commonly apply `rtl:rotate-180`; this is manual per component, not automatic icon mirroring.

### Components and actual rules

- **Button:** variants filled/default, tonal/secondary, elevated, outlined/outline, text/ghost, link, destructive. Heights: 32/40/48px; icon 32/40px. Default padding 24px, sm 16px, lg 32px. Full pill, label-large text, 16px default icon. Hover mostly 8% primary tint or primary at 92%; only dark mode explicitly defines active and disabled role colors. Loading disables and prepends spinning `LoaderCircle`.
- **Inputs/forms:** Input default 40px, `px-3`, 8px radius, body-medium; Select 40px or 32px; Textarea min 64px (`p-3`/`py-2`, 16px at narrow size and 14px at md+). Placeholder muted; focus/invalid/disabled are implemented. Checkbox, radio, switch, slider, input OTP, calendar/date picker, search field and form wrappers exist. A global standard for helper/validation layout is not found; forms implement labels/messages locally.
- **Cards:** `elevated` (card/e1), `filled` (surface-low/e0), `outlined` default (card/e0); all 16px radius, 1px border. Primitive header/content/footer use 24px padding. Many feature cards substitute 8/12px radii and 12–20px padding.
- **Data:** table header uses surface-container, 40px label-medium heads, 12px horizontal / 10px vertical cells, row hover surface-low, selected primary-container at 35%. Shared `StatusBadge` is a full pill with 6px dot and 10px/12px text. KPIs use `StatCard`/`KpiCard`; progress, avatars, pagination, chips/badges and lists are present.
- **Feedback:** `LoadingState` is 160px-min centered 24px spinner; `Skeleton` is available; `EmptyState` is 192px-min centered with 48px circular icon; `ErrorState` is an error-container rounded 16px-equivalent card with retry. Dialog/sheet/drawer/alert/tooltip/popover/dropdown/context/hover cards and Sonner toast are available. Dialog is center-screen, viewport minus 32px wide, 8px radius, 16px mobile/24px desktop padding, e3, black 40% overlay, and fade/zoom. Sheet/drawer is 75% width and max `sm` (or feature-specific 480–540px), with slide transition.

### Navigation and responsive behavior

Desktop navigation is a sticky, full-height sidebar at `lg` (Tailwind default 1024px): 288px expanded / 72px collapsed, 64px header, grouped/expandable items with 44px minimum height, 18px icons, 8px radius. Active items use primary-container, e1, and a 2px logical-start primary rail. Topbar is sticky 64px; desktop search appears at `md` (768px); language/theme controls move into user menu under `sm` (640px). Mobile menu is a 55%-scrim overlay with a full-width sidebar constrained to viewport minus 40px.

Tailwind defaults supply breakpoints: sm 640, md 768, lg 1024, xl 1280, 2xl 1536px. Layouts are generally single column then 2+ columns; dashboard cards use 1→2→3/4/6 depending on component. Tables either horizontally scroll or are replaced by cards at `md` in several newer features. No custom breakpoint configuration was found. Auth visual panel is hidden below `lg`.

### RTL and dark mode

`I18nProvider` persists language and sets root `lang` plus `dir` (`ar` → rtl, `en` → ltr). Logical Tailwind utilities (`start/end`, `ps/pe`, `border-s/e`), `rtl:` rotations and text alignment are broadly used. LTR isolates apply to email/tel/url, IDs, code, currency/account values. Some older feature components use physical `ml/mr` with manual `rtl:` correction; tables sometimes use `text-left rtl:text-right`.

Dark mode persists `light`/`dark`/`system` in local storage, honors OS preference for system, and toggles `.dark` on `<html>`. It remaps semantic variables to the dark palette listed above and adds a large, CSS-selector-based button-state override layer. It is a semantic-token system at foundation level, but feature-local raw colors and explicit `dark:` utilities mean it is not token-only.

## B. Business status color mappings

These are mappings actually found; a status receives the color of the cited shared badge variant.

| Domain status | Variant / color family | Source |
|---|---|---|
| Apartment/building: Occupied, Vacant, Under maintenance | default primary, secondary-container, destructive | `ApartmentsList.tsx`, `ApartmentDetails.tsx`, `BuildingDetails.tsx` |
| Lease: Active, Draft/Renewed, Pending signature, Expired/Terminated | default, secondary, outline, destructive | `leasing/constants/leasingEnums.ts` |
| Rent payment: Paid, Partially paid/Late, Overdue unpaid, Pending verification, Cancelled/Pending | success, warning, danger, info, neutral | `financials/components/RentCollectionTable.tsx` |
| Payment purpose: obligation, received payment, adjustment | warning, success, info | `RentCollectionTable.tsx` and payment drawer |
| Allocation: Active/Reversed | success/neutral | `FinancialPaymentDetailsDrawer.tsx` |
| System online | success dot | `layout/Sidebar.tsx` |

Additional status components exist for maintenance, subscriptions, utility/provider/payment flow and tenant utility flow; their exact mappings are feature-owned rather than consolidated into `StatusBadge`. No universal status registry was found.

## C. Component inventory

**Foundation:** `ThemeProvider`, `colors`, `tokens`, `bidi`, typography utilities, `AqariOSLogo`, `ImageWithFallback`.  
**Actions/controls:** `Button`, `Toggle`, `ToggleGroup`, `Pagination`, `Command`, `Tooltip`.  
**Forms:** `Form`, `Input`, `SearchField`, `InputOTP`, `Textarea`, `Select`, `Checkbox`, `RadioGroup`, `Switch`, `Slider`, `Calendar`, `DatePicker`, `Label`.  
**Navigation/layout:** `AppShell`, `PageContainer`, `NavigationSidebar`, `Sidebar`, `Topbar`, `Breadcrumbs`, `BreadcrumbContext`, `CommandMenu`, `Tabs`, `NavigationMenu`, `Menubar`, `ScrollArea`, `Resizable`, `Carousel`.  
**Data/display:** `Card`, `Table`, `Badge`, `StatusBadge`, `StatCard`, `KpiCard`, `DataTable`, `Avatar`, `Progress`, `Chart`, `Separator`, `AspectRatio`, `MapPicker`.  
**Overlays/feedback:** `Dialog`, `AlertDialog`, `Sheet`, `Drawer`, `Popover`, `DropdownMenu`, `ContextMenu`, `HoverCard`, `Toast`/`Sonner`, `Alert`, `LoadingState`, `Skeleton`, `EmptyState`, `ErrorState`, `ArchiveBlockedDialog`, `Overlays`.

## D. Current design language

AqariOS is a warm-neutral, property-operations interface: muted olive primary and earth-brown brand roles on cream layered surfaces. It is moderately dense and enterprise-oriented, with strong borders, restrained green-tinted elevation, full-pill actions/statuses, small rounded input/menu corners, and larger card corners. Navigation is a grouped sidebar plus compact sticky top bar. Arabic is a first-class language mode with Tajawal and RTL-aware layout, though older local code remains partly physical-direction based.

## E. Current design-system inconsistencies

1. The existing `docs/AqariOS_Design_System.md` is stale: it specifies unrelated black/OKLCH/Inter values and incorrect locations/radii.
2. TypeScript core color objects and CSS roles are duplicated; CSS adds compatibility, status and dark button values absent from `colors.ts`.
3. Tailwind `secondary` is deliberately a neutral surface while `brand-secondary` is brown; this is semantically surprising and easy to misuse.
4. The `app/components/ui` and `shared/ui` primitive trees duplicate many components.
5. Feature code introduces raw gray, yellow (`#FFD98A`), emerald, white and ad hoc colors, bypassing semantic roles; these do not consistently receive dark-mode equivalents.
6. Button geometry is centralized (32/40/48px) but numerous feature buttons use `h-9`, `min-h-10`, `min-h-11`, or bespoke classes. Only dark mode explicitly supplies active colors for standard button roles.
7. Foundation radius is 4/8/16/28/full, while live feature UI regularly uses 6/8/12/16px. Card padding also varies from 12 to 24px.
8. Token e0–e4 exists, but `shadow-xs`, `shadow-sm`, `shadow-xl`, and `shadow-2xl` are also used, weakening a single elevation hierarchy.
9. Shared `StatusBadge` exists alongside feature-specific badge mapping/components; there is no single business-status token registry.
10. RTL is robust in current shared layout but is not uniform in feature code: manual `mr/ml`, `text-left`, and component-specific icon rotation remain.
11. The font declaration requests Arabic 600 behavior but the external font import only requests 400/500/700.

## F. Web → mobile transformation foundation

| Web component/pattern | Future mobile equivalent | Preparation note |
|---|---|---|
| Desktop sidebar | App navigation / drawer or bottom navigation | Information hierarchy and interaction model need mobile design |
| Sticky topbar + menus | Compact app bar and overflow actions | Preserve roles, not desktop density |
| Data table | Prioritized data list/cards | Existing `md` table-to-card patterns are evidence, not a universal rule |
| Dialog | Dialog or bottom sheet | Current dialogs already use mobile padding; interaction choice remains open |
| Side drawer | Full-screen detail or bottom sheet | Preserve detail content/status patterns |
| 40px form fields/buttons | Touch-optimized form controls | Preserve semantic states; mobile dimensions require explicit next-phase decisions |
| KPI grid/cards | Scrollable/stacked metric cards | Preserve color/status roles and mono metrics |
| StatusBadge | Flutter status chip | Carry exact mapped colors and status semantics |
| Theme/i18n provider | Flutter theme + locale/direction state | Preserve light/dark role pairs, Tajawal/Roboto Flex intent, LTR isolates |

# AQARIOS CURRENT DESIGN SYSTEM — SOURCE OF TRUTH

For the Flutter phase, preserve the implemented semantic role names and exact light/dark values in `styles/theme.css`/`shared/theme/colors.ts`; the type scale in `styles/globals.css`; the 4px base spacing scale from `tokens.ts`; shapes xs/sm/md/lg/full; e0–e4 elevation; Tajawal for Arabic and Roboto Flex/Roboto for Latin; the shared primitive state behavior; and `I18nProvider`’s RTL/LTR rules. Treat raw feature values, duplicate primitive folders, and the inconsistencies above as **current implementation facts requiring an explicit later decision**, not as tokens to silently normalize. Values or behaviors not stated above were not found or could not be verified from the codebase.

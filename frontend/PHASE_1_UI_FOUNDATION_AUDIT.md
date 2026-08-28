# AqariOS frontend Phase 1 audit

## Scope and safeguards

This phase changes the reusable visual foundation only. No route definitions, API clients, DTOs, permissions, backend files, database behavior, or feature-page workflows were changed as part of the design-foundation work.

The repository did not contain a file named `aqarios-design-system.html` at audit time. The exact semantic colors and component requirements supplied with the Phase 1 brief were used. The older `docs/AqariOS_Design_System.md` describes the previous neutral/Inter system and is not treated as the new source of truth.

## Current frontend structure

```text
frontend/src
├── app
│   ├── layouts
│   │   ├── AppLayout.tsx                 owner/manager shell
│   │   ├── TenantLayout.tsx              tenant shell
│   │   ├── PlatformAdminLayout.tsx       platform shell
│   │   └── AuthLayout.tsx                public authentication shell
│   ├── providers/AppProvider.tsx
│   ├── router/{AppRouter,ProtectedRoute}.tsx
│   └── components/ui/*                   compatibility primitive imports
├── shared
│   ├── components
│   │   ├── layout
│   │   │   ├── AppShell.tsx              shared workspace geometry
│   │   │   ├── Sidebar.tsx               owner/manager navigation
│   │   │   ├── Topbar.tsx                owner/tenant topbar
│   │   │   ├── PageContainer.tsx
│   │   │   ├── Breadcrumbs.tsx
│   │   │   └── CommandMenu.tsx
│   │   └── ui
│   │       ├── DataTable.tsx
│   │       ├── Feedback.tsx
│   │       ├── Filters.tsx
│   │       ├── Headers.tsx
│   │       ├── NavigationUI.tsx
│   │       ├── Overlays.tsx
│   │       ├── StatusBadge.tsx
│   │       └── supporting domain-neutral wrappers
│   ├── ui/*                              canonical Radix/shadcn-style primitives
│   ├── theme/{colors,tokens,theme,ThemeProvider}.ts(x)
│   └── i18n/*                            Arabic/English, root dir/lang management
├── styles
│   ├── theme.css                         semantic CSS tokens
│   ├── fonts.css                         Tajawal + Roboto Flex/Roboto
│   ├── globals.css                       typography, bidi, base behavior
│   ├── tailwind.css                      Tailwind v4 sources
│   └── index.css                         style entry point
└── features
    ├── apartments          ├── auth              ├── buildings
    ├── dashboard           ├── documents         ├── financials
    ├── floors              ├── leasing           ├── maintenance
    ├── marketplace         ├── notifications     ├── payments
    ├── platformAdmin       ├── properties        ├── subscriptions
    ├── tenantPortal        ├── tenants           └── utilityBills
```

Audit size: 254 TSX files, 34 feature pages, and 85 feature component files. The feature layer still contains 81 raw buttons, 41 raw inputs, 16 raw selects, 10 raw tables, and 147 hard-coded color occurrences. These are Phase 2 migration targets; changing them now would amount to redesigning feature pages.

## Component inventory and decisions

| Area | Existing implementation | Phase 1 decision |
|---|---|---|
| Button | duplicate primitives in `app/components/ui` and `shared/ui`; raw feature buttons | Reuse canonical shared primitive; add filled, tonal, outlined, elevated, text, destructive, disabled, and loading states; preserve legacy variant names |
| Input / Select | Radix/select and native input duplicates | Reuse and restyle canonical primitives with default, focus, error (`aria-invalid`), and disabled states |
| Search | hand-built `SearchBar` in `Filters.tsx` | Add canonical `SearchField`; keep `SearchBar` API as an adapter |
| Checkbox / Radio / Switch | Radix primitives | Reuse and standardize shape, state colors, focus, disabled, and RTL thumb movement |
| Card | duplicate primitive and many feature cards | Reuse; add elevated, filled, and outlined variants; do not convert arbitrary page sections into cards |
| Badge / Chip | primitive `Badge` plus semantic `StatusBadge` | Reuse both; standardize primitive shapes and semantic containers; retain operational status meanings |
| Tabs | Radix primitive and higher-level navigation tabs | Reuse primitive; standardize selected/focus/disabled states |
| Dropdown | Radix primitive plus bespoke topbar menus | Reuse and restyle primitive; topbar menus remain behavior-compatible for later migration |
| Dialog | Radix dialog/alert-dialog plus bespoke `Overlays.Modal` | Canonical primitive restyled; bespoke overlay kept for compatibility and later replacement |
| Drawer | Radix Sheet, Vaul Drawer, and bespoke record drawers | Keep primitives; shared styling standardized; record-specific drawers remain feature-owned |
| Table | primitive Table plus `DataTable` and feature-specific tables | Reuse; canonical table made compact; feature-specific migration deferred |
| Tooltip | Radix primitive plus manual sidebar tooltip | Reuse canonical tooltip; manual sidebar tooltip visually aligned without changing navigation behavior |
| Toast | Sonner existed but was not mounted and used the wrong theme provider | Reuse Sonner, connect to AqariOS ThemeProvider, and mount one global toaster |
| Loading | Skeleton plus page-specific loading blocks | Reuse Skeleton and add canonical `LoadingState` |
| Error | `Feedback.ErrorState` plus page-specific errors | Reuse wrapper, align its visuals, and add canonical `ErrorState` |
| Empty | `Feedback.EmptyState` plus page-specific empty blocks | Reuse wrapper, remove decorative card treatment, and add canonical `EmptyState` |
| App shell | three duplicated workspace geometries | Add shared `AppShell`; retain role-specific navigation/topbar components and all route outlets |

## Foundation tokens

- Semantic roles: primary, primary container, secondary, secondary container, tertiary, tertiary container, background, surface, surface variant, on-colors, outline, outline variant, error, error container.
- Surface hierarchy: dim, bright, container-lowest, container-low, container, container-high, container-highest.
- Typography: Tajawal for Arabic; Roboto Flex with Roboto fallback for English. Material type roles are exposed as `type-display-*`, `type-headline-*`, `type-title-*`, `type-body-*`, and `type-label-*`.
- Shape: 4px, 8px, 16px, 28px, and full.
- Elevation: E0 through E4 using quiet olive-tinted shadows.
- Direction: logical CSS properties are used for new foundation work. `BidiText` plus `data-bidi` utilities isolate emails, phone numbers, identifiers, codes, dates, and numeric values inside RTL content.

## Architecture conflicts and Phase 2 prerequisites

1. `src/app/components/ui` and `src/shared/ui` were duplicate primitive libraries. The former now re-exports the latter for in-scope primitives, so existing imports keep working while new code has one canonical implementation.
2. Feature pages mix canonical components, high-level wrappers, raw HTML controls, and hard-coded Tailwind colors. Migrate feature-by-feature in Phase 2; avoid a repository-wide blind class replacement.
3. Authentication has a separate “architectural glass/skyscraper” visual system with many hard-coded colors. It was intentionally not redesigned in Phase 1.
4. Owner, tenant, and platform-admin navigation data and behavior are separate. Their geometry is shared, but merging role/permission navigation models requires a behavior review and is not a visual-foundation task.
5. The existing product exposes a dark/system theme switcher. Dark mode was preserved for functionality, although the supplied Phase 1 palette specifies the light semantic roles. Confirm dark reference tones before treating the dark palette as final.
6. Supply or place `aqarios-design-system.html` in the repository before Phase 2 visual QA so surface-hierarchy tonal values, motion, and any reference-only details can be compared directly.
7. The production bundle is currently about 1.6 MB before gzip and Vite reports a chunk-size warning. Route-level code splitting is a separate performance task and does not block the foundation.

## Light/dark contrast refinement

The second semantic pass separates the warm page background, bright card surface, navigation surface, and elevated containers instead of relying on shadows. Legacy `bg-secondary` usage now resolves to a neutral surface while the earthy secondary brand role remains available separately.

Measured representative text contrast ratios:

- Light primary text on page background: 12.50:1
- Light secondary text on card: 9.46:1
- Light muted text on card: 6.44:1
- Light primary button text: 5.47:1
- Dark primary text on surface: 16.67:1
- Dark secondary text on card: 10.37:1
- Dark muted text on card: 8.14:1
- Dark primary button text: 6.67:1

The authentication shell was also brought onto the semantic surface system. It now uses Tajawal for Arabic instead of forcing Inter, with stronger labels, field borders, placeholders, supporting copy, topbar/footer separation, and semantic light/dark surfaces.

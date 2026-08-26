# AqariOS Design System

AqariOS uses a custom design system built on **Tailwind CSS v4**, **Radix UI primitives**, and a **shadcn/ui-style component layer**. It also includes a typed token system for colors, typography, spacing, radius, elevation, and status states.

## Foundation

- Styling engine: Tailwind CSS v4
- Component primitives: Radix UI
- Utility pattern: `cn()` with `clsx` + `tailwind-merge`
- Theme model: CSS variables plus TypeScript tokens

## Color Palette

The project uses a neutral-first palette with strong semantic tokens.

### Light Theme

- Background: `#ffffff`
- Foreground: `oklch(0.145 0 0)`
- Card: `#ffffff`
- Card foreground: `oklch(0.145 0 0)`
- Popover: `oklch(1 0 0)`
- Popover foreground: `oklch(0.145 0 0)`
- Primary: `#030213`
- Primary foreground: `oklch(1 0 0)`
- Secondary: `oklch(0.95 0.0058 264.53)`
- Secondary foreground: `#030213`
- Muted: `#ececf0`
- Muted foreground: `#717182`
- Accent: `#e9ebef`
- Accent foreground: `#030213`
- Destructive: `#d4183d`
- Destructive foreground: `#ffffff`
- Border: `rgba(0, 0, 0, 0.1)`
- Input background: `#f3f3f5`
- Ring: `oklch(0.708 0 0)`

### Dark Theme

Dark mode swaps the palette to near-black surfaces with light foregrounds:

- Background: `oklch(0.145 0 0)`
- Foreground: `oklch(0.985 0 0)`
- Card: `oklch(0.145 0 0)`
- Primary: `oklch(0.985 0 0)`
- Secondary: `oklch(0.269 0 0)`
- Muted: `oklch(0.269 0 0)`
- Accent: `oklch(0.269 0 0)`
- Border: `oklch(0.269 0 0)`
- Ring: `oklch(0.439 0 0)`

### Semantic and Chart Colors

The token layer also defines:

- Success
- Warning
- Danger
- Info
- Chart palette `chart-1` through `chart-5`

## Typography

### Font Families

- Sans: `Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`
- Mono: `JetBrains Mono, ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace`

### Type Scale

- `xs`: `0.75rem` / `1rem`
- `sm`: `0.875rem` / `1.25rem`
- `base`: `1rem` / `1.5rem`
- `lg`: `1.125rem` / `1.75rem`
- `xl`: `1.25rem` / `1.75rem`
- `2xl`: `1.5rem` / `2rem`
- `3xl`: `1.875rem` / `2.25rem`
- `4xl`: `2.25rem` / `2.5rem`

### Font Weights

- Normal: `400`
- Medium: `500`
- Semibold: `600`
- Bold: `700`

## Spacing

The spacing scale is simple and consistent:

- `0`: `0px`
- `1`: `0.25rem`
- `2`: `0.5rem`
- `3`: `0.75rem`
- `4`: `1rem`
- `5`: `1.25rem`
- `6`: `1.5rem`
- `8`: `2rem`
- `10`: `2.5rem`
- `12`: `3rem`
- `16`: `4rem`

## Radius

Radius tokens:

- `none`: `0px`
- `xs`: `0.125rem`
- `sm`: `0.25rem`
- `md`: `0.375rem`
- `lg`: `0.5rem`
- `xl`: `0.75rem`
- `full`: `9999px`

The CSS theme also defines a default radius of `0.625rem`.

## Elevation

Shadow tokens are defined from subtle to strong:

- `none`
- `xs`
- `sm`
- `md`
- `lg`
- `xl`
- `2xl`

## Motion

Animation tokens:

- Fast: `75ms`
- Normal: `150ms`
- Medium: `200ms`
- Slow: `300ms`

Easing curves are standard cubic-bezier values for:

- `easeInOut`
- `easeOut`
- `easeIn`

## Focus States

The system uses a consistent focus ring approach:

- Ring color token: `var(--ring)`
- Ring width: `2px`
- Offset: `2px`
- Base class pattern: `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring`

## Component Language

The shared UI components follow a shadcn-style pattern and are designed to be composable and token-driven.

Common component variants include:

- Buttons: `default`, `destructive`, `outline`, `secondary`, `ghost`, `link`
- Cards: `header`, `title`, `description`, `content`, `footer`, `action`

## Implementation Notes

- Theme values are exposed through CSS variables in `src/styles/globals.css`
- TypeScript tokens live in `src/shared/theme/tokens.ts`
- Reusable UI components live in `src/app/components/ui`

## Short Summary

AqariOS is using a **custom Tailwind + Radix + shadcn-style design system** with a token layer for color, typography, spacing, radius, elevation, motion, and semantic states.


# AqariOS mobile production implementation matrix

This matrix is the delivery contract for the Flutter client. It was derived from the Web routes/API modules and the backend controllers; it does not treat Web placeholders as completed functionality.

| Feature | Web route | Backend endpoint(s) | Mobile route / screen | Required API models | Permissions / roles | Status | Dependencies |
|---|---|---|---|---|---|---|---|
| Authentication | `/auth/login` plus registration, OTP, and reset flows | `/api/v1/auth/login`, `/me`, `/refresh`, `/logout`; registration/OTP/reset endpoints | Auth gate / Login | `LoginRequestDto`, `LoginResponseDto`, `UserProfileDto`, refresh/logout requests | Public, then resolved role | Phase 1 implemented: login, refresh, logout, `/me` | Secure storage, API client |
| Company dashboard | `/dashboard` | `GET /api/v1/dashboard/summary` | `app/company/dashboard` / Overview tab | `DashboardSummaryDto` and nested property, leasing, payment, financial summaries | Authenticated company context | Phase 1 implemented | Authenticated shell, active company |
| Buildings and units | `/buildings*`, `/apartments*`, parking | Buildings, floors, apartments, parking controllers | `app/company/properties` / Properties tab | List/detail/upsert DTOs, paged query envelopes, location DTOs | `properties.*` | Planned phase 2 | Pagination/search primitives, company context |
| Leasing and tenants | `/leases*`, `/tenants*` | Lease contracts and tenant controllers, lifecycle and documents | `app/company/leasing` / Leasing tab | Lease, tenant, lifecycle, document, paged-query DTOs | Company admin; contract actions use `contracts.*` | Planned phase 3 | Properties/units, upload flows |
| Payments and finance | `/payments`, `/financial-operations` | Rent payments, cheques, expenses, receipts, reporting | `app/company/finance` / Finance tab | Payment, cheque, expense, receipt and report DTOs | `payments.*`, `cheques.read`, `expenses.*`, `receipts.*`, `reports.export` | Planned phase 4 | Leasing, pagination, download/export handling |
| Utility bills | `/utility-bills` | Management utility-bill endpoints | `app/company/utilities` / Utilities | Utility-bill list/detail/action DTOs | Company context | Planned | Properties/units |
| Maintenance | `/maintenance` | Versioned maintenance requests, comments, attachments, history | `app/maintenance` / Maintenance | Request, comment, attachment, history and status DTOs | `maintenance.*` | Planned; reconcile the Web `/api/v1.0/maintenance-requests` spelling before implementation | Upload handling, properties |
| Subscriptions | `/subscriptions` | Plans, own subscription, change requests | `app/more/subscription` / Subscription | Plan, subscription and change-request DTOs | `subscriptions.*` | Planned | Authenticated shell |
| Notifications | `/notifications` | Notification inbox and management endpoints | `app/notifications` / Inbox | Notification, paging and template DTOs | Authenticated; management uses `notifications.*` | Planned; Web route is currently partly placeholder | Pagination, deep-link policy |
| Marketplace | `/marketplace` | Marketplace endpoints | `app/more/marketplace` / Marketplace | Listing, publishing and paging DTOs | `marketplace.publish` for publishing | Planned; Web route is currently placeholder | Pagination, media handling |
| Documents | `/documents` | Document/category endpoints | `app/more/documents` / Documents | Document, category, upload and paging DTOs | `documents.*` | Planned; Web route is currently placeholder | Upload/download handling |
| Tenant portal | `/tenant/dashboard`, `/tenant/profile`, `/tenant/lease`, `/tenant/payments`, `/tenant/bills` | Tenant portal and tenant utility-bill endpoints | `app/tenant/*` / Tenant shell | Tenant summary, profile, lease, payment and bill DTOs | `tenant.portal.access` | Planned phase 2; honest unavailable states until delivered | Authenticated tenant context |
| Platform administration | `/platform/dashboard`, landlord registrations, plans, subscriptions, plan changes | Platform admin endpoints | `app/platform/*` / System-admin shell | Platform summary, registration, plan, subscription and review DTOs | `platform.*` | Planned phase 2; honest unavailable state until delivered | System-admin role |
| Settings/profile | `/settings`, `/profile`, `/preferences` | Auth profile plus feature-specific settings | `app/more/profile` / Profile and settings | `UserProfileDto` plus supported preference requests | Authenticated | Planned; several Web pages are placeholders | Profile update contract verification |

## Contract decisions

- The native client persists access and refresh credentials only in platform secure storage. The refresh token is captured from the login/refresh `Set-Cookie` header and is sent in the supported refresh request body.
- Every protected request uses Bearer authentication. A single in-flight refresh operation serves concurrent 401 responses; each request is retried at most once.
- Role resolution matches the Web client: `SYSTEM_ADMIN` wins, otherwise the active-company role is used, then the first supported company role.
- Permissions control navigation affordances only. Backend authorization remains authoritative.
- The first screen shows only fields returned by `DashboardSummaryDto`. It does not invent activity, trends, currency conversion, or business rules.

## Design-system gaps closed by production composition

The established tokens and primitives remain the source of truth. Production screens add composition-level patterns—bootstrap gate, authenticated shell, pull-to-refresh, form validation, retry/error presentation, and role-aware empty states—without introducing parallel colors, spacing, typography, or bespoke component styling.

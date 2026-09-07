# Remaining Web MVP — implementation report

Date: 2026-09-03. Scope: Parking & Garages, Documents, Notifications, Settings.

Implemented the bounded frontend scope using existing APIs and the existing Web components. This is not a claim that every backend capability is production-ready. Confidential-document security remains limited by existing backend behavior. No backend, Flutter, database, migration, authentication, or authorization implementation was changed. No dotnet, database, migration, or Docker commands were run. Pre-existing worktree changes were preserved.

## 1. Existing files changed by this task

All paths below are under `D:/AqariOS/AqariOS/frontend/`.

| File | This task's change |
| --- | --- |
| `package.json` | Focused test/type-check scripts and test/type tooling dev dependencies; retained existing scripts/dependencies. |
| `package-lock.json` | Dependency installation lockfile changes. |
| `src/app/router/AppRouter.tsx` | Replace four existing placeholder route elements with implemented pages. |
| `src/features/notifications/api/notifications.api.ts` | Encode page size and both cursor fields into the request URL; the existing HTTP client does not support a `params` option. |
| `src/features/notifications/types/notifications.types.ts` | Match current backend notification type, priority, and delivery-status enum values. Read state remains separate. |
| `src/features/notifications/hooks/useNotifications.ts` | Add company to existing user-scoped query keys; disable automatic inbox-query retries. Preserve polling/invalidation architecture. |
| `src/features/notifications/components/NotificationBell.tsx` | Sent-only read eligibility, truthful delivery/read labels, action error feedback, returned mark-all count. |
| `src/features/tenantPortal/pages/TenantDashboardPage.tsx` | Correct its existing notification read guards to Sent and unread, with pending-mutation protection. |
| `src/shared/i18n/translations/ar.ts` | Register module copy and correct the Parking & Garages navigation label. |
| `src/shared/i18n/translations/en.ts` | Register module copy. |
| `src/shared/components/layout/Breadcrumbs.tsx` | Resolve these four module labels explicitly, avoiding existing missing-translation fallback behavior. |
| `src/shared/components/layout/PageContainer.tsx` | Add optional `showBreadcrumbs`, defaulting to existing behavior. New pages disable the duplicate breadcrumb already rendered by AppLayout. |

This list describes only this task's edits, not every dirty file in the repository.

## 2. Files added

- `src/features/parking/ParkingPage.tsx`
- `src/features/parking/parking.api.ts`
- `src/features/documents/DocumentsPage.tsx`
- `src/features/documents/documents.api.ts`
- `src/features/notifications/pages/NotificationsPage.tsx`
- `src/features/notifications/utils/inbox.ts`
- `src/features/settings/SettingsPage.tsx`
- `src/features/settings/settings.api.ts`
- `src/features/mvp/copy.ts`
- `src/features/mvp/primitives.tsx`
- `src/features/mvp/test-setup.ts`
- `src/features/mvp/workflows.mvp.test.tsx`
- `vitest.mvp.config.ts`
- `tsconfig.mvp.json`
- `MVP_IMPLEMENTATION_REPORT.md`

The small module helpers compose existing controls, loading/error states, dialogs, and localization; they do not introduce another design system or HTTP transport.

## 3. Routes

No new route paths. Existing `/parking`, `/documents`, `/notifications`, and `/settings` placeholders now render functional pages. No Maintenance or Marketplace navigation was added.

## 4. API endpoints consumed

All paths use the existing shared HTTP client, existing base URL, and existing session handling.

| Module | Method and path |
| --- | --- |
| Lookups | `GET /api/v1/buildings`; `GET /api/v1/apartments?buildingId=…` |
| Parking | `GET`, `POST /api/v1/buildings/{buildingId}/parking-spots` |
| Parking | `PUT`, `DELETE /api/v1/parking-spots/{id}` |
| Categories | `GET`, `POST /api/v1/document-categories` |
| Categories | `PUT`, `DELETE /api/v1/document-categories/{id}` |
| Documents | `GET /api/v1/buildings/{buildingId}/documents?categoryId=…&searchTerm=…&pageNumber=…&pageSize=50` |
| Documents | `GET /api/v1/building-documents/{id}` |
| Documents | `GET /api/v1/building-documents/{id}/download-url` |
| Notifications | `GET /api/v1/notifications/me?pageSize=50` with `lastSeenCreatedAt` and `lastSeenId` together on subsequent pages |
| Notifications | `GET /api/v1/notifications/me/unread-count` |
| Notifications | `PATCH /api/v1/notifications/{id}/read` |
| Notifications | `PATCH /api/v1/notifications/me/read-all` |
| Settings | `GET /api/v1/companies/me` |
| Settings | `PUT /api/v1/companies/{companyId}` |
| Settings | `GET`, `PUT /api/v1/companies/{companyId}/settings` |
| Security | Existing `POST /api/v1/auth/logout-all`; existing logout workflow is reused afterwards. |

The existing bell continues to request its smaller 20-item preview. Password recovery and subscription remain links into existing modules, not recreated workflows.

## 5. Parking capabilities

- Current-company active building selection using existing Properties lookup.
- Building-scoped list: spot code, type, location description, default apartment, active state. Preserve server SpotCode ordering; no invented search/pagination.
- Create/edit supported fields only; required code/max 50, type validation, description max 255, optional default apartment including clearing to null.
- Apartment lookup errors/loading block saving rather than silently losing selection.
- Archive confirmation and existing dependency-aware ARCHIVE_BLOCKED dialog, including PARKING_ASSIGNMENTS. No assignment screen.
- Respect properties.read/create/update/delete and the existing properties.manage alternative; backend remains authoritative.

## 6. Document capabilities

- Category list/create/edit/delete with confirmation, permission controls, and DOCUMENT_CATEGORY_IN_USE feedback.
- Building document list, submitted server-side name search, category filter, page-number pagination at 50 items. Search/filter reset to page 1; no local filtering/infinite scroll.
- Detail sheet: document/category/building ID, dates, confidentiality, filename, MIME, byte size, description, uploader ID, creation date. No invented user-directory lookup.
- Document-specific signed download only; reject executable/credential-bearing URLs, do not attach Bearer headers to the storage navigation, and do not log signed URLs.
- Denied detail/download renders existing error infrastructure; no generic-file fallback. Attachment mutations are intentionally absent because the documented backend gaps remain unresolved.

## 7. Notification capabilities

- Personal inbox, type/delivery/read labels, expandable content, unread count, refresh, next/previous cursor navigation.
- Both keyset fields preserved; ordering remains server-owned CreatedAt DESC, Id ASC.
- Only Sent notifications without readAt are eligible for read actions, including existing bell/dashboard consumers.
- Mark-all feedback uses returned markedCount; query invalidation refreshes count/list caches.
- Existing unread-count polling retained. No new realtime/push architecture.
- Unavailable-module notification content is displayed without invented destinations.

## 8. Settings capabilities

- Company profile read and update of legalName, displayName, primaryPhone, primaryEmail only.
- Registration, tax number, company type, country, active state remain read-only. Company type is localized.
- Operational update only: rentGracePeriodDays, lateFeeType, lateFeeValue, fiscalYearStartMonth. None sends null; other fee types require positive values. Currency/language/timezone are read-only API values.
- Company ID must match authenticated company before rendering company forms. Query keys and page state are user/company scoped. This is UX/cache isolation, not a replacement for backend authorization.
- Existing theme/language providers, profile display, logout, confirmed logout-all, recovery and subscription links reused.
- Forms do not reset on focus/reconnect background refetch; successful saves reload server state.

## 9. Explicit exclusions and their classification

| Capability | Classification/reason |
| --- | --- |
| Garage CRUD, assignments, availability, reservations, parking fees/documents | Unavailable APIs / outside approved parking-spot contract. |
| Parking search/pagination | Intentionally excluded; current list contract does not provide them. |
| Document attachment creation | Security limitation: building/company ownership gap; excluded conservatively, not purportedly secured by a frontend selector. |
| Document replacement, metadata mutation, soft/hard deletion | Confidential mutation security limitations / bounded MVP exclusion. No attachment mutation buttons exposed. |
| Expiry dashboard/reminders/jobs | Intentionally excluded; not required by current navigation. Automatic reminders would be a future feature, not implied by expiry-read API. |
| Company-wide notification sending/view, arbitrary recipient selection | Backend authorization/RLS limitations; not exposed. |
| Notification templates | Intentionally excluded; not required for current personal-inbox MVP. |
| Email/SMS/WhatsApp delivery/resend, push, new SignalR client | Intentionally excluded; no claim that external delivery is operational. |
| MFA, session/device list, new password-change/preferences API | Unavailable/unauthorized scope; future feature only with supported contracts. |
| Maintenance, Marketplace | Explicitly skipped. |

## 10. Inherited security limitations

1. Confidential document enforcement differs between document-specific paths and generic file paths. The UI never falls back to generic file download endpoints. Frontend controls do not fix the backend inconsistency.
2. Document creation has a building/company ownership gap; confidential mutation paths are also inconsistent. These workflows remain excluded.
3. Confidential denial can be returned as HTTP 401. The unchanged shared HTTP client treats 401 as a session-refresh condition and may retry once/redirect to sign-in before the page can show denial. Query-level retry is disabled, but this does not override shared authentication behavior. This remains an integration limitation, not a frontend security fix.
4. Company-wide notification authorization/RLS limitations remain. Only personal endpoints are consumed.
5. No credentials, tokens, or signed URLs were deliberately read, stored, or logged during validation. Test fixtures contain only synthetic test data and are not wired into production pages.

## 11–12. Focused tests and totals

`npm run test:mvp`: **18 passed, 0 failed**.

- Parking: 4 tests cover scoped list/edit, invalid/valid create, archive confirmation/dependencies, read-only actions.
- Documents: 5 tests cover server search/filter/page parameters, details/document-specific download, confidential 401 denial without fallback, category create/edit/in-use delete, unsafe signed-URL rejection.
- Notifications: 5 tests cover bell delivery eligibility, inbox/count/read/read-all, cursor pair/previous, loading/empty, error/retry.
- Settings: 4 tests cover profile load/exact update payload, operational validation/null fee payload/read-only values, permission behavior, load failure.

Tests render real components with existing providers; only HTTP/auth boundaries are mocked. Mutations are asserted without writing records to the live company. Shared Radix wrappers emit existing React ref warnings during tests; the suite passes despite those warnings.

## 13. Build

`npm run build`: **PASS**. Vite transformed 3,405 modules. Main JS approximately 1,767 kB / 478 kB gzip. Existing large-chunk warning remains; no bundling architecture changes were introduced.

## 14. TypeScript, lint, other checks

`npm run typecheck:mvp`: **FAIL**, eight diagnostics in existing imported files, none in the newly added module/test files:

- apartments/schemas/apartments.schema.ts:11 — Zod invalid_type_error option.
- apartments/utils/apartmentMappers.ts:52 — currency union mismatch.
- auth/providers/AuthProvider.tsx:492 — impossible narrowed-state comparison.
- buildings/schemas/buildings.schema.ts:6,18,21 — Zod errorMap/invalid_type_error options.
- shared/lib/http.ts:166 — non-callable callback type.
- shared/services/storage.ts:118 — generic value/string mismatch.

These files were not edited by this task. The focused tsconfig still follows their imports; the failures were not hidden or suppressed. A full-repository type-check was not claimed.

No lint command/configuration is provided in this frontend, so lint is **not available**, not a pass. New module/config files were formatted. `git diff --check` passed.

The existing `verify:error-handling` command reported its assertions passed and exited 0, but emitted Vite dependency-scan shutdown errors afterwards; it is not a clean-log pass. npm installation reported three high-severity dependency advisories; attribution and remediation were not performed, and no broad audit-fix upgrade was run.

## 15. Manual browser verification

Used the signed-in local Web app in the Codex browser, not a mock-data build. No company records were created/updated/archived/deleted and no sessions were revoked.

| Check | Result |
| --- | --- |
| Four destinations | Opened all four with the real session. |
| Parking | Real building options; genuine empty list; create dialog and apartment lookup; long mixed Arabic/English description input inspected without save. |
| Documents | Real empty document/category states, building selection, submitted search, disabled pagination for empty result, category create dialog opened/cancelled. |
| Notifications | Genuine empty inbox, unread count 0, read-all and next/previous appropriately disabled. |
| Settings | Real profile/operational fields load; readonly values verified; logout-all confirmation opened/cancelled. |
| Languages/themes | Arabic/RTL dark and English/LTR light checked using existing controls; original Arabic/dark restored. |
| Responsive | Desktop 1280px dialog bounds and approximately 768px tablet DOM layout checked; no root horizontal overflow measured. Normal embedded browser width (~608px) light Settings screenshot inspected. |
| Navigation labels | Missing module breadcrumb labels and duplicate module breadcrumbs found and corrected locally; Parking Arabic navigation label corrected. |
| Live mutation/error/permission scenarios | Not exercised on the live company. Automated coverage only. |
| Populated document detail/download; long list text | Not live-verified: available account/building data was empty. Automated fixtures exercise populated workflows. |

Viewport overrides were reset. Resized in-app screenshots were clipped/inconsistent with DOM viewport measurements, so a complete desktop/tablet visual sign-off is not claimed. Additional populated-data visual verification remains necessary. Loading/error/permission behavior has automated coverage; not all states were manually reproduced.

## 16. Findings by severity

- **P0:** No remaining crash observed in the exercised new routes. Tests found a Settings link crash caused by the existing shared Button asChild composition; new Settings links use existing buttonVariants directly, without changing the shared button.
- **P1:** Inherited document security gaps block attachment mutations/full confidential production readiness. Existing 401/session behavior can disrupt confidential denial UX. Those backend/auth paths were not changed. Dependency advisories require separate assessment.
- **P2:** Eight existing TypeScript diagnostics prevent a clean check; large JS chunk warning; existing shared Radix ref warnings. Full populated-data/responsive visual and live restricted-role verification remains pending.
- **P3:** Optional later polish: resolve displayed building/uploader IDs through already-authorized lookups if appropriate; no speculative directory/API was introduced.

## 17. Final Web MVP scope matrix

| Module | Implemented safe frontend scope | Remaining boundary | Status |
| --- | --- | --- | --- |
| Parking & Garages | Building parking list/create/edit/archive and permission/error handling | No garage/assignment/availability API; live mutation QA not performed | Implemented within parking-spot contract |
| Documents | Categories CRUD; building search/filter/pages; detail and specific signed download | Attachment/confidential security gaps; live populated download QA pending | Bounded read/category MVP only |
| Notifications | Personal inbox/count/read/read-all/cursors and existing bell integration | Unsafe company administration excluded; no delivery claims | Implemented personal MVP |
| Settings | Company/operational allowed-field forms; existing appearance/security/subscription entry points | No invented writable settings or security APIs; live save QA not performed | Implemented within current contracts |
| Maintenance / Marketplace | None | Explicitly skipped | Not implemented |

Decision: the approved bounded frontend functionality is implemented and the focused suite/build pass. Do not label the entire four-module backend surface production-complete. Backend security constraints, existing type-check failures, and the stated live-validation gaps remain visible for follow-up.

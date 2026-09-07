# Module 6 — Flutter Rent Payments

## Scope and audit correction

The payment implementation consumes the existing v1 contracts and plugs into the company Finance and existing authenticated tenant Payments destinations. No backend, Web, Properties source, database, migration, endpoint, or permission definition was changed.

The earlier inspection report incorrectly described cheque list/upcoming/lifecycle actions as existing Web workflows. A repository-wide search during implementation found no Web calls to `/cheques`, `/cheques/upcoming`, or `/cheques/{id}/status`; Web currently exposes cheque information and tenant cheque submission. Per the final parity decision, standalone cheque list, upcoming cheque and cheque lifecycle screens are intentionally not exposed in Flutter at this stage. No lifecycle API calls have been added. Existing cheque data/statuses and tenant cheque submission remain supported where they are part of confirmed Web payment workflows.

Backend-only manual payment, allocation mutation/reversal, receipt issuance, installment generation, payment cancellation, and Expenses remain excluded.

## Integration

- `features/payments/domain`: typed read models, enum normalization, exact thousandths validation, filter/query parameters, tenant ordering, submission payloads, safe financial errors.
- `features/payments/data`: PaymentsRepository consumes the existing ApiClient; PaymentFiles uses the existing LeaseFiles transport and enforces the Web proof limits.
- `features/payments/application`: PaymentsController owns loading, errors, paired-cursor history and stale-search suppression.
- `features/payments/presentation`: company records, filters, detail, owner verification queue/review, tenant submission; existing design-system components and section/row layouts.
- `app.dart`/`app_shell.dart`: dependencies and user/company scoped screens. Session changes discard repository results and cancel the shared uploader.
- Shared ApiClient: additive authenticated PDF read using the existing refresh concurrency/single retry/timeout/error path; reject a successful non-PDF response.
- Shared LeaseFiles: additive module/entity arguments; original Leasing upload defaults remain unchanged.
- `share_plus`: returned PDF bytes are handed to the device's native share/save sheet. No recipient is selected automatically. The plugin may create temporary app-cache files for handoff. Web falls back to browser download when native sharing is unavailable. Signed receipt/proof links open through the existing external handler.

## Consumed API contracts

| Endpoint | Request / response |
|---|---|
| GET `/api/v1/rent-payments` | `pageSize=50`, optional `buildingId`, `status`, inclusive `dateFrom/dateTo`, trimmed `searchTerm`, paired `lastSeenId/lastSeenDueDate`; payment array |
| GET `/api/v1/rent-payments/{id}` | Enriched payment with incoming/outgoing allocations, submissions, cheque, transaction receipts, settlement summary |
| GET `/api/v1/owner/payments/pending-verifications` | `pageSize=50`, optional opaque cursor; items/nextCursor/hasMore |
| POST `/api/v1/owner/payments/{id}/submissions/{submissionId}/approve` | Empty body; 204 |
| POST `/api/v1/owner/payments/{id}/submissions/{submissionId}/reject` | Trimmed reason; 204 |
| GET `/api/v1/tenant-portal/payments` | Authenticated tenant array; no supplied tenant/company ID |
| POST `/api/v1/tenant/payments/{id}/submit-verification` | Positive amount, numeric method, reference, confirmed proofFileId, optional chequeDetails; submission ID |
| POST `/api/v1/rent-payments/{id}/remind` | Empty object; backend reminder result |
| GET `/api/v1/rent-payments/{id}/receipt` | Receipt metadata; 404 is not-issued; other failures remain errors |
| GET `/api/v1/rent-payments/{id}/settlement-statement` | Authenticated PDF |
| GET `/api/v1/tenant-portal/payments/{id}/settlement-statement` | Tenant authenticated PDF |
| GET `/api/v1/files/{id}/download-url?inline=...` | Signed URL; proof preview vs receipt download |
| POST `/api/v1/files/upload-request` | `moduleName=Financials`, payment entity ID, filename, MIME, size |
| PUT returned signed URL | Binary bytes, content type and storage-required blob header only; no bearer/cookie headers; redirects disabled |
| POST `/api/v1/files/confirm` | fileId/storageKey/originalFilename/MIME/size; confirmed ID |
| GET `/api/v1/buildings` | Existing building filter lookup through PropertiesRepository |

## Parity matrix

| Web capability | Flutter equivalent | Status |
|---|---|---|
| Financial records grouped into obligations, receipts, adjustments | Row-based sections preserving array order | Implemented |
| Debounced server search | 350ms search, cancel stale results | Implemented |
| Building/status/due-date filters, quick presets, reset | Filter sheet, presets, reset | Implemented |
| Keyset previous/next | Existing date/ID cursor history, 50 records, no invented total | Implemented |
| Refresh/loading/empty/error/retry | Shared DS states and pull-to-refresh | Implemented |
| Detail, terms, paid/remaining, reference, dates, notes | Full-screen detail | Implemented |
| Incoming/outgoing allocations, reversal reason | Read-only detail sections | Implemented |
| Submission history and rejection reason | Status-labelled history sections | Implemented |
| Lease/building/unit context | Existing module detail routes | Implemented |
| Tenant relationship | Name and explicit Tenant Management dependency dialog | Deferred destination, per approval |
| Owner queue and cursor load-more | Queue with deduplication by submission ID | Implemented |
| Submitted amount vs installment/outstanding | Verification detail | Implemented |
| View proof and cheque submission fields | Signed file open; complete submission fields | Implemented |
| Approve confirmation and rejection reason | Pending-state and permission checks, confirmation, busy guard, recoverable errors | Implemented |
| Refresh after review | Queue reload and dashboard refresh | Implemented |
| Tenant summary, priority ordering and expanded details | Existing tenant Payments tab and full-screen details | Implemented |
| Submit/resubmit Cash, CliQ, Cheque | Same three choices, remaining default, exact <=3-decimal validation | Implemented |
| CliQ reference and proof; cheque dates/number/bank | Same required fields; server remains authoritative | Implemented |
| File selection/progress/remove/retry | PDF/PNG/JPEG <=15MB; reuse confirmed file ID after failed submit | Implemented |
| Receipt metadata and not-issued | Receipt sheet, signed PDF link, 404 state | Implemented |
| Transaction receipts and final settlement | Detail rows, receipt open, authenticated PDF share/save | Implemented |
| Reminder | Existing eligibility plus payments.approve, meaningful backend errors | Implemented |
| Cheque fields/status within payments | Details and tenant cheque submission | Implemented |
| Standalone cheque list/upcoming/lifecycle | No active Web consumer found; backend APIs are available but not exposed by current Web workflows | Deferred by strict Web parity |
| Financial Operations Expenses/KPI dashboard | Separate module/existing dashboard | Not duplicated |

## Permissions and security

Actual permission values are `payments.read`, `payments.approve`, `cheques.read`, `receipts.read`, and `receipts.issue`, not their C# constant names. Only permissions relevant to implemented actions are used. Tenant API context remains server-resolved. No client-supplied company scope is accepted.

Approval requires a still-pending loaded submission; the server rechecks state and amount. Changes are never optimistically treated as successful. Timeout wording asks the user to check payment state before retrying. No new financial calculation/settlement rules are introduced. UI balance validation uses integer thousandths to avoid binary floating-point rejection of valid balances; currency display follows Web's two-decimal convention.

Proof transport is separate from authenticated API calls. Dedicated tests assert no Authorization or Cookie headers on the signed PUT, exact bytes, disabled redirects and correct upload/confirmation contracts. No application token, signed URL, proof contents, or financial data is logged.

## Existing contract limitations / dependencies

- Company records return an array; Web infers next-page availability from a full 50-item page and a non-null final due date. Mobile preserves that limitation, including no continuation when the final due date is absent.
- Tenant payments return an array with no continuation. The existing Web summary uses that returned set and the first record's currency; Mobile preserves it without adding conversion or alternate totals.
- No user-selectable sort or amount/lease/unit filters are invented for company records. Tenant sort remains status priority then due date descending.
- Owner review may require `payments.read` for the detail in addition to `payments.approve` for the queue; a backend 403 remains visible.
- The detail projection can omit settlement summary or receipt metadata supplied by the list. Mobile follows Web's list/detail merge for those fields, while fresh detail status and paid amount remain authoritative. This was verified against a live payment and covered by a regression test.
- Receipt and signed-file availability depends on configured file storage. The external viewer/share target is a device dependency.
- Tenant Management, tenant Lease/Profile, Expenses, Notifications destinations remain their own modules.
- No future backend change is needed to consume the implemented contracts. Pagination/search evolution remains separately authorized future work.
- Standalone cheque list/upcoming/lifecycle APIs are available on the backend, but are intentionally not exposed in Mobile because the current Web source does not expose them as active user-facing workflows. If Web later introduces those workflows, Mobile should add the equivalent native experience during that parity update.

## Validation

- `dart analyze lib test`: no issues found.
- Full Flutter regression suite: **89 tests passed**, including the final settlement-summary fallback regression.
- Payment coverage includes shared PDF refresh concurrency, signed upload isolation, stale reads, amount precision, filters, review conflicts, submission retry, permissions, receipt states and Arabic dark/large-text rendering. Fixtures are test-only; runtime screens consume real APIs.
- Android debug build succeeded and launched on `emulator-5554` (Pixel 8). The final display correction was hot-reloaded and visually verified.
- Read-only live API smoke validation: authenticated company Finance list and payment details in Arabic RTL, light and dark mode. Remaining balance displayed correctly after the list/detail merge correction. The emulator's original light-mode setting was restored.
- Font-backed Pixel 8-sized visual tests cover the Arabic list, dark details and tenant submission form. These screenshots use synthetic test fixtures, not live financial records.
- No real submission, approval, rejection, reminder or proof upload was performed. Tenant and review mutations are covered by automated tests, not claimed as live-account end-to-end validation. Native external PDF viewers/share targets still require device-level interaction validation.
- Standalone cheque list/upcoming/lifecycle is intentionally deferred by strict Web parity. The implemented Module 6 payment scope is functionally complete pending final parity/audit review.

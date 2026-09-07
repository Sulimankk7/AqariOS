# Leasing mobile implementation and parity review

Scope: the approved company-admin Leasing flow. Web is the functional reference; backend contracts are consumed unchanged. The separate Tenants Management module is explicitly deferred. Properties implementation files are not changed.

## Architecture and changed areas

- `lib/features/leasing/domain`: contract/detail/document/history/settlement/tenant-lookup/utility models, form payloads, Web search/sort behavior, permission/status actions, localized errors.
- `lib/features/leasing/data`: repository over the existing ApiClient; signed-file transport and native file/open integration.
- `lib/features/leasing/presentation`: list, detail/history, forms, documents, embedded utilities and shared module widgets.
- `lib/app.dart`, `lib/features/shell/presentation/app_shell.dart`: dependency injection and company-admin Leasing destination. Tenant/company changes invalidate reads, cancel signed transfers, discard the previous list state and leave child routes through the existing session flow.
- `lib/core/network/api_client.dart`: additive ID-response POST, no-content POST, object-query support; preserve timeout identity and Retry-After seconds. Existing authentication, refresh single-flight and single-retry behavior remain in use.
- `lib/core/network/api_problem.dart`: optional Retry-After seconds.
- `lib/core/design_system/components/forms/aqari_fields.dart`: optional `maxLines` (default 1), required for Web notes/reasons. Deferred keyboard-focus work is not included.
- `pubspec.yaml` / lockfile and generated Flutter plugin registrants: `file_selector`, `url_launcher` for existing document capabilities.

No production fixture data, extra API architecture, business transitions, permissions, backend changes, or machine-specific API defaults are introduced.

## Existing endpoints

`C = /api/v1/leasing/contracts`, `U = /api/v1/utility-bills/accounts`.

| Capability | Existing endpoint |
|---|---|
| Contract results | GET C/search?searchTerm=&pageSize=50 |
| Expiring | GET C/expiring?daysAhead=7/14/30/60/90/120 |
| Detail | GET C/{id} |
| Identifier suggestion | GET C/next-number |
| Create | POST C → JSON ID, 201 |
| Update | PUT C/{id} → 204 |
| Activate | POST C/{id}/activate → 204 |
| Renew | POST C/{id}/renew → new JSON ID, 201 |
| Terminate | POST C/{id}/terminate → 204 |
| Apartment history | GET C/history/apartment/{id}?pageSize=50 |
| Tenant lookup | GET /api/v1/leasing/tenants |
| Property lookups/details | Existing PropertiesRepository GET /api/v1/buildings, /api/v1/apartments?buildingId=…, /api/v1/apartments/{id} |
| Attach | POST C/{id}/documents |
| View/download | GET C/{id}/documents/{documentId}/download?inline=true/false&redirect=false |
| Replace document | PUT C/{id}/documents/{documentId}/replace |
| Delete document | DELETE C/{id}/documents/{documentId} |
| Upload authorization | POST /api/v1/files/upload-request |
| Binary upload | PUT the returned signed URL, unchanged |
| Confirm file | POST /api/v1/files/confirm |
| Utility accounts | GET U?leaseContractId=…&includeUnlinked=true&pageSize=25[&cursor=…] |
| Link account | POST U |
| Replace account | POST U/{id}/replace |
| Request sync | POST U/{id}/sync |
| Unlink | DELETE U/{id} |

## Capability-by-capability parity

Permissions: `A` = company-admin route + authenticated company-scoped read; `C` = contracts.create; `P` = contracts.approve; `U` = UtilityBills.Manage. These are existing policies, not new ones.

Tests: D = leasing_data_test.dart, W = leasing_widgets_test.dart, V = leasing_visual_test.dart. “Implemented” describes code/automated evidence, not a claim that a real tenant's production transactions have been executed.

| Web capability | Flutter implementation | API | Permission | Validation | Error behavior | Status behavior | Test coverage | Result |
|---|---|---|---|---|---|---|---|---|
| Contract list | Lazy list rows with compact dates/rent/frequency | C/search | A | Strict model decoding | Retry/empty/loading | All eight statuses, unknown fallback | D repository, W list, V | Implemented |
| All / Expiring | AqariTabs and same six expiry choices | C/search, C/expiring | A | Fixed Web choices | Retry current query | Expiring eligibility remains server-defined | W expiry | Implemented |
| Actual search | Search all scalar values in loaded response; clear button | No extra request while typing | A | Trim/case-insensitive as Web | Matching empty state | Does not change status | D search, W request count | Implemented |
| Actual sort | Number/status/start/end/rent; asc/desc/server order | None | A | Same accessor semantics, including rent string ordering | No synthetic records | Localized status labels | D sorting | Implemented |
| Local table paging | Ten loaded rows/page, previous/next/count | None | A | Clamp current page when results change | No fabricated backend total/cursor | Preserves result set | W paging | Implemented |
| Details | Terms, finances, relationships, optional signed date/notes | C/{id} | A | Typed detail | Loading/error/retry, pull refresh | Badge and eligible actions | W detail/actions, V | Implemented |
| Status history | Status/date/reason rows | C/{id}.statusHistory | A | Typed events | Empty history | Display server sequence | V/detail tests | Implemented |
| Apartment history | Leasing-owned history route with local search/sort/page | C/history/apartment | A | Cap remains 50 | Loading/empty/error/retry | Historical status display | D history, W navigation | Implemented |
| Create | Full-screen sections and dependent selectors | POST C; property/tenant lookups | C | Required identities/number/dates, positive rent, nonnegative deposit, due day 1–28 | Field errors and global server errors; retain input | Creates draft, server authoritative | D payload/rules; W suggestion | Implemented |
| Number suggestion | Optional editable suggestion; late response never overwrites user edits | C/next-number | C | Only untouched empty field accepts suggestion | Manual entry remains possible on suggestion failure | Create only | W late suggestion | Implemented |
| Apartment rent default | Set only positive JOD apartment amount; editable | Existing apartment lookup | C | No cross-currency conversion | Selector loading/empty/retry | Does not alter apartment | Form code and D payload | Implemented |
| Edit | Prefilled terms, immutable contract number | PUT C/{id} | C | Same form rules; no number in payload | Signed-document, renewal identity, overlap and not-draft errors | Web offers Draft/Pending Signature; backend rejects Pending Signature | W pending rejection, D payload | Implemented |
| Activate | Blocking confirmation with pending state | POST activate | P | Server gates start date/signed document/overlap | Failure stays open; retry | Draft/Pending Signature action visibility | W activation retry, D actions | Implemented |
| Renew | New-number/terms form, Web period and amount defaults | POST renew | P | No apartment/tenant fields | Existing successor/date/overlap errors | Active/Expired action visibility; new draft | W renewal submit, D defaults | Implemented |
| Terminate | Settlement form and destructive submission | POST terminate | P | Nonnegative amounts, type/date, server date/reason rules | Field/global rejection preserves form | Active only | W termination, D settlement | Implemented |
| File selection | Native picker; filename and remove/reselect | Platform picker | C | Web file types and 25 MB limit | Recoverable selection error | Attach/replace availability follows Web | D file validation, W document state | Implemented |
| Upload/confirm/attach | Phased progress; attach confirmed ID | Files endpoints + signed PUT + POST documents | C | File length/type; server content validation | Retain confirmed ID after attach failure to retry without reupload | No invented signing transition | D credential-free bytes, W retry | Implemented |
| View/download | Fresh signed URL opened via platform handler | Document download URL | C | HTTP(S) URL | Opening errors remain visible; retry obtains fresh URL | Available on terminated contracts too | W view/download, D query | Implemented |
| Replace document | File-selection form, description fallback, existing document type | Files pipeline + PUT replace | C | Same file limits | Retain form and confirmed ID on failure | Hidden on terminated contracts | D replace mapping; shared upload tests | Implemented |
| Delete document | Explicit confirmation and pending protection | DELETE document | C | Existing selected document | Keep dialog open on failure | Hidden on terminated contracts | W confirmed deletion, D mapping | Implemented |
| Prior contract | Navigate to referenced contract | C/{priorId} | A | Existing prior ID | Normal detail retry/error | No transition invented | W prior navigation | Implemented |
| View apartment | Reuse existing ApartmentDetailsScreen | Existing PropertiesRepository | Existing API permissions | Existing screen | Existing screen handling | Properties implementation unchanged | Wiring/source review | Implemented |
| View tenants | Visible action explains missing destination; no substitute Tenant screen | None | A | No invented destination | Explicit dependency dialog | No business change | W dependency | Explicitly deferred by user |
| Utility current/history | Electricity/water rows, sync badge, expandable prior accounts | GET U (existing cursor contract) | U | Typed page and account enums | Independent loading/error/retry | Current vs unlinked accounts | D query, W utility | Implemented |
| Link utility | Lease-scoped form, selectable existing type | POST U | U | Electricity 10 digits; water 1–20; optional meter | Retain input and show failure | Existing server eligibility/duplicate rules | W validation/payload | Implemented |
| Replace utility | Existing account form and preserved type | POST U/{id}/replace | U | Same number validation | Recoverable failure | Existing server replacement semantics | Repository + shared form implementation | Implemented |
| Sync utility | Request sync, reload, success feedback | POST U/{id}/sync | U | Disabled while Syncing/pending | Show failure without success | No invented polling/processing result | W sync | Implemented |
| Unlink utility | Confirmation explaining history preservation | DELETE U/{id} | U | Existing account | Server can reject while sync is claimed | No local deletion on failure | W unlink confirmation | Implemented |
| Authentication / tenant boundary | Reuse ApiClient/session; discard old screen state and reads | Existing auth flow | Existing policies | No contracts.read invented | Existing refresh/single retry; timeout/rate-limit semantics | Scope changes cancel transfers | D stale read, existing auth suite | Implemented |
| Arabic/English/dark/accessibility | Existing tokens, fonts, fields, rows, badges, semantics, 48px actions, bidi isolation | None | Same actions | Long text; 1.5× scale | No raw technical exception strings | Semantic colors and labels | W RTL/scale, V three screens | Implemented |

## Deliberately preserved constraints

- Actual Web DataTable ignores the old `onSearchChange` prop passed by Leasing. Its visible search, sorting and ten-row pagination operate on the loaded result array. Mobile preserves that behavior. The existing server search accepts contract number/external registration text, but enabling a new server-search interaction is outside this approved parity scope.
- Contract search and apartment history return at most the requested 50 results (backend maximum 200) with no continuation or total. Expiring and lookup endpoints remain their existing arrays. No cursor pagination is claimed for contracts.
- Utilities have a real existing cursor contract. Additional returned pages are requested explicitly; this does not create contract pagination.
- Pending Signature edit remains offered with `contracts.create`; the actual backend rejection is shown. Signed-document edits and renewal identity changes are likewise rejected by the server.
- Currency is controlled by the Leasing backend; no currency selector/conversion is invented.
- Web status labels do not imply APIs for cancellation, archive/delete contract, manual expiry, or signing. No such actions are added.
- Tenant Portal/tenant payments are separate workflows, not silently implemented inside company-admin Leasing.
- Properties remains approved. Apartment history is available inside Leasing; adding the reverse entry point to Properties is deferred.
- Viewing/downloading uses the platform browser/document handler for the returned signed URL, equivalent to Web opening that URL. External handler availability is device-dependent. Signed URLs and credentials are not logged or persisted by Leasing.
- If binary upload/confirmation succeeds but final attach fails, retry reuses the confirmed file ID while the screen remains open. Failed/abandoned upload cleanup remains the existing server's responsibility; there is no invented cleanup API.
- Native file selection and signed storage access require a configured device and reachable configured storage host. No stored credentials were inspected and no real company records were created for automated tests.

## Validation and deferred work

- `dart analyze lib test`: no issues found after the final source adjustments.
- Full Flutter suite: 64 tests passed (28 new Leasing tests plus 36 existing tests). The subsequent focused visual/data run also passed (12 tests).
- Pixel 8 logical viewport visual review: Arabic list, Arabic dark detail, Arabic create; fixture-backed widget renders, not a claim of live Android workflow verification.
- Android debug build: succeeded with the new file-selector and URL-launcher Android plugins. The ordinary build hit Kotlin incremental-cache cross-drive roots (`C:` pub cache vs `D:` project), followed by already-registered cache errors. The initial command-line workaround did not persist for ordinary Flutter runs.
- Android build follow-up: `android/gradle.properties` now disables Kotlin incremental compilation and uses in-process compilation. Flutter `run -d emulator-5554 --no-pub --dart-define=AQARIOS_API_BASE_URL=http://10.0.2.2:5235` successfully built, installed, and launched without those cache errors. The authenticated dashboard loaded on the emulator. The URL remains supplied at invocation, not committed as an app default. SDK, NDK, plugin versions, and backend remain unchanged. Non-incremental Kotlin compilation can increase rebuild time.
- Windows desktop plugin generation reported missing symlink/Developer Mode support. Android compilation succeeded independently. No Windows machine setting was changed.
- Shared keyboard-focus refinement, global Unit creation polish, Properties pagination/search, and release/profile performance testing remain explicitly deferred.
- Future backend contract work: continuation/total/search evolution for capped contract/history results only when separately authorized. No backend change is required to consume the currently approved Leasing capabilities.

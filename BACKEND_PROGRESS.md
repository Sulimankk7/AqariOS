# AqariOS Backend Progress

> **MISSION STATE: Definition of Done REACHED** (2026-07-26) with documented external blockers only — see `docs/backend-progress/14-production-readiness.md`. Final verification: build **0W/0E**, unit **746/746**, integration **118/118**, **0 skips**. Working tree is READY FOR USER COMMIT; five generated migrations are pending USER apply (`dotnet ef database update`). Remaining externally-blocked items: Reports (no approved spec), real payment/notification gateways (credentials/specs), tenant invitation flow (intentional deferral), deployment gates (non-superuser DB role, secret provisioning).

> Authoritative continuation checkpoint. Read with CLAUDE.md, `docs/backend-progress/` phase reports, `git status`/`git diff`. Updated 2026-07-26 (autonomous completion mission, new execution policy active: Claude runs build/test; never commits; never applies migrations to the local AqariOS DB).

## Current Objective
Complete the entire AqariOS backend to production readiness (Modules 1–11 + cross-cutting + hardening), per the phase plan in `docs/backend-progress/`.

## Current Phase
Phases 11–14 (final passes). **Phases 06–10 COMPLETE** (see docs/backend-progress/06–10 reports; Reports itself = BLOCKED on missing approved spec, documented in 10-files-reports.md). Verification at close: **build 0W/0E, unit 703/703, integration 118/118, ZERO skips** — the 4 previously-skipped Module 3 rate-limiter tests were rewritten against real routes and now pass (login/OTP dedicated limiters, Retry-After on 429, ForwardedHeaders CIDR trust replacing loopback defaults, loopback-socket test filter).
Previous phase summary:
Phases 06–09 (Maintenance/Documents/Notifications/Files APIs + security). Phases 04/05 are **COMPLETE** (build 0W/0E, unit 646/646, integration 110/0 — see docs/backend-progress/04+05). Previous phase summary:
Phase 04/05 (Financials, Modules 6–7). Correctness core COMPLETE and verified (unit 507/507, integration 110/0): client-generated UUIDv7 IDs across Financials (fixed fatal eFAWATEERcom callback Guid.Empty defect); FOR UPDATE allocation locking protocol (`GetByIdsForUpdateAsync`, sorted, deterministic) in Record/Reverse/ChequeBounce handlers with batch running totals + currency guard + soft-delete filters; DB trigger `trg_payment_allocations_enforce_limits` (migration `20260726150948`, obligation+receiving sides, adjustment exemption) proven by direct-SQL integration tests; removed `ValueGeneratedOnAddOrUpdate` from `amount_paid`/`due_date_status` (they NEVER persisted before — critical pre-existing defect; migration `20260726151426_Module6_AmountPaidAppMaintained` syncs snapshot, schema unchanged); receipts from `IssueReceipt`/`AttachReceipt` now explicitly Added (client-set keys made navigation discovery mark them Modified → DbUpdateConcurrencyException); `AllocationSettlement.DeriveStatus` shared helper; concurrency integration test proves exactly-one-wins. Remaining: exception sweep for ~13 other Financials handlers, missing commands (manual payment+cheque registration, manual receipt, cancel payment), 3 background jobs, API surface, pagination validators, pro-ration rounding.

## Overall Module Status
- Module 1 — Companies: **COMPLETE**
- Module 2 — Plans / Subscriptions: **COMPLETE**
- Module 3 — Security / Identity / RBAC / Audit: **COMPLETE** (hardening items in Phase 12: JWT fail-fast, refresh-token review)
- Module 4 — Properties: **COMPLETE** (Delete-policy regression fixed & verified)
- Module 5 — Leasing: **COMPLETE** (Contract Document Attachment completed, SignedContract EF persistence bug fixed, 13/13 focused unit tests passed, manual signed-document DB persistence verified, manual lease activation with SignedContract verified; tenant account/invitation flow remains intentional deferred scope)
- Module 6 — Rent Payments & Cheques: **COMPLETE** (full API + jobs + integrity core + §6.1 grace matrix; verified)
- Module 7 — Financial Operations: **COMPLETE** (API + webhook fail-closed + expiry job; real gateway = external blocker)
- Module 8 — Maintenance: **COMPLETE** (persistence restored, exceptions, full API, verified)
- Module 9 — Marketplace: **COMPLETE** (exception sweep done + verified)
- Module 10 — Documents: **COMPLETE** (full API, file security hardened, signed upload/download endpoints live, verified)
- Module 11 — Notifications: **COMPLETE** (RLS + per-row recipient policies, dispatch pipeline + providers, hub, full API, first test suites; external channel gateways = tracked blockers)
- Module 5 addendum — Tenant-person CRUD: **COMPLETE** (Tenant.Create previously had zero callers; commands/queries/repo/endpoints + national-id uniqueness incl. DB-race mapping + active-lease delete guard, verified)
- Cross-cutting — Files: **COMPLETE** (hardened physical provider; S3 remains a provider swap)
- Cross-cutting — Reports: **BLOCKED — no approved spec** (architecture review itself flags report_definitions/report_snapshots as never specified; see 10-files-reports.md)
- Production Hardening: **PARTIAL** (0 build warnings achieved; JWT fallback + secrets hygiene pending Phase 12)

## Completed Work
Increment 2 (this session, after Phase 03 close): Marketplace exception sweep — all 11 command handlers now use NotFound(404, with cross-tenant masking)/BusinessRule(422, wrapping domain InvalidOperationException)/Conflict(409); image-not-found remapped to 404. JWT fail-fast hardening (Program.cs + JwtTokenGenerator — no fallback secrets; placeholder rejected outside Development). `DbUpdateConcurrencyException` → 409 CONCURRENCY_CONFLICT in GlobalExceptionHandler. Verified: build 0W/0E, unit 507/507.

This session (uncommitted, on top of the prior Module 5 increment — see `docs/backend-progress/03-leasing.md` for detail):
- CLAUDE.md rewritten with the new execution policy (may build/test; never commit/push; never apply local-DB migrations).
- Module 5: client-side `Guid.CreateVersion7()` IDs for `LeaseContract`; `Create`/`Renew` commands now `ICommand<Guid>` returning the new ID with 201 CreatedAtAction; full exception-model migration in all 6 leasing handlers (NotFound/BusinessRule+codes/Conflict); deposit-deduction-reason test added.
- Warnings: all resolved — ILike null-guard in `RentPaymentRepository`, `string?` theory params (Marketplace/Maintenance domain tests), `PostgreSqlBuilder("postgres:17")` ctor. **Build: 0 warnings, 0 errors.**
- Phase docs created: `docs/backend-progress/00-current-state-audit.md`, `03-leasing.md`.

## Current Work In Progress
Phase 06–09 progress (this session):
- DONE Module 11 RLS: migration `20260726180807_Module11_NotificationsRls` — GRANTs (original migration omitted them entirely) + company policies on templates/deliveries + per-row recipient-ownership policies on notifications (INSERT company-checked only; unset `app.current_user_id` = trusted system context for jobs; `notifications.view_all` RLS bypass deferred per doc "own design pass" — REST-level ViewAll policy wired instead). Verified: 4 new RLS integration tests green.
- DONE file-storage security: path containment in `PhysicalFileStorageProvider.GetFullPath` (canonicalize + reject rooted/escaping keys), HMAC-signed capability URLs (`IFileUrlSigner`/`HmacFileUrlSigner`, purpose+key+absolute-expiry, fail-closed on missing `FileStorage:UrlSigningSecret` — NEW required config, 32+ bytes), tenant-prefix + FileId binding + on-disk size verification in ConfirmFileUpload (FileStorage.Create now accepts the upload-request id), real `PUT /files/upload` + `GET /files/download` endpoints (FilesController; bounded body reads, 411/413, nosniff + attachment disposition). Verified: 24 new unit tests (traversal/signer/roundtrip) green; suites unit 660/660, integration 114/0.
- DONE NotificationsHub + `/hubs/notifications` mapping + JWT `access_token` query-string support (hub paths only) + 8 new authorization policies (maintenance ×3, documents ×2, notifications ×3).
- DONE Modules 8/10/11 controllers (agent, 23 files: 5 controllers, 15 request models, 3 permission alias classes; endpoint→policy map in the agent report). Integration fix applied: removed client-supplied `UploadedBy` from AddMaintenanceAttachmentRequest (actor-attribution spoofing at the HTTP boundary; handler now always falls back to ICurrentUserContext).
- DONE Phase-12 rate-limiter hardening (my files): named `AuthLoginLimit` (5/60s default) + `AuthOtpRequestLimit` (3/60s) policies layered over the global limiter, config keys `RateLimiting:Login:*` / `RateLimiting:OtpRequest:*`; `Retry-After` header on every 429; `UseForwardedHeaders` with config-driven `ForwardedHeaders:KnownNetworks` CIDR trust (untrusted X-Forwarded-For can no longer rotate limiter partitions); `TestController` gated to Development (404 otherwise); the 4 previously-skipped Module 3 tests rewritten against real routes/behavior and UNSKIPPED, with a loopback RemoteIp startup filter so TestServer exercises the trust path.
- IN FLIGHT (workflows): Module 11 dispatch pipeline (channel providers, DispatchNotificationCommand, sweep job); Module 5 tenant-person CRUD (commands/queries/repository/controller — Tenant.Create had ZERO production callers). Integration build deferred until both land.

Three agent waves in flight (none run builds; orchestrator integrates + verifies after):
- DONE Wave 1 (Financials): exception sweep of 13 handlers + 3 PageSize validators + explicit 3-dp rounding (agent-verified 0W/0E, 84/84); new commands RecordManualRentPayment / IssueRentPaymentReceipt / CancelRentPayment + validators + 26 unit tests (agent-verified green).
- RUNNING Sweep (Modules 8/10/11): systemic fixes — 24+ command records are plain IRequest so TransactionBehavior NEVER opened transactions/saved changes for Maintenance, Documents, Files, Notifications (nothing persisted!); converting to ICommand/ICommand<T>, exception-model migration, ID-generation/return fixes with the navigation-discovery Added-vs-Modified rules, missing validators, first Notifications tests.
- RUNNING Wave 2 (Financials): 3 background jobs (installment generation, overdue marking, eFAWATEERcom expiry — multi-tenant sweep pattern) + MarkRentPaymentOverdue command + repo candidate queries; API controllers for Modules 6–7 (RentPayments/Allocations/Cheques/Expenses/Efawateercom+HMAC-webhook-fail-closed/ReceiptSequence) + FinancialsPermissions.

## Integration-pass TODO (orchestrator, after waves land)
1. Build; fix cross-agent compile issues (expected: AllocationSettlement signature).
2. **Doc-conformance refit of DeriveStatus per §6.1 grace matrix**: add graceDays param (company_settings.rent_grace_period_days via ICompanyRepository.GetSettingsByIdAsync); zero-paid past grace → OverdueUnpaid (not Late); partial past grace → Late (not PartiallyPaid); null due_date never late/overdue; Cancelled is sticky (recompute sites must skip Cancelled rows); widen overdue-candidate query to Pending|PartiallyPaid; update affected unit tests.
3. Wire Program.cs (4 Financials policies, 3 recurring jobs, webhook config) + DependencyInjection.cs (job registrations) from agents' wiringNeeded snippets.
4. Full unit + integration suites; update phase docs 04/05.
5. Then: Module 11 RLS migration (two-predicate policy per doc §271-275), NotificationsHub + JWT access_token query-string auth, file-storage security fixes (path traversal, tenant key prefix, HMAC-signed URLs, real upload/download endpoints), Modules 8/10/11 API controllers.

## Known Issues
- `Jwt:Secret` hardcoded fallback in Program.cs (Phase 12: replace with fail-fast on missing config outside Development).
- Real secrets present in working-tree `appsettings.Local.json` — never commit; rotate at deployment time.
- eFAWATEERcom gateway is a Null implementation (by design until credentials exist); callback idempotency to be audited in Phase 05.
- Marketplace `CreateMarketplaceListingCommandHandler` still throws `KeyNotFoundException` (500) for missing apartment — fix in Phase 07/11 exception sweep.
- Queries run outside transactions → RLS context absent; per-query repository filtering must be confirmed in Phase 12 (tenant-isolation review).
- Repo hygiene: stale `test_output*.txt`, `migration*.sql`, `ef_sql.log`, `verify_*.sql` at root (cleanup at a commit checkpoint — user decides deletion).

## Architectural Decisions
- (New) Leasing aggregate IDs are client-generated UUIDv7 via `Guid.CreateVersion7()` — matches Marketplace; DB default `uuid_generate_v7()` stays as fallback. Rationale: TransactionBehavior owns SaveChanges, so returning IDs and writing child FKs pre-save requires app-side generation.
- (New) Leasing business failures use the established exception model: NotFound→404, BusinessRuleException(code)→422, Conflict→409; domain keeps `InvalidOperationException` as defense-in-depth behind handler pre-checks.
- (Standing) RLS ordering contract, Infrastructure-only tenant switching, TransactionBehavior ownership, Jordan business clock — see `docs/backend-progress/03-leasing.md`.

## Database / Migration State
- Local dev DB: `Host=localhost; Database=AqariOS; Username=postgres` (password never stored). **Claude never applies migrations to it.**
- **PENDING USER APPLY:** `20260726040000_FixCompanyRlsPlatformAdminBypass` (companies platform-admin SELECT policy; sweep company enumeration returns 0 rows without it). Command: `dotnet ef database update --project src/PropertyOS.Infrastructure --startup-project src/PropertyOS.Api`.
- Integration tests migrate their own Testcontainers DB automatically — they do not touch the local AqariOS DB.

## Tests Added
- This session: `Handle_DeductionWithoutReason_ThrowsBusinessRuleException` (Terminate); create-ID assertions; exception-type updates across 6 leasing handler test files + `CommandPipelineTransactionTests`.
- Suite state: **unit 507/507 PASS, 0 warnings**. Integration: running.

## Manual Verification Required
- None blocking development. For the local runtime DB only: apply the pending RLS migration (command above) whenever you next run the API against localhost.

## Files Modified In Current Phase
See `git status`: prior Module 5 increment (~50 files) plus this session: `CLAUDE.md`, `Program.cs`, `LeaseContract.cs`, 6 leasing handlers + 2 commands, `LeaseContractsController.cs`, `RentPaymentRepository.cs`, `PostgresTestFixture.cs`, 8 test files, `BACKEND_PROGRESS.md`, `docs/backend-progress/{00,03}*.md`. Nothing committed (policy: user commits).

## Blockers
None.

## NEXT ACTION
Begin Module 6 — Rent Payments & Cheques final repository audit and API completion planning.
Superseded plan:
Phases 11–14 final passes: (1) API-completeness sweep of the remaining pre-existing surfaces (Companies/Subscriptions/Identity/User — verify every implemented use case is exposed and consistent, pagination/DTO checks); (2) consolidate `12-security-hardening.md` from the work already done + remaining items (production runtime role must NOT be superuser or RLS is inert — deployment doc; queries-without-transaction reality; view_all RLS bypass; Hangfire dashboard auth; magic-byte gaps; secret rotation guidance); (3) performance pass (13) — targeted: verify no unbounded queries remain, review hot paths; (4) `14-production-readiness.md` with per-category evidence; refresh CLAUDE.md's module-status line (6–8/10/11 now have APIs). Working tree is READY FOR USER COMMIT at every green checkpoint.
Superseded plan:
Execute Phases 06–09 in this order: (1) generate the Module 11 RLS migration (two-predicate recipient policy per doc §271–275 + company policy on templates — NEVER apply to local DB); (2) file-storage security: canonicalized path containment in PhysicalFileStorageProvider, companyId prefix enforcement on client-supplied StorageKeys, HMAC-signed download/upload tokens, real `/api/v1/files/upload` + `/download` endpoints, size verification on confirm; (3) API controllers + permissions for Maintenance/Documents/Notifications; (4) NotificationsHub + JWT access_token query-string auth + hub mapping. Verify with full suites after each block. READY FOR USER COMMIT already applies to the current tree (Phases 03–05 + systemic sweeps, all green) — see git status.

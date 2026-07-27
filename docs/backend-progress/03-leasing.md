# Phase 03 — Leasing (Module 5)

## Initial State
Large uncommitted increment existed: draft editing (`UpdateDraftLeaseContract`), expiration (`ExpireLeaseContract` + multi-tenant Hangfire sweep), controllers, renewal guards, status history, cross-tenant defenses, RLS migration `20260726040000_FixCompanyRlsPlatformAdminBypass`. Build green, unit tests 506/506, but: create endpoint broken (returned MediatR `Unit` to `CreatedAtAction` → 500 on success), raw BCL exceptions surfacing as 500s, sweep job never scheduled, `PropertyPermissions.Delete` policy registration lost.

## Requirements Reviewed
`docs/PropertyOS_Module5_Leasing_FINAL.md` (§5.0 draft editability, §5.1 lease_contracts, §5.2 terminations, §5.3 status history, §5.6 security/RLS); Architecture doc transaction/exception conventions; Marketplace module as the precedent for ID generation and exception style.

## Problems Found
1. `CreateLeaseContractCommand : ICommand` returned `Unit`; contract IDs were DB-generated (`Id = Guid.Empty` pre-save), so the API could not return the created ID, and `CreatedAtAction(..., Unit)` would throw at Location-generation → 500 after successful create.
2. `ContractStatusHistory` rows were created with `LeaseContractId = Guid.Empty` pre-save, relying on fragile EF key-propagation behavior.
3. Handlers threw `KeyNotFoundException`/`InvalidOperationException` for business failures → HTTP 500 via `GlobalExceptionHandler` (unmapped) instead of 404/422/409.
4. Expiration sweep job registered in DI but no `RecurringJob` schedule existed.
5. `PropertyPermissions.Delete` policy registration had been accidentally replaced by the Leasing policies in `Program.cs`.
6. Terminate deposit-deduction-reason rule had no unit test.

## Implementation Completed
- `LeaseContract.Create` now assigns `Guid.CreateVersion7()` client-side (same pattern as `MarketplaceListing`); column default `uuid_generate_v7()` retained as fallback. Status-history FKs are now real values pre-save.
- `CreateLeaseContractCommand` and `RenewLeaseContractCommand` are `ICommand<Guid>`; handlers return the new contract ID; controller returns `201 CreatedAtAction` for both create and renew.
- Exception model migration across all six command handlers (Create, UpdateDraft, Activate, Terminate, Renew, Expire): missing/cross-tenant → `NotFoundException` (404); status-transition and business rules → `BusinessRuleException` with machine codes (`LEASE_ACTIVATE_INVALID_STATUS`, `LEASE_TERMINATE_DATE_IN_FUTURE`, `LEASE_EDIT_NOT_DRAFT`, `LEASE_EXPIRE_TERM_NOT_ENDED`, …) (422); overlap/active-exists/successor-exists → `ConflictException` (409). Domain invariants keep throwing `InvalidOperationException` as defense-in-depth behind handler pre-checks.
- Restored `PropertyPermissions.Delete` policy; added Leasing Create/Approve policies (from prior session).
- Scheduled `ExpireLeaseContractsJob` via `IRecurringJobManager` — daily at 00:15 Asia/Amman, only when Hangfire storage is configured.
- Removed two comment-only tombstone files left from relocating the job/interface to Infrastructure.

## Files / Areas Changed
Domain: `LeaseContract.cs`. Application: 6 leasing command handlers + 2 command records. Api: `LeaseContractsController.cs`, `Program.cs`. Tests: 7 leasing handler test files (exception types + return values + new deduction-reason test), `CommandPipelineTransactionTests.cs` (NotFoundException expectation).

## Database Impact
No schema change. Pending apply (user-run): migration `20260726040000_FixCompanyRlsPlatformAdminBypass` (adds `companies_platform_admin_select_policy` so the sweep can enumerate companies under RLS). Integration tests apply it automatically inside Testcontainers.

## Security Review
CompanyId never accepted from requests; `ITenantContext` + RLS remain the boundary; cross-tenant reads masked as 404 (no existence oracle). Sweep uses `app.is_platform_admin` transaction-locally, never BYPASSRLS. `ISystemTenantContextSetter` remains Infrastructure-internal (handlers cannot switch tenants).

## Multi-Tenancy / RLS Review
Tenant scope is set before `BeginTransactionAsync` in every sweep scope (interceptor contract). Per-company and per-contract DI scopes prevent context leakage. RLS integration suites: `Rls/*`, `LeaseContractRlsTests`, `LeaseExpirationIntegrationTests`.

## Transaction Review
Handlers never call `SaveChangesAsync`; `TransactionBehavior` owns commit for `ICommand`/`ICommand<T>` (the `ICommand<Guid>` change keeps the transaction — behavior matches on `ICommand<TResponse>`). Multi-row operations (contract + history (+ termination); predecessor supersede + successor activate) remain in one transaction.

## Performance Review
Sweep batches (100/batch), point-reads per contract, poison threshold caps NOT-IN growth. UpdateDraft skips reference lookups when apartment/tenant/dates unchanged (covered by dedicated tests). No N+1 introduced.

## Tests Added / Updated
Updated: exception-type expectations across 6 handler test files; create test asserts returned ID equals stored ID and is non-empty. Added: `Handle_DeductionWithoutReason_ThrowsBusinessRuleException`. Full unit suite: **507/507 passing, 0 build warnings**.

## Verification Results
- `dotnet build PropertyOS.sln`: 0 errors, **0 warnings**.
- `dotnet test tests/PropertyOS.Tests.Unit`: **507/507 passed**.
- `dotnet test tests/PropertyOS.Tests.Integration`: **105 passed, 0 failed, 4 skipped** (36s; Testcontainers postgres:17). The 4 skips are pre-existing Module 3 placeholders (`Skip = "Endpoints not implemented yet"`, `Module3SecurityIntegrationTests`) — tracked for Phases 11/12. The historical `tenants.tenant_type` schema-drift failures no longer reproduce.

## Remaining Issues
- Tenant account/invitation flow (link Module 5 tenants to Module 3 user accounts) — intentional deferral, tracked for a later phase.
- Tenant CRUD API surface (create/update tenant persons) — to be verified in Phase 11 API completeness.
- `renewed` status reporting semantics (doc §5.1) are application-layer only; no dedicated reporting yet (Reports phase).

## Phase Result
COMPLETE — full build clean, unit suite 507/507, integration suite 105/105 (4 pre-existing unrelated skips).

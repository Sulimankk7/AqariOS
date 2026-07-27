# Phase 00 — Current State Audit

Audited 2026-07-26 against the working tree (branch `main`, last commit `e036095`), the approved module documents in `docs/`, and live build/test runs.

## Initial State
- Clean Architecture solution (.NET 9, PostgreSQL/Npgsql, MediatR CQRS, FluentValidation, Mapster, Hangfire, Testcontainers integration tests).
- Modules 1–5 committed through `e036095` plus a large uncommitted Module 5 increment (lifecycle completion, expiration sweep, API surface).
- Verified at audit time: `dotnet build` 0 errors; unit tests 506/506 passing (before this mission's changes).

## Requirements Reviewed
`docs/PropertyOS_Backend_Architecture.md` (de-facto spec), module docs 4–11, `docs/PropertyOS_Database_Design.md`, `docs/architecture/ProvisioningContract.md`.

## Problems Found
1. `Program.cs` had lost the `PropertyPermissions.Delete` policy registration (4 delete endpoints would throw "policy not found") — restored.
2. `POST /leasing/contracts` returned `CreatedAtAction` with MediatR `Unit` as route id → guaranteed 500 on success — fixed (see Phase 03).
3. `ExpireLeaseContractsJob` was DI-registered but never scheduled — recurring job registration added (daily 00:15 Asia/Amman).
4. Leasing handlers threw raw BCL exceptions (`KeyNotFoundException`, `InvalidOperationException`) → HTTP 500 instead of 404/409/422 — fixed in Phase 03.
5. Known warnings: CS8604 (ILike null), xUnit1012 (null theory param), CS0618 (Testcontainers obsolete ctor) — all fixed (0 warnings now).
6. `Jwt:Secret` falls back to a hardcoded literal — open, scheduled for Phase 12 security hardening.
7. Real secrets in working-tree `appsettings.Local.json` (gitignored but present) — treat as compromised; never commit.
8. Modules 6, 7, 8, 10, 11 have no API controllers despite complete Domain/Application/Infrastructure layers.
9. Reports: not started. Files: local-disk placeholder storage provider.
10. Historical integration failures (schema drift `tenants.tenant_type`) recorded in old `test_output*.txt` — must re-verify against current migrations.

## Module Status (evidence-based)
| Module | Domain | App | Infra | API | Verdict |
|---|---|---|---|---|---|
| 1 Companies | ✅ | ✅ | ✅ | ✅ | COMPLETE |
| 2 Subscriptions | ✅ | ✅ | ✅ | ✅ | COMPLETE |
| 3 Identity/RBAC/Audit | ✅ | ✅ | ✅ | ✅ | COMPLETE (hardening items open) |
| 4 Properties | ✅ | ✅ | ✅ | ✅ | COMPLETE (policy regression fixed) |
| 5 Leasing | ✅ | ✅ | ✅ | ✅ | Phase 03 in progress |
| 6 Rent Payments | ✅ | ✅ | ✅ | ❌ | PARTIAL — no API |
| 7 Financial Ops | ✅ | ✅ | ✅ | ❌ | PARTIAL — no API, null eFAWATEERcom gateway |
| 8 Maintenance | ✅ | ✅ | ✅ | ❌ | PARTIAL — no API, undispatched domain events |
| 9 Marketplace | ✅ | ✅ | ✅ | ✅ | COMPLETE |
| 10 Documents | ✅ | ✅ | ✅ | ❌ | PARTIAL — no API |
| 11 Notifications | ✅ | ✅ | ✅ | ❌ | PARTIAL — no API, SignalR hub unmapped |

## Phase Result
COMPLETE (audit itself). Findings feed Phases 03–14.

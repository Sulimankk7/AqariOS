# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Session protocol (mandatory)

- At the start of every session, read this file and `BACKEND_PROGRESS.md` (repo root) before working. `BACKEND_PROGRESS.md` is the authoritative continuation checkpoint between sessions; `docs/backend-progress/` holds per-phase reports.
- Then inspect `git status` / `git diff` and the relevant source files. Continue from `BACKEND_PROGRESS.md` → **NEXT ACTION** after verifying it against the actual repository state. Work autonomously; do not repeatedly ask permission for normal development work.
- Never restart completed work. Never rely solely on previous conversation context.

### Execution policy (authoritative, supersedes earlier command restrictions)

- Claude **MAY run**: `dotnet restore`, `dotnet build`, `dotnet test` (targeted or full, unit and integration), and `dotnet format` verification. Integration tests use Testcontainers; Docker must be running — invoking Docker indirectly through the existing test infrastructure is allowed.
- Claude **MUST NEVER** run `git commit`, `git push`, `git rebase`, `git merge`, `git reset --hard`, `git clean`, `git checkout -- .`, `git restore .`, or any command that discards or publishes work. The user controls Git history. Read-only git commands (`status`, `diff`, `log`, `show`, `branch`) are always allowed. When a coherent change set is ready, report **READY FOR USER COMMIT** with a summary — never commit automatically, even when everything passes.
- Claude **MUST NOT** apply migrations to the user's local AqariOS database: no `dotnet ef database update`, no dropping/recreating databases, no destructive SQL, no resetting migration history. Generating a migration is allowed only when the implementation genuinely requires one — inspect the generated migration carefully before considering it valid. When actual DB application is required, document the exact pending operation in `BACKEND_PROGRESS.md` and continue with everything that does not depend on it.
- Report only actual results: never claim a build/test passed without having run it (or having been given the output).
- Keep `BACKEND_PROGRESS.md` updated after every meaningful implementation unit, before/after verification runs, before switching modules, and whenever a blocker is discovered. Keep it a concise checkpoint, not an activity log.
- The authoritative local development database identity is `Host=localhost; Database=AqariOS; Username=postgres`. Never store or expose its password.
- The old **PropertyOS** naming in namespaces/projects/docs is historical; do NOT perform a repository-wide rename unless explicitly requested. The product is **AqariOS**.
- Preserve the established Clean Architecture, multi-tenancy, RLS, RBAC, audit, and transaction architecture unless a demonstrated defect requires correction.

## Commands

```powershell
dotnet build PropertyOS.sln

# Unit tests (fast, no external dependencies)
dotnet test tests/PropertyOS.Tests.Unit

# Integration tests — require Docker running (Testcontainers spins up postgres:17)
dotnet test tests/PropertyOS.Tests.Integration

# Single test / class
dotnet test tests/PropertyOS.Tests.Unit --filter "FullyQualifiedName~LeaseContractTests"
dotnet test tests/PropertyOS.Tests.Unit --filter "Handle_MissingApartment_ThrowsKeyNotFoundException"

# Run the API (needs a reachable PostgreSQL — startup seeds the permission catalog and fails without a DB)
dotnet run --project src/PropertyOS.Api

# EF Core migrations (named Module<N>_<Description>)
dotnet ef migrations add Module<N>_<Description> --project src/PropertyOS.Infrastructure --startup-project src/PropertyOS.Api
./scripts/database/apply-migrations.ps1
```

Configuration comes from `src/PropertyOS.Api/appsettings.Local.json` (explicitly loaded by Program.cs, gitignored). Required: `ConnectionStrings:DefaultConnection` (PostgreSQL), `Jwt:Secret` (≥32 bytes; startup fails fast when missing, and non-Development rejects the committed placeholder). Fail-closed feature secrets (each feature returns 503/throws until configured, by design — never add fallbacks): `FileStorage:UrlSigningSecret` (≥32 bytes, signs file upload/download URLs), `Efawateercom:WebhookSecret` (HMAC for the payment webhook). Optional: `RateLimiting:{Global,Login,OtpRequest}:*`, `ForwardedHeaders:KnownNetworks` (CIDR proxy trust — empty = trust no proxy), `Financials:Efawateercom:StaleAfterMinutes`.

## Architecture

Clean Architecture, .NET 9, PostgreSQL only (Npgsql). One-directional references:
`Domain` (no packages, no references — enforced by `tests/PropertyOS.Tests.Unit/Architecture/DependencyRuleTests.cs`) ← `Application` (MediatR, FluentValidation, Mapster) ← `Infrastructure` ← `Api`.

`docs/PropertyOS_Backend_Architecture.md` is the de-facto spec; code comments cite it by section ("Architecture §7"). Each business module (1 Companies, 2 Subscriptions, 3 Identity/RBAC/Audit, 4 Properties, 5 Leasing, 6–7 Financials, 8 Maintenance, 9 Marketplace, 10 Documents, 11 Notifications) has a design doc in `docs/` and is spread across all four layers under the same folder name (Modules 6–7 are merged as `Financials`). All eleven modules have full vertical slices including API controllers; cross-cutting Files (signed two-phase upload/download) and the SignalR `NotificationsHub` are live. **Reports is intentionally unbuilt** — its physical design was never specified in any approved doc (see `docs/backend-progress/10-files-reports.md`); do not invent it.

### CQRS / Application layer

- Commands implement `ICommand<TResponse>` (`Application/Common/Interfaces/ICommand.cs`); queries are plain `IRequest<T>`. This marker matters: `TransactionBehavior` (Infrastructure/Persistence/Behaviors) opens a transaction **only for commands** and owns the single `SaveChangesAsync` call. **Handlers must never call SaveChanges** — they only add/mutate entities.
- Folder convention per use case: `Application/<Module>/Commands/<VerbNoun>/` containing `<VerbNoun>Command.cs` (a record), `...CommandHandler.cs`, `...CommandValidator.cs`. Same shape for `Queries/`. Shared DTOs in `Queries/Common/`.
- Repository interfaces live at the Application module root (`Application/Leasing/ILeaseContractRepository.cs`); implementations in `Infrastructure/<Module>/Repositories/`. Read-path repository methods return Application DTOs directly (projection happens in Infrastructure, not in handlers).
- No Result<T> pattern — errors are exceptions. Use `NotFoundException`, `ConflictException`, `BusinessRuleException` (carries a `Code`) from `Application/Common/Exceptions/`; `Api/Middleware/GlobalExceptionHandler.cs` maps them to 404/409/422 ProblemDetails. Raw `KeyNotFoundException`/`InvalidOperationException` surface as 500 — some older leasing handlers do this; don't copy that pattern.

### Domain layer

Deliberately minimal — no base entity/aggregate/domain-event framework. Aggregates are POCOs with `private set` properties, a private parameterless ctor for EF, a static `Create(...)` factory, and behavior methods that enforce state transitions by throwing (see `Domain/Leasing/LeaseContract.cs`). Conventions:

- Time and actor are injected: mutators take `DateTimeOffset updatedAt, Guid? updatedBy` parameters. The domain never calls `DateTimeOffset.UtcNow`.
- `public uint xmin { get; private set; }` is the PostgreSQL concurrency token — never remove or assign it.
- `Guid Id` is database-generated: it is `Guid.Empty` until SaveChanges, so relationships created pre-save rely on EF fixup.
- Domain is Jordan-specific (Governorate enum, `JordanBusinessClock`, JOD currency, eFAWATEERcom).

### Multi-tenancy (the most important invariant)

CompanyId is **never** a route or body parameter. Handlers read `ITenantContext.CompanyId` (populated from JWT claims). The real tenant boundary is PostgreSQL **Row-Level Security**, not EF query filters (there are none, not even for soft delete):

- `TenantSessionInterceptor` issues transaction-local `set_config('app.current_company_id', ...)` when a transaction starts. Session-scoped `SET` is forbidden (connection-pool leak).
- Because only commands get transactions, plain queries may run without RLS tenant context — repository-level filtering matters there; verify per query.
- Fail-closed: null CompanyId means RLS rejects everything. Never treat null as "all tenants". Platform admin uses `app.is_platform_admin`, never BYPASSRLS.

### Adding a PostgreSQL enum touches 4 files

Npgsql enum mapping is declared in: `Infrastructure/DependencyInjection.cs` (twice — `NpgsqlDataSourceBuilder` and `npgsqlOptions.MapEnum`), `PropertyOsDbContext.OnModelCreating` (`HasPostgresEnum`), and `tests/PropertyOS.Tests.Integration/Infrastructure/PostgresTestFixture.cs`. Missing one produces confusing runtime mapping errors.

### API layer

Controllers (not minimal APIs), routes declared as absolute per-action templates with API versioning: `[HttpPost("api/v{version:apiVersion}/leasing/contracts")]`. Permission-based authorization policies are registered manually in `Program.cs` (claim type `"permissions"`, constants in `Application/<Module>/Security/*Permissions.cs`). Hangfire (same Postgres DB) runs background jobs; job definitions in `Application/<Module>/Jobs/`, implementations in `Infrastructure/<Module>/Jobs/`.

## Testing

- xUnit + **NSubstitute** (not Moq) + FluentAssertions. `Xunit` is a global using. Naming: `Method_Scenario_ExpectedResult`; test classes mirror the source folder structure.
- Integration tests use Testcontainers (postgres:17) + Respawn, shared via `[Collection("Postgres collection")]`. `PostgresTestFixture` exposes two contexts: `Context` (superuser — RLS effectively bypassed) and `AppUserContext` (non-superuser — RLS enforced). Tenant-isolation tests must use `AppUserContext` or they silently prove nothing. RLS suites live in `tests/PropertyOS.Tests.Integration/Rls/`.

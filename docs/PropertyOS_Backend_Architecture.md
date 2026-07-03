# PropertyOS — Backend Architecture

**Status:** Final architecture for implementation. Database design (11 modules, 47 tables, `PropertyOS_Database_Design.md` + `PropertyOS_Phase3_Physical_Design.md` + Modules 1–11 + `PropertyOS_Architecture_Review.md` + `PropertyOS_Business_Logic_Review.md`) is treated as immutable source of truth.
**Stack:** ASP.NET Core 9 / .NET 9, EF Core + Npgsql, PostgreSQL 17, Clean Architecture, JWT + HttpOnly refresh cookies, RBAC, FluentValidation, Serilog, Sentry, Swagger/Scalar, Hangfire, SignalR, xUnit, REST, React 19 frontend (out of scope here).

---

## 0. How the Database Docs Map to This Architecture

Every module below is taken directly from the approved physical design. No table, column, or business rule is reinterpreted; this document only decides *how C# code is organized around* that schema.

| DB Module | Tables | Backend Feature Folder |
|---|---|---|
| 1 — Core | `companies`, `company_settings` | `Companies` |
| 2 — SaaS | `subscription_plans`, `company_subscriptions` | `Subscriptions` |
| 3 — Security | `users`, `user_company_roles`, `roles`, `permissions`, `role_permissions`, `refresh_tokens`, `login_history`, `audit_logs` | `Identity`, `Audit` |
| 4 — Properties | `buildings`, `building_addresses`, `floors`, `apartments`, `parking_spots`, `parking_assignments`, `utility_meters`, `meter_readings` | `Properties` |
| 5 — Leasing | `tenants`, `tenant_family_members`, `tenant_emergency_contacts`, `tenant_vehicles`, `lease_contracts`, `contract_terminations`, `contract_status_history`, `contract_documents` | `Leasing` |
| 6 — Rent Payments & Cheques | `rent_payments`, `cheque_details`, `payment_allocations` | `Payments` |
| 7 — Financial Operations | `expenses`, `expense_receipts`, `rent_payment_receipts`, `company_receipt_sequences`, `efawateercom_transactions` | `Financials` |
| 8 — Maintenance | `maintenance_requests`, `maintenance_request_attachments`, `maintenance_request_comments`, `maintenance_status_history` | `Maintenance` |
| 9 — Marketplace | `marketplace_listings`, `listing_images`, `viewing_requests` | `Marketplace` |
| 10 — Documents | `document_categories`, `building_documents` | `Documents` |
| 11 — Notifications | `notification_templates`, `notifications`, `notification_deliveries` | `Notifications` |
| Cross-cutting | `file_storage`, `report_definitions`, `report_snapshots` | `Files`, `Reports` |

`tenants` and `file_storage` are referenced throughout the DB docs as already-established FK targets but were never given full column-level specs in the uploaded material (flagged as Documentation Gaps in `PropertyOS_Architecture_Review.md` §2.1/§2.2). This architecture treats them as **existing modules whose EF configuration will be completed from the conceptual description (Phase 1 §1.9–§1.12, §1.32) at implementation time**, following the identical patterns already used for every other table — it does not invent new business fields, only the standard audit/soft-delete/RLS columns and the FKs already implied by every table that references them.

---

## 1. Solution Overview — Clean Architecture

Four production projects, two test projects, strict inward dependency direction:

```
PropertyOS.Api            → PropertyOS.Application, PropertyOS.Infrastructure
PropertyOS.Infrastructure  → PropertyOS.Application, PropertyOS.Domain
PropertyOS.Application     → PropertyOS.Domain
PropertyOS.Domain           → (nothing)
```

`Domain` never references EF Core, ASP.NET Core, Npgsql, Hangfire, SignalR, Serilog, or Sentry — it is pure C#. This is enforced structurally (see §22) with `Microsoft.CodeAnalysis.PublicApiAnalyzers`-style project-reference assertions and an architecture test in `PropertyOS.Tests.Unit` that fails the build if `Domain.csproj` gains a forbidden `PackageReference`.

### 1.1 PropertyOS.Domain

Responsibility: the pure business model — entities, value objects, domain enums, domain events, and domain-level invariants that are true regardless of persistence technology (e.g., "a superseded lease contract's financial terms are immutable," "an allocation cannot exceed the obligation's amount due" as a guard clause inside the aggregate, not just a DB trigger). Contains:
- Entity classes for all 47+ tables, one class per table, matching column-for-column what the DB docs specify (types, nullability, defaults represented as C# defaults).
- Value objects for repeating shapes: `Money` (amount + `CHAR(3)` currency, wraps every `NUMERIC(12,3)`/`currency` pair — one type, used everywhere the DB pairs them), `Address` (governorate/district/area), `DateRange` (start/end with the `end > start` invariant baked in as a constructor guard, mirroring `chk_lease_contracts_dates` etc.).
- Domain enums mirroring every PostgreSQL enum 1:1 (`ContractStatus`, `DueDateStatus`, `ChequeStatus`, `ListingStatus`, …) — the DB enum is the source of truth; the C# enum is a direct mechanical translation, never independently extended.
- Domain events (POCO records, not MediatR-coupled) raised by aggregates: `LeaseContractActivated`, `PaymentAllocationReversed`, `MarketplaceListingPublished`, etc. — consumed later by Application-layer handlers.
- No repository interfaces here (see §5) — Domain does not know persistence exists at all, not even as an abstraction, matching the DB docs' own posture that business rules and storage strategy are separate concerns (Phase 1 §1.20's receipts design being the canonical example of business shape driving storage shape, not the reverse).

### 1.2 PropertyOS.Application

Responsibility: use cases (CQRS commands/queries, see §4), DTOs, FluentValidation validators, MediatR handlers, the `ITenantContext`, `ICurrentUserContext`, repository/unit-of-work **interfaces** (implemented in Infrastructure), and orchestration of the multi-table transactions the DB docs call out as "application-orchestrated" (lease renewal, lease termination, cheque bounce reversal, payment allocation, receipt issuance, marketplace listing reconciliation). Application depends only on Domain. It defines the *shape* of persistence it needs (interfaces) without knowing EF Core exists.

### 1.3 PropertyOS.Infrastructure

Responsibility: EF Core `DbContext`, entity configurations (`IEntityTypeConfiguration<T>` per table), migrations, repository implementations, the Npgsql provider, PostgreSQL RLS session-variable plumbing, Hangfire job implementations, SignalR hub implementations, `IFileStorageService` implementation (S3-compatible), the eFAWATEERcom gateway client, Serilog sinks, and every other concrete technology adapter. Depends on Application + Domain.

### 1.4 PropertyOS.Api

Responsibility: ASP.NET Core host — controllers (thin), middleware pipeline, authentication/authorization wiring, Swagger/Scalar, global exception handling, request/response DTOs at the transport boundary (distinct from Application DTOs — see §16), API versioning, health checks, SignalR hub routes, Hangfire dashboard. Depends on Application + Infrastructure (composition root — this is the only project allowed to reference Infrastructure directly, per standard Clean Architecture; `Program.cs` performs DI registration for all four projects).

### 1.5 PropertyOS.Tests.Unit

Tests Domain and Application in isolation — no database, no HTTP. Mocks repository interfaces. Includes the architecture-boundary tests described in §22.

### 1.6 PropertyOS.Tests.Integration

Tests Infrastructure and Api against a real PostgreSQL 17 instance via Testcontainers (§19) — migrations, RLS policies, triggers, concurrency behavior, and full HTTP request/response cycles.

---

## 2. Feature / Module Organization

Each backend module folder appears **once** in each project, named identically to the table in §0, never collapsed into one global `Services`/`Controllers`/`DTOs` folder. Example for `Leasing`:

```
PropertyOS.Domain/
  Leasing/
    Tenant.cs, TenantFamilyMember.cs, TenantEmergencyContact.cs, TenantVehicle.cs
    LeaseContract.cs, ContractTermination.cs, ContractStatusHistory.cs, ContractDocument.cs
    Enums/ ContractStatus.cs, LegalRegime.cs, TenantType.cs, PaymentFrequency.cs
    Events/ LeaseContractRenewed.cs, LeaseContractTerminated.cs, LeaseContractActivated.cs

PropertyOS.Application/
  Leasing/
    Commands/
      CreateLeaseContract/ CreateLeaseContractCommand.cs, Handler.cs, Validator.cs
      RenewLeaseContract/  ...
      TerminateLeaseContract/ ...
    Queries/
      GetLeaseContractById/ ...
      GetLeaseContractsByApartment/ ...
      SearchLeaseContracts/ ...
    Dtos/ LeaseContractDto.cs, ContractTerminationDto.cs, ...
    Mapping/ LeasingMappingProfile.cs
    Validators/ (shared cross-command validators, e.g., overlap-check)

PropertyOS.Infrastructure/
  Leasing/
    Configurations/ LeaseContractConfiguration.cs, ContractTerminationConfiguration.cs, ...
    Repositories/ LeaseContractRepository.cs
    Services/ LeaseRenewalOrchestrator.cs (the multi-table transaction, §12)

PropertyOS.Api/
  Leasing/
    Controllers/ LeaseContractsController.cs, ContractTerminationsController.cs
    Contracts/ (transport request/response records)
```

Every one of the 11 DB modules follows this identical four-project mirrored-folder shape. `Identity`/`Audit` (Module 3) and `Files` (`file_storage`) are treated as shared/platform folders since they're consumed by every business module, but still get their own folder, never merged into a catch-all.

---

## 3. Application-Layer Pattern — Decision: CQRS via MediatR, Vertical-Slice Inside Application

**Decision:** MediatR-based CQRS, organized as vertical slices (one folder per use case containing its Command/Query + Handler + Validator co-located), not a classic layered "Services/Repositories/DTOs" split, and not a full separate-assembly CQRS (single `Application` project, internal folder-level separation).

**Why this fits PropertyOS specifically:**
- The DB design is explicit that reads and writes have fundamentally different shapes on the highest-volume tables. `rent_payments`/`payment_allocations` (Module 6) is the clearest case: writing a payment allocation runs through a `BEFORE INSERT` over-allocation trigger and touches 2–3 tables in one transaction, while reading "outstanding balances" is a single indexed aggregate query against `idx_rent_payments_company_status_due_date`. Forcing both through one generic `PaymentService.GetOrUpdate(...)` would blur exactly the distinction the schema itself is built around. CQRS makes that split explicit in code.
- **Leasing:** commands (`CreateLeaseContractCommand`, `RenewLeaseContractCommand`, `TerminateLeaseContractCommand`) each map 1:1 to a documented multi-table application-orchestrated transaction (§5.1/§5.0 of Module 5) — a natural MediatR handler boundary. Queries (`GetLeaseHistoryForApartmentQuery`, `GetLeaseHistoryForTenantQuery`) map 1:1 to the named indexes (`idx_lease_contracts_apartment_history`, `idx_lease_contracts_tenant_history`).
- **Payments:** `RecordPaymentAllocationCommand` is the single write path funneling every settlement (simple or complex) through the allocation mechanism (Module 6 §6.0) — CQRS keeps this one command instead of scattering "pay rent" logic across multiple service methods. `GetOutstandingBalancesQuery`/`GetLatePaymentsQuery` map directly to `idx_rent_payments_late_overdue`.
- **Financial Operations:** `IssueRentPaymentReceiptCommand`/`IssueExpenseReceiptCommand` each wrap the atomic `company_receipt_sequences` increment (§7.4) as a single handler — CQRS's "one command, one transaction" model is exactly the shape that mechanism requires.
- **Maintenance:** command handlers for status transitions double-write `maintenance_requests.status` and `maintenance_status_history` in one handler (mirroring §8.0's "written in the same transaction" rule) — this is naturally a single MediatR command, not two separate service calls a caller could accidentally split.
- **Marketplace:** `ActivateLeaseContractCommand`'s handler is also where the Module 9 §9.0 listing-reconciliation rule lives (transition a `published` listing to `rented` in the same transaction as lease activation) — CQRS commands are the natural place to compose two modules' side effects inside one transaction without giving Marketplace a hard compile-time dependency on Leasing (handled via a MediatR-published domain event/notification instead, see §3.1).
- **Notifications:** generation is virtually always a *side effect* of another command (a lease renewed → a notification generated) — MediatR's `INotificationHandler<T>` pattern lets `Notifications` react to domain events published by other modules' commands without Leasing/Payments/Maintenance ever referencing `Notifications` directly. This keeps the module boundaries in §2 real, not just folder-deep.

**Why not a heavier pattern:** A fully separate `Application.Contracts`/`Application.Handlers` assembly split, or full Event Sourcing, would be overengineering for this system — the DB design's own philosophy throughout (table-per-type over polymorphism, application-orchestrated transactions with documented Phase 8 trigger backstops rather than a fully event-sourced audit model) is pragmatic, not maximalist, and the backend architecture matches that posture rather than exceeding it.

### 3.1 Cross-Module Composition via Domain Events

When one module's command must trigger a side effect in another module (lease activation → marketplace reconciliation; any financial event → notification), the **triggering** handler publishes a MediatR notification (`INotification`) after its own transaction commits; the **reacting** module's `INotificationHandler<T>` runs as a separate, subsequent unit of work. This mirrors the DB docs' own language precisely — e.g. Module 9 §9.0 describes lease activation and listing reconciliation as "the same application-orchestrated transaction" for the *core* DB write (both writes happen together, inside one DB transaction, orchestrated by the Leasing module's own repository calls), while any *downstream* effect that is not itself part of that DB transaction (e.g., queuing a notification) is deferred to a post-commit domain event. This distinction — same-transaction DB writes vs. post-commit side effects — is enforced explicitly in each orchestrator (§12) rather than left ambiguous.

### 3.2 Building Blocks

- **Commands:** `IRequest<TResult>` records, one per use case, named as an imperative verb phrase (`CreateLeaseContractCommand`, not `LeaseContractCommand`).
- **Queries:** `IRequest<TResult>` records, named `Get*Query`/`Search*Query`/`List*Query`.
- **Handlers:** `IRequestHandler<TCommand, TResult>`, one per command/query, injected with repository interfaces + `ITenantContext` + `IUnitOfWork`. Handlers are the only place business-transaction orchestration lives — controllers never orchestrate.
- **Validators:** FluentValidation `AbstractValidator<TCommand>`, registered via `MediatR.FluentValidation` pipeline behavior — validation runs before the handler, short-circuiting on failure, returning a structured `ValidationProblemDetails` (§16).
- **DTOs:** Application-layer DTOs are the shape returned by queries and accepted by commands' internal parameters — distinct from Api-layer request/response contracts (§16), so a transport format change never touches Application code.
- **Mapping:** Mapster (not AutoMapper — lower allocation overhead, compile-time-checkable mapping, appropriate given the performance priority in §6) configured per-module in a single `*MappingProfile.cs`.

### 3.3 Pipeline Behaviors (cross-cutting, registered once)

`ValidationBehavior` → `TenantAuthorizationBehavior` (verifies the resolved `company_id` matches the requested resource's tenant scope before the handler runs, as an application-layer belt-and-suspenders check *in addition to* RLS — see §7) → `TransactionBehavior` (wraps command handlers, not queries, in an EF Core transaction per §12's rules) → `AuditBehavior` (captures before/after state for `audit_logs`, §10) → `LoggingBehavior` (structured Serilog scope with correlation ID, §18).

---

## 4. CQRS Detail per Hot Module

| Module | Representative Commands | Representative Queries |
|---|---|---|
| Leasing | `CreateLeaseContractCommand`, `RenewLeaseContractCommand`, `ActivateLeaseContractCommand`, `TerminateLeaseContractCommand` | `GetLeaseContractByIdQuery`, `GetLeaseHistoryForApartmentQuery`, `GetLeaseHistoryForTenantQuery`, `GetExpiringLeasesQuery` |
| Payments | `GenerateScheduledInstallmentCommand` (Hangfire-invoked), `RecordPaymentAllocationCommand`, `ReverseAllocationCommand` (cheque bounce), `RecordChequeStatusChangeCommand` | `GetOutstandingBalancesQuery`, `GetLatePaymentsQuery`, `GetPaymentHistoryForContractQuery`, `GetPaymentHistoryForTenantQuery` |
| Financials | `RecordExpenseCommand`, `IssueRentPaymentReceiptCommand`, `IssueExpenseReceiptCommand`, `RecordEfawateercomCallbackCommand` | `GetExpenseHistoryQuery`, `GetFinancialReportQuery` |
| Maintenance | `CreateMaintenanceRequestCommand`, `ChangeMaintenanceStatusCommand`, `AddMaintenanceCommentCommand` | `GetOpenRequestsQuery`, `GetRequestsByApartmentQuery` |
| Marketplace | `PublishListingCommand` (vacancy-gated), `CreateViewingRequestCommand` (anonymous-allowed) | `SearchPublishedListingsQuery` (public), `GetCompanyListingsQuery` |
| Documents | `UploadBuildingDocumentCommand` | `GetExpiringDocumentsQuery` |
| Notifications | (handled via `INotificationHandler`, not directly commanded by clients except `MarkNotificationReadCommand`) | `GetUnreadNotificationsQuery`, `GetNotificationHistoryQuery` |

---

## 5. Repository Strategy — Decision: Aggregate-Specific Repositories, No Generic Repository

**Decision:** One repository interface per **aggregate root**, not per table, and not a generic `IRepository<T>`. Concrete rule: *a table gets its own repository only if it is queried/mutated independently of its parent; a table that is always accessed through a parent aggregate (e.g., `contract_status_history`, `payment_allocations`, `maintenance_status_history`, `listing_images`) is exposed only as a navigation property loaded by its parent's repository, never through its own repository.*

Concretely:
- `ILeaseContractRepository` — aggregate root `LeaseContract`, includes `ContractTermination`, `ContractStatusHistory`, `ContractDocuments` as owned navigation collections.
- `IRentPaymentRepository` — aggregate root `RentPayment`; `payment_allocations` and `cheque_details` are loaded/mutated through it, never independently, matching the DB design's own description of `payment_allocations` as "the linkage layer," not an independent business object.
- `IMaintenanceRequestRepository` — aggregate root, with attachments/comments/status-history as owned collections.
- `IMarketplaceListingRepository` — aggregate root, with `listing_images` owned.
- `INotificationRepository` — aggregate root, with `notification_deliveries` owned (§11 of Module 11 explicitly describes deliveries as "the channel-level fulfillment record behind every notifications row" — never independently queried by a client).
- Lookup/reference tables with no meaningful aggregate boundary (`subscription_plans`, `permissions`, `document_categories`, `notification_templates`) get a thin `IReadOnlyLookupRepository<T>` — the **one** place a shared generic abstraction is justified, since these are genuinely interchangeable simple CRUD-over-a-lookup-table cases with no orchestration logic to protect.

**Why not a generic `IRepository<T>`:** EF Core's `DbSet<T>` already *is* a generic repository; wrapping it in a second generic layer adds an abstraction with no behavior of its own (the textbook "repository over an ORM that's already a repository" anti-pattern) and, more importantly, would erase exactly the aggregate boundaries the DB design spent 11 modules establishing (e.g., it would make it trivially easy to write `db.Set<PaymentAllocation>().Add(...)` from outside the payment-recording orchestrator, bypassing the over-allocation trigger's *application-layer* pre-check and the `RentPayment` aggregate's own invariant). Aggregate-specific repositories are the concrete rule that prevents that: **write access to a child table is only exposed through the repository method that also enforces the DB's documented business rule for it** (e.g., `IRentPaymentRepository.AllocatePaymentAsync(...)` is the only way to create a `payment_allocations` row; there is no `IPaymentAllocationRepository`).

**Direct DbContext access:** Application code never references `PropertyOsDbContext` directly — only Infrastructure repository implementations do. Query handlers for genuinely simple, single-table, read-only lookups (e.g., `GetSubscriptionPlansQuery`) may use the `IReadOnlyLookupRepository<T>` directly with `.AsNoTracking()` rather than round-tripping through a bespoke repository interface — this is the one sanctioned "thin" path, chosen deliberately rather than the generic-repository-everywhere alternative.

**Unit of Work:** `IUnitOfWork` wraps `DbContext.SaveChangesAsync` plus explicit transaction control (`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`), injected into command handlers. Query handlers never receive `IUnitOfWork`.

---

## 6. Database Access Architecture

- **`PropertyOsDbContext` location:** `PropertyOS.Infrastructure/Persistence/PropertyOsDbContext.cs`. The `Application` layer only sees `IUnitOfWork` and repository interfaces (`PropertyOS.Application/Common/Interfaces/`) — never the concrete context type, per Clean Architecture's dependency rule.
- **EF Core configuration location:** one `IEntityTypeConfiguration<T>` class per entity, colocated under each module's `Infrastructure/<Module>/Configurations/` folder (§2) — never one giant `OnModelCreating`. `PropertyOsDbContext.OnModelCreating` only calls `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- **PostgreSQL enum mapping:** every DB enum (`contract_status_enum`, `due_date_status_enum`, `cheque_status_enum`, etc.) is mapped via `Npgsql`'s native enum support (`NpgsqlDataSourceBuilder.MapEnum<T>()` at startup, plus `modelBuilder.HasPostgresEnum<T>()` per Module 3 §3.0's confirmation that "Prisma supports PostgreSQL enums natively" — the EF Core/Npgsql equivalent is used here). C# enum member names are `PascalCase`; the Npgsql name-translator maps them to the DB's `snake_case` enum labels automatically. No enum is ever represented as a raw `string`/`int` column — this preserves the DB-level domain-integrity guarantee the schema was explicitly designed around.
- **UUIDv7 generation strategy:** generated in the **database**, not in C# — every table's `id` column keeps its PostgreSQL `DEFAULT uuid_generate_v7()` (per Module 3 §3.0's PL/pgSQL function), and EF Core entity configurations set `.HasDefaultValueSql("uuid_generate_v7()")` with `ValueGeneratedOnAdd()`. This is a deliberate choice over `Guid.CreateVersion7()` (available in .NET 9) at the application layer: keeping generation server-side means the guarantee holds identically for rows inserted outside the API (migrations, admin scripts, Hangfire jobs using raw SQL) exactly as the DB docs assume throughout ("uuid_generate_v7(), used as every table's id default"). EF Core reads the generated value back via `RETURNING id` on insert (Npgsql supports this natively).
- **Transaction strategy:** see §12 in full — `SaveChangesAsync` alone for single-aggregate command handlers; explicit `IDbContextTransaction` for every documented multi-table orchestration.
- **Optimistic concurrency:** every table gets a Postgres `xmin` system column mapped as a EF Core concurrency token (`.IsRowVersion()` via `.HasColumnName("xmin").HasColumnType("xid").IsConcurrencyToken()`) — zero schema change required (the DB docs never mention `xmin`, and none is needed; it's a Postgres built-in), giving optimistic concurrency on every entity for free without adding a `row_version` column the approved schema doesn't define. `DbUpdateConcurrencyException` is translated to a `409 Conflict` ProblemDetails response (§16).
- **Query tracking rules:** command handlers load aggregates with tracking (default) since they mutate and call `SaveChangesAsync`. **Every query handler uses `.AsNoTracking()`** — no exception — since queries never mutate; this is enforced by a Roslyn analyzer rule (`PROPOS001`) flagging any `IRequestHandler` implementing `IRequestHandler<TQuery, ...>` (query-suffixed) that doesn't call `.AsNoTracking()`.
- **Projection rules:** queries project directly to DTOs via Mapster's `ProjectToType<T>()` (translates to a SQL-level column projection, not "load full entity then map in memory") for every list/search query — this is what keeps the dashboard-style queries the DB docs optimized with narrow composite indexes (e.g., `idx_rent_payments_company_status_due_date`) from being defeated by EF Core materializing full entity graphs. Detail/single-record queries may load the full aggregate when the DTO genuinely needs the full owned-collection shape (e.g., a lease contract detail page needing its status history).
- **Pagination strategy:** **keyset (cursor) pagination**, matching the DB docs' own explicit indexing strategy — nearly every high-volume composite index in the schema (`idx_rent_payments_contract_due_date`, `idx_notifications_recipient_created`, `idx_maintenance_requests_company_status_request_date`, etc.) is documented as built specifically to support `WHERE (col1, col2) < ($1, $2) ORDER BY ... LIMIT n` keyset queries, explicitly "preferred over OFFSET-based pagination" (Module 3 §3.10 and repeated in every subsequent module). Offset pagination is **not implemented** for any high-volume list endpoint; a shared `KeysetPage<T>` result type (`Items`, `NextCursor`, `HasMore`) is used platform-wide. Low-volume lookup tables (`subscription_plans`, `document_categories`) may use simple offset/limit since their row counts are in the dozens.
- **Split query rules:** any query materializing an aggregate with more than one owned `ICollection<T>` navigation (e.g., `LeaseContract` with both `ContractStatusHistory` and `ContractDocuments`) uses `.AsSplitQuery()` explicitly — never the single-query JOIN default — to avoid the cartesian-explosion row multiplication EF Core's default `AsSingleQuery` produces across multiple one-to-many includes.
- **Compiled query policy:** `EF.CompileAsyncQuery` is applied only to the handful of queries proven hot by production telemetry (the notification unread-badge count and the auth-middleware subscription-status check are the two pre-identified candidates, both flagged in the DB docs themselves as evaluated on "effectively every request/session") — not applied speculatively elsewhere, consistent with the DB design's own repeated "don't add unnecessary [index/mechanism] without a concrete measured need" discipline.
- **Raw SQL policy:** raw SQL (`FromSqlInterpolated`/`ExecuteSqlInterpolated`) is permitted **only** for the mechanisms the DB docs themselves specify as needing to live outside ORM-expressible DDL/DML: (1) the receipt-number atomic-increment statement (§7.4's `UPDATE ... RETURNING`), (2) the `SET LOCAL app.current_company_id` / `app.current_user_id` RLS session variables (§7 below), (3) hand-written partial/GIN-trigram index creation inside EF Core migrations (Module 3 §3.0 already flags these as unexpressible in Prisma's DSL — identically true of EF Core's `HasIndex()` fluent API, which also lacks native `WHERE`-clause partial-index support prior to raw SQL migration customization). All raw SQL is parameterized; string concatenation into SQL is forbidden and enforced by the same Roslyn analyzer as the tracking rule.

---

## 7. Multi-Tenant Architecture

**Principle, stated once and never violated:** application-layer `company_id` filtering is a convenience and a second line of defense; **PostgreSQL RLS is the actual isolation boundary**, exactly as the DB design insists throughout (Phase 1 §1.0: "not just the application level"). The backend is built assuming RLS is always on and would still be safe even if an Application-layer filter were accidentally omitted.

- **`ITenantContext`** (`Application/Common/Interfaces/ITenantContext.cs`): exposes `CompanyId` (nullable `Guid`, resolved per-request) and `IsPlatformAdmin` (bypasses tenant scoping for the narrow set of platform-operator screens — `subscription_plans` management, `permissions` catalog). Implemented in `Infrastructure` by reading claims off the authenticated `ClaimsPrincipal` (see §8) established at JWT-validation time — never trusted from a request body/query string.
- **Resolution from authentication:** the JWT access token carries a `company_id` claim, set at login time from the `user_company_roles` row the user selected as their active company (a multi-company user picks/switches company client-side; switching issues a **new** token via a lightweight re-auth endpoint, it does not mutate the existing token). `ITenantContext` is populated once per request in a scoped DI registration, read by both the `TenantAuthorizationBehavior` (§3.3) and by Infrastructure's session-variable setup below.
- **EF Core global query filters:** every tenant-scoped entity configuration applies `HasQueryFilter(e => e.CompanyId == _tenantContext.CompanyId)` — this is the Application-layer convenience filter. It is **not** relied upon for isolation on its own.
- **PostgreSQL RLS relationship:** every tenant-scoped table's RLS policy (as specified per-module in the DB docs, e.g. `USING (company_id = current_setting('app.current_company_id')::uuid)`) is created in migrations and left **enabled and forced** (`ALTER TABLE ... FORCE ROW LEVEL SECURITY`, so even the table owner role is subject to it). The application's runtime DB role is a non-superuser role with no `BYPASSRLS` attribute.
- **Transaction-local tenant context / `SET LOCAL`:** at the start of every unit of work (both `SaveChangesAsync`-only and explicit-transaction paths, §12), an EF Core `SaveChangesInterceptor` (`TenantSessionInterceptor`) issues `SET LOCAL app.current_company_id = '<guid>'; SET LOCAL app.current_user_id = '<guid>';` as the first statement inside the transaction, using `NpgsqlCommand` raw SQL against the same connection EF Core is about to use. `SET LOCAL` is used specifically (not `SET`) because its scope is transaction-local and it is automatically reset on commit/rollback — this is what makes it **connection-pooling safe**: a pooled connection handed back to the pool after a transaction ends carries no leftover tenant context that could leak into the next, unrelated request that happens to reuse the same physical connection. This is a hard non-negotiable: the codebase forbids any raw-SQL `SET app.current_company_id` (session-scoped, not transaction-scoped) anywhere.
- **Read-only queries outside an explicit transaction** (the common case, since query handlers use `AsNoTracking()` with no `SaveChangesAsync`) still need the session variable set for RLS to apply. Npgsql's `NpgsqlDataSource` is configured with a connection-opened callback that wraps every command EF Core issues in an implicit `SET LOCAL` via Npgsql's `EnableTransactionScopeInterception` pattern — concretely, every query handler's repository call is wrapped by a lightweight `IDbContextTransaction` opened at `ReadCommitted` even for pure reads specifically so `SET LOCAL` has a transaction scope to live in; this transaction is always rolled back (never committed) for read-only work, which is cheap and side-effect-free in Postgres. This is documented explicitly here because it is the one place a naive EF Core setup would silently produce **connection-level** tenant leakage if `SET LOCAL` were issued outside any transaction (it would silently no-op or, worse, behave like session-level `SET` depending on driver behavior) — the always-wrap-in-a-transaction rule closes that gap.
- **Background job tenant context:** every Hangfire job method accepts an explicit `Guid companyId` parameter (never inferred from ambient state) and constructs its own scoped `ITenantContext` at the start of job execution — jobs never run under a request-scoped context, since none exists. Cross-company batch jobs (e.g., the subscription-expiration sweep, Module 2) explicitly loop over companies and set/reset tenant context per iteration inside its own transaction, never processing multiple companies' rows in one untenant-scoped query.
- **SignalR tenant context:** connections authenticate via the same JWT (passed as an access token query-string parameter on the SignalR handshake, per ASP.NET Core SignalR convention); on connect, the hub adds the connection to a `company:{companyId}` group derived from the token's claim, never from client-supplied input. Cross-tenant group membership is structurally impossible since the group name is server-computed.

---

## 8. Authentication Architecture

- **Login flow:** `POST /api/v1/auth/login` (email or +962 phone + password, or OTP request/verify for phone-first users per Phase 1 §1.2) → validates credentials against `users.password_hash` (Argon2id) → on success, issues an access token (JWT, 15-minute expiry, claims: `sub`=user id, `company_id`, `roles`, `permissions` — permissions embedded at issuance to avoid a DB round-trip per request, refreshed on next token issuance if changed) and a refresh token (opaque random value, stored **hashed** in `refresh_tokens.token_hash` per Module 3 §3.6) delivered as an `HttpOnly`, `Secure`, `SameSite=Strict` cookie — never returned in the JSON body, closing the XSS-token-theft vector the DB docs' own `token_hash`-only-storage design already defends against on the server side.
- **Access token flow:** short-lived, stateless, validated via standard JWT middleware (`Microsoft.AspNetCore.Authentication.JwtBearer`); never persisted server-side (per Module 3 §3.9: "access tokens (JWT, not persisted server-side)").
- **Refresh token flow:** `POST /api/v1/auth/refresh` reads the `HttpOnly` cookie, hashes the presented token, looks it up via `uq_refresh_tokens_token_hash`, and — if valid and unexpired — **rotates**: marks the presented row `revoked_reason = 'rotated'`, `replaced_by_token_id` set, and issues a new refresh-token row in the same `family_id`, per Module 3 §3.6's exact chain design. New access + refresh tokens are returned (refresh again as an `HttpOnly` cookie).
- **Rotation & reuse detection:** if a token whose row already has `revoked_reason = 'rotated'` is presented again, this is treated as replay/theft: the entire `family_id` is revoked in one transaction (`revoked_reason = 'theft_detected'`, `severity = 'critical'` audit log entry per Module 3 §3.9), and the response forces re-authentication (`401` with a `reason: "session_revoked"` body the frontend uses to redirect to login) — implemented as `RevokeTokenFamilyCommand`.
- **HttpOnly cookie strategy:** refresh token only; access token is returned in the JSON response body and held in memory (not `localStorage`) by the React client per the frontend's own security posture — this document only asserts the backend contract (cookie name, `Path=/api/v1/auth`, `SameSite=Strict`, `Secure` always on, no `Domain` attribute so it's never sent cross-subdomain).
- **Logout:** `POST /api/v1/auth/logout` revokes the current refresh token (`revoked_reason = 'logout'`) and clears the cookie.
- **Logout all sessions:** `POST /api/v1/auth/logout-all` revokes every non-revoked `refresh_tokens` row for `user_id` (used on password change and on explicit "sign out everywhere" — Module 3 §3.9's stated "Concurrent Session Management" requirement).
- **Password hashing:** Argon2id via `Konscious.Security.Cryptography`, tuned parameters (memory/iterations) stored as a versioned constant so future tuning doesn't invalidate existing hashes; `password_algorithm` column read to select the verification path, per the schema's own explicit future-migration provision.
- **Account lockout:** `failed_login_attempts` incremented atomically (`UPDATE ... SET failed_login_attempts = failed_login_attempts + 1`) on failed auth; on crossing the application-configured threshold, `locked_until` is set. Every attempt, success or failure, writes a `login_history` row regardless of lockout state, per Module 3 §3.7.
- **Email verification:** in scope per Phase 1 §1.2 (`email_verified_at`) — `POST /api/v1/auth/verify-email` consumes a signed, time-limited token (not stored in `refresh_tokens`; a separate short-lived signed-URL pattern, not a new table, since the DB design doesn't specify one).

---

## 9. Authorization Architecture

RBAC exactly as modeled in `roles`/`permissions`/`role_permissions`/`user_company_roles` (Module 3 §3.3–§3.5) — the backend introduces **no parallel authorization concept**, per the DB docs' own repeated assertion that every module "consumes the existing platform-wide permission catalog."

- **Roles used by the platform (per the approved scope):** System Admin (platform operator, `company_id IS NULL` context), Owner (immutable, system-seeded per company), Employee (maps to the various company-custom roles — Accountant, Maintenance, Leasing Agent, etc., all still instances of the same `roles` table), Tenant (portal-access role for `users` rows linked from `tenants.user_id`).
- **Permissions:** loaded once at token-issuance time from `role_permissions` (resolved via the same `user_company_roles → roles → role_permissions → permissions` path the DB docs specify, Module 3 §3.9/§3.10) and embedded as a claim array in the JWT — this avoids a `role_permissions` query on every request while staying correct within a token's 15-minute lifetime (a permission change takes effect on the user's next token refresh, an accepted latency window consistent with the DB design's own note that "permission changes take effect immediately for all members via the shared role_id lookup" at the data layer, with the token-refresh cadence being the application-layer propagation delay).
- **Policies:** one ASP.NET Core `AuthorizationPolicy` per permission key (`contracts.create`, `payments.approve`, `documents.view_confidential`, `notifications.view_all`, etc.) registered dynamically at startup by enumerating the `permissions` seed data — not one hardcoded policy per controller action written by hand, which would drift from the DB's own permission catalog over time.
- **Permission naming convention:** `<module>.<action>` exactly matching the DB docs' own examples (`contracts.create`, `payments.approve`, `reports.export`, `documents.view_confidential`, `notifications.view_all`) — the Api layer's `[Authorize(Policy = "contracts.create")]` attributes reference these strings directly, so the permission catalog is the single source of truth for both DB seed data and API authorization.
- **Authorization handlers:** a single generic `IAuthorizationHandler` (`PermissionAuthorizationHandler`) checks the JWT's embedded permission claims against the policy's required permission — no per-module custom handler needed for the common case. Two bespoke handlers exist for the two genuinely novel intra-tenant restrictions the DB docs flag explicitly: `ConfidentialDocumentAuthorizationHandler` (Module 10 §10.6 — permission-gated read of `is_confidential = true` rows) and `NotificationOwnershipAuthorizationHandler` (Module 11 §11.7 — `recipient_user_id` must equal the caller unless `notifications.view_all` is held).
- **Company boundary enforcement:** never repeated ad hoc inside controllers — the `TenantAuthorizationBehavior` pipeline behavior (§3.3) is the single enforcement point, checking that any resource ID referenced in a command/query actually belongs to `ITenantContext.CompanyId` (a defense-in-depth check *in addition to* RLS, per §7) before the handler runs.

---

## 10. Audit Architecture

Backed by `audit_logs` (Module 3 §3.8) exactly as specified — the backend guarantees audit coverage **without relying on developers remembering to write a row**, via an EF Core `SaveChangesInterceptor`.

- **`AuditSaveChangesInterceptor`** runs inside `SavingChangesAsync`, inspecting the `ChangeTracker` for every tracked entity whose `EntityState` is `Added`/`Modified`/`Deleted` (including soft-deletes, detected as a `Modified` entity whose `DeletedAt` changed from `null`) and constructs one `audit_logs` row per changed entity, capturing `entity_name`, `entity_id`, `action`, `previous_values`/`new_values` (JSONB diff of only the changed scalar properties, not a full-row snapshot, matching the DB docs' own stated JSONB-diff rationale) — inserted into the **same transaction** as the triggering change, guaranteeing atomicity (an audit row can never be missing for a change that committed, nor exist for one that rolled back).
- **Actor resolution:** `ICurrentUserContext.UserId`, populated identically to `ITenantContext` from the JWT; `null` for background-job-initiated changes (matching `actor_user_id`'s documented nullability for system actions).
- **Company resolution:** `ITenantContext.CompanyId`; `null` for the narrow set of platform-level actions (`subscription_plans` catalog edits by a System Admin).
- **Request correlation ID:** a `X-Correlation-Id` header, generated by middleware if absent, flows into `audit_logs.request_id` and into every Serilog log scope for the request (§18) — this is what lets a support engineer trace "this contract got corrupted — which request did it?" exactly as Module 3 §3.8 describes `request_id`'s purpose.
- **`correlation_id` (business-transaction grouping):** a second, business-level identifier (distinct from the per-HTTP-request `request_id`) generated once per MediatR command at the top of the `TransactionBehavior` and threaded through every `audit_logs` row written during that command's execution — this is what lets `WHERE correlation_id = $1` reconstruct "everything that happened when this tenant renewed" across the multiple `lease_contracts`/`contract_status_history`/`notifications` rows one renewal touches, exactly as Module 3 §3.8 specifies.
- **Old/new values:** JSONB diffs via `System.Text.Json`, restricted to changed properties only (never a full entity dump) — property-level change detection uses EF Core's own `PropertyEntry.IsModified`/`OriginalValue`/`CurrentValue`.
- **Sensitive field redaction:** a `[Sensitive]` attribute on entity properties (applied to `password_hash`, `mfa_secret_encrypted`, national ID fields on `tenants`, and any future PDPL-covered column) causes the interceptor to write a redaction placeholder (`"[REDACTED]"`) instead of the real value into `previous_values`/`new_values` — enforced at the interceptor level so no handler can accidentally leak a sensitive value into the audit trail.
- **Financial operation auditing:** high-severity fields (`amount`, `status`, `security_deposit_amount`, `outstanding_balance`, etc., per the specific "treated as high-severity events" call-outs scattered through Modules 5–7) are tagged `[AuditSeverity(Severity.High)]`; the interceptor maps this to `audit_logs.severity`. Everything else defaults to `info`, matching the DB docs' explicit "calibrated by actual consequence, not applied uniformly" posture.
- **System/background-job auditing:** Hangfire jobs run inside the same `SaveChangesAsync`/interceptor pipeline as request-driven code (via the same `PropertyOsDbContext`), so audit coverage is identical — `source = 'system_job'` is set by the interceptor when `ICurrentUserContext.UserId` is null and a `IJobExecutionContext` marker is present in the DI scope.

---

## 11. Soft Delete Architecture

- **`ISoftDeletable`** interface (`DeletedAt`, `DeletedBy`) applied to entity classes for every table the DB docs mark as standard-soft-delete. **Not** applied to the three explicitly documented exception classes from the DB design (repeated verbatim from the Database Architecture Summary):
  1. Pure append-only event logs — `meter_readings`, `login_history`, `audit_logs`, `contract_status_history`, `maintenance_status_history`. These entities simply have no `DeletedAt` property at all.
  2. No-independent-lifecycle 1:1 extensions — `company_settings`, `building_addresses`, `company_receipt_sequences`. No `DeletedAt` property; lifecycle is `CASCADE`-only from the parent.
  3. `notification_deliveries` — explicit "must never be deleted" mandate; no `DeletedAt` property, mutated in place instead (§11.3 of Module 11).
- **EF Core query filters:** every `ISoftDeletable` entity gets `HasQueryFilter(e => e.DeletedAt == null)` **combined with** (not replacing) the tenant `CompanyId` filter from §7 — EF Core supports multiple filters via a single combined lambda per entity, applied in each `IEntityTypeConfiguration<T>`.
- **Delete interception:** the same `AuditSaveChangesInterceptor` (§10) intercepts any `EntityState.Deleted` and — for any `ISoftDeletable` entity — converts it in-flight to `EntityState.Modified` with `DeletedAt = utcNow`, `DeletedBy = currentUserId` before `SaveChangesAsync` executes, so **no code path in the application can ever issue a hard `DELETE`** against a soft-deletable table by accident (`context.Remove(entity)` is safe to call anywhere; the interceptor rewrites it). Entities in soft-delete-exception classes 1–3 above simply don't implement `ISoftDeletable`, so `context.Remove(...)` on them performs (or, for append-only logs, is never called at all — repositories for those entities expose no `Delete` method).
- **`DeletedAt`/`DeletedBy`:** `DateTimeOffset?`/`Guid?`, matching the DB's `TIMESTAMPTZ`/`UUID` nullable columns exactly.
- **Restore behavior:** a `RestoreEntityCommand<T>` (used narrowly — e.g., un-retiring a `document_categories` row) sets `DeletedAt = null` explicitly; restore is **not** exposed for tables the DB docs describe as "exceptionally rare in practice" for soft-delete (`lease_contracts`, `rent_payments`) — those restores, if ever needed, are a deliberate admin/support action performed via direct, audited SQL, not a generic API endpoint, matching the DB design's own operational-policy language ("an active or superseded contract should, as a matter of operational policy, never be [soft-deleted]").
- **Unique index interaction:** every partial-unique-index-behind-a-`WHERE deleted_at IS NULL` clause in the DB docs (e.g., `uq_lease_contracts_company_contract_number`) is trusted to allow value reuse post-soft-delete at the database level; the backend does not duplicate this uniqueness check in C# beyond a friendly pre-check for UX (returning a clean `409` before hitting the DB constraint), since the DB constraint remains authoritative.
- **RLS interaction:** RLS policies (§7) are independent of soft-delete filtering — a soft-deleted row is still tenant-isolated by RLS even though the EF query filter additionally hides it from ordinary queries; an explicit "show deleted" admin query bypasses only the soft-delete filter (`IgnoreQueryFilters()` scoped narrowly to just that filter via EF Core 9's multi-filter selective-ignore support), never the tenant filter.

---

## 12. Transaction Architecture

**Rule, stated once:** `SaveChangesAsync` alone is sufficient when a command mutates exactly one aggregate root and its owned collections (EF Core already wraps a single `SaveChangesAsync` call in an implicit transaction). An **explicit `BeginTransactionAsync`** is mandatory whenever a command's documented business rule spans more than one aggregate root, or combines a raw-SQL statement (the receipt-sequence increment) with entity mutations. Every "application-orchestrated transaction" the DB docs name explicitly is implemented as exactly one `IDbContextTransaction` scope in Infrastructure, one-to-one:

| DB-documented transaction | Orchestrator | Aggregates touched |
|---|---|---|
| Lease activation | `LeaseActivationOrchestrator` | `LeaseContract` (status → active) |
| Lease renewal | `LeaseRenewalOrchestrator` | `LeaseContract` (new row) + `LeaseContract` (old row → superseded) + `ContractStatusHistory` ×2 |
| Lease termination | `LeaseTerminationOrchestrator` | `LeaseContract` (status → terminated) + `ContractTermination` + `ContractStatusHistory` |
| Lease activation → Marketplace listing rented | `LeaseActivationOrchestrator` calls into `IMarketplaceReconciliationService` inside the **same** transaction (Module 9 §9.0: "the same application-orchestrated transaction") | `LeaseContract` + `MarketplaceListing` |
| Payment allocation | `PaymentAllocationOrchestrator` | `RentPayment` (receiving) + `RentPayment` (obligation, trigger-recomputed) + `PaymentAllocation` |
| Cheque bounce reversal | `ChequeBounceOrchestrator` | `ChequeDetails` + `PaymentAllocation` (reversed) + `RentPayment` (recomputed via DB trigger) |
| Receipt issuance | `ReceiptIssuanceOrchestrator` | `CompanyReceiptSequence` (raw-SQL atomic increment) + `RentPaymentReceipt`/`ExpenseReceipt` |
| eFAWATEERcom transaction processing | `EfawateercomCallbackOrchestrator` | `EfawateercomTransaction` (update in place) + on success, delegates into `PaymentAllocationOrchestrator` |

- **Isolation level:** `ReadCommitted` (Npgsql/PostgreSQL default) for all transactions **except** the receipt-number generation and the payment-allocation over-allocation check, both of which rely on **row-level locking** (`SELECT ... FOR UPDATE` / the atomic `UPDATE ... RETURNING` pattern the DB docs specify) rather than a higher isolation level — this matches the DB design's own chosen concurrency mechanism exactly (Module 7 §7.4, Module 6 §6.3) rather than reaching for `Serializable`, which would be both unnecessary (the row lock already serializes the specific contended path) and a real throughput cost platform-wide.
- **Retry behavior:** Npgsql's built-in transient-failure retry (`EnableRetryOnFailure`) is **disabled** for any `IDbContextTransaction`-wrapped orchestrator (retrying a multi-statement transaction automatically is unsafe unless every statement is provably idempotent, which financial-allocation statements are not) — retry is instead handled by having the *caller* (Hangfire job or HTTP client) resubmit, using the idempotency mechanisms in §13. `EnableRetryOnFailure` **is** enabled for simple single-aggregate `SaveChangesAsync` command handlers and all read queries, where EF Core's automatic retry is safe.
- **Preventing partial financial writes:** every orchestrator in the table above is implemented as `try { begin tx; ...; commit; } catch { rollback; throw; }` with **no early return before commit** — enforced by a code-review checklist item and an integration test per orchestrator (§19) that deliberately injects a failure after the first of N writes and asserts zero rows persisted.

---

## 13. Background Job Architecture (Hangfire)

Job boundaries map 1:1 to the scheduled/automated processes the DB docs name explicitly — no new business workflows are introduced.

| Job | DB Docs Reference | Schedule |
|---|---|---|
| `GenerateScheduledInstallmentsJob` | Module 6 §6.0: "a background-job-generated ledger" | Daily, per company's `payment_due_day` |
| `ApplyGracePeriodAndLateStatusJob` | Module 6 §6.1 grace-period-aware trigger relies on `CURRENT_DATE`; a daily sweep additionally drives the "overdue reminder" notification trigger point | Daily |
| `ExpireSubscriptionsJob` | Module 2: "a scheduled job transitions trialing → expired" | Daily |
| `ExpireLeaseContractsJob` | Module 5 §5.1/§5.2: automated expiration sweep for `normal_expiration` terminations | Daily |
| `ExpireMarketplaceListingsJob` | Module 9 §9.1: "published → expired... via the scheduled job comparing expiration_date" | Daily |
| `DocumentExpiryReminderJob` | Module 10 §10.2 `idx_building_documents_expiring`: "the renewal-reminder job's daily sweep" | Daily |
| `NotificationDeliveryDispatchJob` | Module 11: dispatch across email/SMS/WhatsApp/in-app channels | Continuous queue (Hangfire recurring + enqueued-on-demand) |
| `RefreshTokenCleanupJob` | Module 3 §3.6: "hard-deleted by a retention-policy cleanup job" | Weekly |
| `EfawateercomReconciliationJob` | Module 7 §7.5: polling fallback for any callback that never arrived | Hourly |

- **Idempotency:** every job method's first action is a check against a durable idempotency marker (for period-based jobs: "has this company already had installments generated for period X" via a query against `rent_payments.billing_period_start`/`uq_rent_payments_contract_period` — the DB's own uniqueness constraint is the idempotency backstop; for callback-driven jobs: `external_transaction_id` uniqueness). No job relies solely on Hangfire's own de-duplication.
- **Retry policy:** Hangfire's `[AutomaticRetry(Attempts = 5, DelaysInSeconds = ...)]` with exponential backoff; jobs that call an orchestrator (§12) are safe to retry precisely because the orchestrator's underlying DB constraints (unique constraints, the over-allocation trigger) make a retried duplicate attempt fail cleanly rather than double-write.
- **Tenant context:** as stated in §7 — every job iterates companies explicitly and constructs a fresh `ITenantContext` per company per iteration, never ambient.
- **Distributed execution safety:** Hangfire's PostgreSQL storage provider (`Hangfire.PostgreSql`) uses the same database, giving job-queue durability without a second infrastructure dependency; recurring jobs use Hangfire's built-in distributed lock (`DisableConcurrentExecutionAttribute`) keyed per company+job-type where a job must not run twice concurrently for the same company (e.g., installment generation).
- **Job correlation IDs:** every job run generates its own `correlation_id` (§10) so every `audit_logs`/`rent_payments` row it produces is traceable to one job execution.

---

## 14. SignalR Architecture

Used **only** for genuinely real-time, push-driven concerns — not as a transport for ordinary CRUD, per the explicit instruction.

- **Hubs:** a single `NotificationsHub`, pushing newly-created `notifications` rows to their recipient in real time (badge-count updates, in-app toast) — the one place SignalR is justified, since `notifications`/`notification_deliveries` (Module 11) is explicitly a push-delivery concern (`in_app` is one of the four `delivery_channel_enum` values) that a polling REST endpoint would serve worse.
- **Groups:** `user:{userId}` (individual notification delivery) and `company:{companyId}` (used narrowly for company-wide operational broadcasts — e.g., "a new maintenance request just came in" for a live dashboard, still notification-shaped, not general CRUD).
- **Tenant isolation:** connection-time group assignment is server-computed from the JWT (§7) — no client-supplied company/user identifier is ever trusted for group membership.
- **Authentication:** standard ASP.NET Core SignalR JWT bearer auth via the access-token query-string parameter on the WebSocket handshake (SignalR's documented pattern, since browsers can't set custom headers on the WS upgrade request).
- **Notification events:** `NotificationCreated`, `NotificationRead` (for multi-device read-state sync) — no other event types. Leasing/Payments/Maintenance/Marketplace never push directly over SignalR; they publish a domain event (§3.1) that the `Notifications` module's handler turns into a `notifications` row, which *then* triggers the SignalR push — keeping SignalR strictly downstream of the `Notifications` module's own persistence, never a parallel unaudited channel.

---

## 15. File Storage Architecture

Backed by `file_storage` (Phase 1 §1.32; flagged as not yet physically detailed in the uploaded docs, per `PropertyOS_Architecture_Review.md` §2.2 — this section defines the standard column set implied by every module that references it: `id`, `company_id`, `uploaded_by`, `original_filename`, `mime_type`, `size_bytes`, `storage_key`, `visibility` enum, plus the Global Convention audit/soft-delete columns, `ON DELETE RESTRICT` from every consumer).

- **`IFileStorageService`** (Application interface) — `UploadAsync`, `GetDownloadUrlAsync` (short-lived pre-signed URL, never a raw public URL), `DeleteAsync` (soft, per §11 — actual object-storage bytes are only purged by a separate, explicitly audited retention job, never synchronously with a `file_storage` soft-delete).
- **Storage provider abstraction:** implemented in Infrastructure against any S3-compatible object store (AWS S3 or a Jordan/GCC-region-compatible equivalent) via the AWS SDK's S3-compatible client — the interface has no AWS-specific types leaking into Application.
- **File key strategy:** `{company_id}/{module}/{entity_id}/{file_storage_id}-{sanitized_original_filename}` — company-scoped prefix first, both for storage-provider-side lifecycle policies (per-tenant retention/cost reporting) and as a defense-in-depth measure (a leaked key still encodes tenant scope, making cross-tenant key-guessing structurally harder).
- **Metadata ownership:** `file_storage` is the single owner of upload metadata and access-control scope, exactly as Phase 1 §1.32 specifies — every consumer table (`contract_documents`, `expense_receipts`, `maintenance_request_attachments`, `building_documents`, `listing_images`, `meter_readings.photo_file_id`) only ever stores a `file_id` FK, never duplicated filename/mime/size metadata.
- **Upload flow:** client requests a pre-signed **upload** URL from the API (`POST /api/v1/files/upload-requests`, permission-checked per the consuming module, e.g. `documents.upload`) → client uploads directly to object storage → client confirms via `POST /api/v1/files/{id}/confirm`, which creates the `file_storage` row only after the provider confirms the object exists (`HeadObjectAsync`) — this avoids ever proxying large binary uploads through the API process itself.
- **Download authorization:** every download goes through `GET /api/v1/files/{id}/download-url`, which re-runs the **same permission check the owning module would apply** before minting a short-lived pre-signed download URL (5-minute expiry) — never a long-lived or public URL, and never a direct object-storage URL returned from any other endpoint.
- **Confidential document access:** for `building_documents.is_confidential = true`, the download-URL endpoint additionally invokes `ConfidentialDocumentAuthorizationHandler` (§9) before minting the URL — implementing the DB docs' own two-layer posture (Module 10 §10.6: application-layer primary defense, RLS-hardening recommended for later) at exactly the point access is granted.
- **Orphan file prevention:** `file_storage` rows are only ever created via the confirm-upload flow above (never speculatively), and every consumer FK is `NOT NULL`/`RESTRICT` per the DB docs — the backend additionally runs a weekly `OrphanFileSweepJob` that flags (not deletes) any `file_storage` row older than 24 hours with zero referencing rows across all consumer tables, surfaced to a platform-admin cleanup screen rather than auto-deleted, since a false-positive delete of a legitimately-in-progress multi-step upload would be worse than a manual review step.

---

## 16. API Architecture

- **Controller organization:** one controller per aggregate root, mirroring §2's folder structure (`LeaseContractsController`, `RentPaymentsController`, `MaintenanceRequestsController`, …) — controllers contain **only**: model binding, calling `IMediator.Send(...)`, and translating the result to an `ActionResult`. No business logic, no direct repository/DbContext access, ever.
- **API versioning:** URL-segment versioning (`/api/v1/...`) via `Asp.Versioning.Http` — chosen over header-based versioning for discoverability in Swagger/Scalar and simplicity for the React client.
- **Route naming:** REST-conventional, resource-plural, nested only one level deep for genuine parent-child ownership matching the DB's own aggregate boundaries (e.g., `/api/v1/lease-contracts/{id}/terminations`, `/api/v1/maintenance-requests/{id}/comments`) — never deeper nesting, matching the "aggregate root owns its children" repository rule in §5.
- **Request/response models:** Api-layer `record` types distinct from Application DTOs (a thin, mostly-1:1 mapping layer maintained via Mapster) — this indirection exists specifically so a public API contract change (e.g., renaming a JSON field for frontend convenience) never forces an Application-layer DTO/command rename, keeping the transport boundary genuinely separate from the use-case boundary.
- **ProblemDetails:** every error response uses RFC 7807 `ProblemDetails` (built-in ASP.NET Core 9 support) — validation failures use the `ValidationProblemDetails` subtype with a `errors` dictionary keyed by property name (from FluentValidation's `ValidationException`, translated by a global exception filter).
- **Global exception handling:** a single `IExceptionHandler` (ASP.NET Core 9's `IExceptionHandler` pipeline, not the older exception-handling middleware pattern) maps: `ValidationException` → `400`, `NotFoundException` (Application-defined) → `404`, `ForbiddenException`/`UnauthorizedAccessException` → `403`, `DbUpdateConcurrencyException` → `409`, `ConflictException` (e.g., a unique-constraint violation surfaced from a repository as a typed exception, per the DB's own partial-unique-index-driven business rules like "one active lease per apartment") → `409`, everything else → `500` with the exception detail **stripped** in production (full detail only in `Development` environment) and the correlation ID always included so the client can reference it in a support request.
- **Correlation IDs:** every response includes an `X-Correlation-Id` header (§10) even on success, so the frontend can log it against any user-reported issue proactively, not only on error.
- **Pagination response format:** `{ "items": [...], "nextCursor": "opaque-base64-string", "hasMore": true }` for keyset-paginated list endpoints (§6); the cursor encodes the last row's index tuple (e.g., `(dueDate, id)`), opaque to the client, decoded server-side by the query handler.

---

## 17. Security Architecture

- **HTTPS enforcement:** `UseHsts()` + `UseHttpsRedirection()` unconditionally; HTTP is not served at all outside local development (`Development` environment only binds HTTP).
- **CORS:** an explicit allow-list of the React app's known origins (per environment), `AllowCredentials()` enabled (required for the `HttpOnly` refresh cookie) — never `AllowAnyOrigin()`, which is incompatible with credentialed requests anyway.
- **CSRF strategy for cookie authentication:** since the refresh token is a cookie, CSRF is mitigated via `SameSite=Strict` (defeats the standard cross-site-form-submission CSRF vector outright) **plus** a double-submit anti-CSRF token (ASP.NET Core's built-in `IAntiforgery`) required on the `/auth/refresh` and `/auth/logout` endpoints specifically, since those are the only cookie-authenticated (as opposed to bearer-token-authenticated) endpoints in the system — every other endpoint requires the JWT bearer token in an `Authorization` header, which is immune to CSRF by construction (a cross-site form cannot set a custom header).
- **Security headers:** `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY` applied via middleware.
- **Rate limiting:** ASP.NET Core 9's built-in `Microsoft.AspNetCore.RateLimiting` — a global fixed-window limiter, plus a **stricter, dedicated limiter on the two unauthenticated public endpoints the DB docs explicitly flag as needing it**: `POST /api/v1/viewing-requests` (Module 9 §9.7: "Rate-limiting and basic spam/bot mitigation... recommended Phase 8 hardening for this specific unauthenticated write surface") and `GET /api/v1/marketplace/listings` search (Module 9 §7.1: "anonymous-traffic abuse... rate-limiting/query-cost abuse from unauthenticated scraping"), plus `POST /api/v1/auth/login` (brute-force protection, in addition to the account-lockout mechanism in §8).
- **Request size limits:** `MaxRequestBodySize` capped platform-wide (small, e.g. 1 MB for JSON bodies); file uploads never hit this limit since they go directly to object storage (§15), not through the API body.
- **File upload validation:** MIME-type allow-list + magic-byte verification (not just trusting the client-supplied `Content-Type`) performed at the upload-confirmation step (§15), before a `file_storage` row is created.
- **SQL injection protection:** structurally near-impossible given §6's raw-SQL policy (parameterized-only, narrowly scoped to three named mechanisms) plus EF Core's parameterization of all LINQ-translated queries by default.
- **Mass assignment prevention:** Api-layer request records are explicit allow-lists of bindable properties (never `[Bind(Include=...)]` string-based binding, never binding directly to an entity or even to an Application DTO that has more properties than the specific use case needs) — a command like `CreateLeaseContractCommand` has no `Status` or `PriorContractId` settable property, since those are set by the orchestrator, not the client.
- **Secret management:** connection strings, JWT signing keys, and the eFAWATEERcom gateway credentials are never in `appsettings.json`; sourced from environment variables in development and a managed secret store (Azure Key Vault / AWS Secrets Manager, environment-dependent) in staging/production, injected via `IConfiguration` at startup.
- **PII logging policy:** the same `[Sensitive]` attribute mechanism from §10 is consulted by a custom Serilog destructuring policy so PII (national ID, password hashes, MFA secrets, full phone numbers beyond a masked suffix) is never written to log sinks even incidentally via exception message interpolation or model dumps.

---

## 18. Observability

- **Serilog:** structured JSON sink to the log aggregator (environment-dependent), console sink (human-readable) in development. Every log event is enriched with `CorrelationId`, `CompanyId`, `UserId` (all `null`-safe for unauthenticated/system contexts) via a custom `LogContext.PushProperty` set once per request/job in middleware, so every downstream log line in that scope automatically carries them without each call site repeating them.
- **Correlation IDs:** as established in §10/§16 — one HTTP-request-scoped ID, one business-transaction-scoped ID, both logged.
- **Sentry:** captures unhandled exceptions (via the `IExceptionHandler` from §16, which reports to Sentry before translating to `ProblemDetails`) with the same correlation/tenant enrichment as Serilog; PII scrubbing configured identically to the logging policy above.
- **Health checks:** `/health/live` (process is running) and `/health/ready` (dependencies reachable) via `Microsoft.Extensions.Diagnostics.HealthChecks`.
- **PostgreSQL health check:** `AddNpgSql(connectionString)` — a lightweight `SELECT 1`, not a full query, to avoid the health check itself becoming a load concern.
- **Hangfire health check:** a custom check verifying the Hangfire storage connection and that the recurring-job schedule has been successfully registered (catches a startup misconfiguration where jobs silently never got scheduled).
- **Logging levels:** `Information` for business events (command succeeded, notification dispatched), `Warning` for recoverable issues (a failed notification-channel delivery, an eFAWATEERcom timeout), `Error` for unhandled exceptions, `Debug` (development only) for EF Core generated SQL.
- **Sensitive data filtering:** covered above; additionally, EF Core's own SQL-parameter logging (which by default can log literal parameter values) is explicitly configured with `EnableSensitiveDataLogging()` **disabled** in every environment except local development.

---

## 19. Testing Architecture

- **Unit tests (`PropertyOS.Tests.Unit`):** Domain entity invariants (e.g., a `LeaseContract` value object rejecting `end_date <= start_date` at construction, mirroring `chk_lease_contracts_dates`) and Application command/query handlers with repository interfaces mocked (via `NSubstitute`) — no database, no HTTP, fast (`<100ms` per test suite run target). Includes the architecture-boundary tests from §22.
- **Integration tests (`PropertyOS.Tests.Integration`):** run against **real PostgreSQL 17 via Testcontainers** (`Testcontainers.PostgreSql`), spinning up a fresh containerized database, applying all EF Core migrations (including the hand-written raw-SQL partial/GIN indexes and RLS policies — §6), seeding minimal reference data, then exercising the full stack. **EF Core's InMemory provider is not used anywhere** for anything touching database behavior — it cannot express PostgreSQL enums, partial indexes, RLS, triggers, or `xmin` concurrency tokens, all of which are load-bearing parts of this design, so any test using InMemory would provide false confidence.
- **RLS tests:** a dedicated `RlsIsolationTests` fixture that opens two application-role connections with two different `SET LOCAL app.current_company_id` values and asserts that a query from company A's context returns zero rows for company B's data **even when the EF Core global query filter is deliberately bypassed** (`IgnoreQueryFilters()`) — this is the test that actually proves RLS, not just the application-layer filter, is doing the isolating.
- **Multi-tenant isolation tests:** end-to-end HTTP tests (via `WebApplicationFactory<Program>` against the Testcontainers database) asserting that an authenticated user for company A receives `404` (not `403`, to avoid resource-existence leakage) when requesting a resource ID that belongs to company B.
- **Financial transaction tests:** per orchestrator in §12's table — asserting correct final state after success, and (per §12's partial-write-prevention rule) zero persisted rows after a deliberately injected mid-transaction failure. Includes a dedicated test reproducing the over-allocation trigger's rejection and asserting the C# layer surfaces it as a clean `409`, not a raw Postgres exception leaking to the client.
- **Concurrency tests:** two concurrent `RecordPaymentAllocationCommand`s racing to allocate against the same `rent_payments` row, asserting the DB trigger + `xmin` concurrency token together prevent an over-allocation regardless of race timing (the DB trigger is the actual correctness guarantee; the concurrency token additionally prevents a lost-update on the cached `amount_paid` display value).
- **Authentication tests:** login, refresh rotation, reuse-detection-triggers-family-revocation (the specific scenario Module 3 §3.6 exists to prevent), lockout-after-N-failures.
- **Authorization tests:** one test per permission-gated endpoint asserting `403` when the required permission is absent, plus the two bespoke handlers from §9 (confidential-document access, notification ownership).

---

## 20. Final Solution Structure

```
PropertyOS.sln
src/
  PropertyOS.Domain/
    PropertyOS.Domain.csproj
    Common/
      Entity.cs, AggregateRoot.cs, ValueObject.cs, ISoftDeletable.cs, IDomainEvent.cs
      ValueObjects/ Money.cs, Address.cs, DateRange.cs
    Companies/
      Company.cs, CompanySettings.cs
      Enums/ CompanyType.cs
    Subscriptions/
      SubscriptionPlan.cs, CompanySubscription.cs
      Enums/ SubscriptionStatus.cs, BillingCycle.cs
    Identity/
      User.cs, UserCompanyRole.cs, Role.cs, Permission.cs, RolePermission.cs
      RefreshToken.cs, LoginHistory.cs
      Enums/ MfaType.cs, MembershipStatus.cs, RevokeReason.cs, LoginStatus.cs
    Audit/
      AuditLog.cs
      Enums/ AuditAction.cs, AuditSeverity.cs, AuditSource.cs
    Properties/
      Building.cs, BuildingAddress.cs, Floor.cs, Apartment.cs
      ParkingSpot.cs, ParkingAssignment.cs, UtilityMeter.cs, MeterReading.cs
      Enums/ BuildingType.cs, Governorate.cs, FloorType.cs, OwnershipStatus.cs, OccupancyStatus.cs,
             MeterScope.cs, MeterType.cs, AllocationMethod.cs, MeterStatus.cs, EntrySource.cs,
             ParkingType.cs, ParkingAssignmentStatus.cs
    Leasing/
      Tenant.cs, TenantFamilyMember.cs, TenantEmergencyContact.cs, TenantVehicle.cs
      LeaseContract.cs, ContractTermination.cs, ContractStatusHistory.cs, ContractDocument.cs
      Enums/ ContractStatus.cs, LegalRegime.cs, TenantType.cs, PaymentFrequency.cs, TerminationType.cs,
             ContractDocumentType.cs
      Events/ LeaseContractActivated.cs, LeaseContractRenewed.cs, LeaseContractTerminated.cs
    Payments/
      RentPayment.cs, ChequeDetails.cs, PaymentAllocation.cs
      Enums/ PaymentPurpose.cs, PaymentMethod.cs, DueDateStatus.cs, ChequeStatus.cs, AllocationStatus.cs
      Events/ PaymentAllocated.cs, AllocationReversed.cs, ChequeBounced.cs
    Financials/
      Expense.cs, ExpenseReceipt.cs, RentPaymentReceipt.cs, CompanyReceiptSequence.cs, EfawateercomTransaction.cs
      Enums/ ExpenseCategory.cs, ExpensePaymentMethod.cs, ReceiptResetPolicy.cs, EfawateercomStatus.cs
    Maintenance/
      MaintenanceRequest.cs, MaintenanceRequestAttachment.cs, MaintenanceRequestComment.cs, MaintenanceStatusHistory.cs
      Enums/ MaintenanceCategory.cs, MaintenancePriority.cs, MaintenanceStatus.cs
      Events/ MaintenanceStatusChanged.cs
    Marketplace/
      MarketplaceListing.cs, ListingImage.cs, ViewingRequest.cs
      Enums/ ListingStatus.cs, ViewingRequestStatus.cs
      Events/ ListingPublished.cs, ListingRented.cs
    Documents/
      DocumentCategory.cs, BuildingDocument.cs
    Notifications/
      NotificationTemplate.cs, Notification.cs, NotificationDelivery.cs
      Enums/ NotificationType.cs, NotificationStatus.cs, NotificationPriority.cs,
             DeliveryChannel.cs, DeliveryStatus.cs
    Files/
      FileStorage.cs
      Enums/ FileVisibility.cs
    Reports/
      ReportDefinition.cs, ReportSnapshot.cs

  PropertyOS.Application/
    PropertyOS.Application.csproj
    Common/
      Behaviors/ ValidationBehavior.cs, TenantAuthorizationBehavior.cs, TransactionBehavior.cs,
                 AuditBehavior.cs, LoggingBehavior.cs
      Interfaces/ ITenantContext.cs, ICurrentUserContext.cs, IUnitOfWork.cs, IFileStorageService.cs,
                  IDateTimeProvider.cs
      Exceptions/ NotFoundException.cs, ForbiddenException.cs, ConflictException.cs
      Pagination/ KeysetPage.cs, KeysetCursor.cs
    Companies/ Commands/ Queries/ Dtos/ Repositories/ (ICompanyRepository.cs)
    Subscriptions/ Commands/ Queries/ Dtos/ Repositories/
    Identity/ Commands/ (Login, Refresh, Logout, ChangePassword) Queries/ Dtos/ Repositories/
    Audit/ Queries/ Dtos/ Repositories/
    Properties/ Commands/ Queries/ Dtos/ Repositories/ (IBuildingRepository, IApartmentRepository, IUtilityMeterRepository)
    Leasing/ Commands/ Queries/ Dtos/ Repositories/ (ILeaseContractRepository, ITenantRepository)
    Payments/ Commands/ Queries/ Dtos/ Repositories/ (IRentPaymentRepository)
    Financials/ Commands/ Queries/ Dtos/ Repositories/ (IExpenseRepository, IReceiptSequenceRepository)
    Maintenance/ Commands/ Queries/ Dtos/ Repositories/ (IMaintenanceRequestRepository)
    Marketplace/ Commands/ Queries/ Dtos/ Repositories/ (IMarketplaceListingRepository)
    Documents/ Commands/ Queries/ Dtos/ Repositories/ (IBuildingDocumentRepository)
    Notifications/ Commands/ Queries/ Dtos/ Repositories/ (INotificationRepository)
    Files/ Commands/ Queries/ Dtos/
    Reports/ Queries/ Dtos/

  PropertyOS.Infrastructure/
    PropertyOS.Infrastructure.csproj
    Persistence/
      PropertyOsDbContext.cs
      Interceptors/ AuditSaveChangesInterceptor.cs, TenantSessionInterceptor.cs
      Migrations/  (EF Core migrations, including hand-appended raw SQL for partial/GIN indexes and RLS policies)
    Companies/Configurations/ Repositories/
    Subscriptions/Configurations/ Repositories/
    Identity/Configurations/ Repositories/ Services/ (PasswordHasher, TokenService)
    Audit/Configurations/ Repositories/
    Properties/Configurations/ Repositories/
    Leasing/Configurations/ Repositories/ Services/ (LeaseActivationOrchestrator, LeaseRenewalOrchestrator, LeaseTerminationOrchestrator)
    Payments/Configurations/ Repositories/ Services/ (PaymentAllocationOrchestrator, ChequeBounceOrchestrator)
    Financials/Configurations/ Repositories/ Services/ (ReceiptIssuanceOrchestrator, EfawateercomClient, EfawateercomCallbackOrchestrator)
    Maintenance/Configurations/ Repositories/
    Marketplace/Configurations/ Repositories/ Services/ (MarketplaceReconciliationService)
    Documents/Configurations/ Repositories/
    Notifications/Configurations/ Repositories/ Services/ (NotificationDispatchService)
    Files/ Services/ (S3FileStorageService)
    Reports/Configurations/ Repositories/
    BackgroundJobs/
      GenerateScheduledInstallmentsJob.cs, ApplyGracePeriodAndLateStatusJob.cs, ExpireSubscriptionsJob.cs,
      ExpireLeaseContractsJob.cs, ExpireMarketplaceListingsJob.cs, DocumentExpiryReminderJob.cs,
      NotificationDeliveryDispatchJob.cs, RefreshTokenCleanupJob.cs, EfawateercomReconciliationJob.cs,
      OrphanFileSweepJob.cs
    RealTime/
      NotificationsHub.cs
    DependencyInjection.cs

  PropertyOS.Api/
    PropertyOS.Api.csproj
    Program.cs
    Middleware/ CorrelationIdMiddleware.cs, ExceptionHandler.cs
    Auth/ AuthorizationPolicyProvider.cs, PermissionAuthorizationHandler.cs,
          ConfidentialDocumentAuthorizationHandler.cs, NotificationOwnershipAuthorizationHandler.cs
    Companies/Controllers/ Contracts/
    Subscriptions/Controllers/ Contracts/
    Identity/Controllers/ (AuthController) Contracts/
    Properties/Controllers/ Contracts/
    Leasing/Controllers/ Contracts/
    Payments/Controllers/ Contracts/
    Financials/Controllers/ Contracts/
    Maintenance/Controllers/ Contracts/
    Marketplace/Controllers/ Contracts/
    Documents/Controllers/ Contracts/
    Notifications/Controllers/ Contracts/
    Files/Controllers/ Contracts/
    Reports/Controllers/ Contracts/
    appsettings.json, appsettings.Development.json

tests/
  PropertyOS.Tests.Unit/
    PropertyOS.Tests.Unit.csproj
    Architecture/ DependencyRuleTests.cs
    Domain/ (per-module invariant tests, mirroring Domain folder structure)
    Application/ (per-module handler tests, mirroring Application folder structure)

  PropertyOS.Tests.Integration/
    PropertyOS.Tests.Integration.csproj
    Common/ PostgresContainerFixture.cs, WebApplicationFactoryFixture.cs
    Rls/ RlsIsolationTests.cs
    MultiTenant/ CrossTenantAccessTests.cs
    Financials/ PaymentAllocationTransactionTests.cs, ReceiptSequenceConcurrencyTests.cs
    Leasing/ LeaseRenewalTransactionTests.cs, LeaseTerminationTransactionTests.cs
    Marketplace/ ListingReconciliationTests.cs
    Identity/ AuthenticationFlowTests.cs, AuthorizationPolicyTests.cs
```

---

## 21. Initial Solution Creation

### 21.1 Solution and Projects

```bash
dotnet new sln -n PropertyOS

dotnet new classlib -n PropertyOS.Domain -o src/PropertyOS.Domain -f net9.0
dotnet new classlib -n PropertyOS.Application -o src/PropertyOS.Application -f net9.0
dotnet new classlib -n PropertyOS.Infrastructure -o src/PropertyOS.Infrastructure -f net9.0
dotnet new webapi -n PropertyOS.Api -o src/PropertyOS.Api -f net9.0 --use-controllers

dotnet new xunit -n PropertyOS.Tests.Unit -o tests/PropertyOS.Tests.Unit -f net9.0
dotnet new xunit -n PropertyOS.Tests.Integration -o tests/PropertyOS.Tests.Integration -f net9.0

dotnet sln add src/PropertyOS.Domain/PropertyOS.Domain.csproj
dotnet sln add src/PropertyOS.Application/PropertyOS.Application.csproj
dotnet sln add src/PropertyOS.Infrastructure/PropertyOS.Infrastructure.csproj
dotnet sln add src/PropertyOS.Api/PropertyOS.Api.csproj
dotnet sln add tests/PropertyOS.Tests.Unit/PropertyOS.Tests.Unit.csproj
dotnet sln add tests/PropertyOS.Tests.Integration/PropertyOS.Tests.Integration.csproj
```

### 21.2 Project References

```bash
dotnet add src/PropertyOS.Application reference src/PropertyOS.Domain
dotnet add src/PropertyOS.Infrastructure reference src/PropertyOS.Application
dotnet add src/PropertyOS.Infrastructure reference src/PropertyOS.Domain
dotnet add src/PropertyOS.Api reference src/PropertyOS.Application
dotnet add src/PropertyOS.Api reference src/PropertyOS.Infrastructure

dotnet add tests/PropertyOS.Tests.Unit reference src/PropertyOS.Domain
dotnet add tests/PropertyOS.Tests.Unit reference src/PropertyOS.Application

dotnet add tests/PropertyOS.Tests.Integration reference src/PropertyOS.Api
dotnet add tests/PropertyOS.Tests.Integration reference src/PropertyOS.Infrastructure
dotnet add tests/PropertyOS.Tests.Integration reference src/PropertyOS.Application
```

### 21.3 NuGet Packages per Project

**PropertyOS.Domain** — none. (Kept dependency-free by design; §22 enforces this.)

**PropertyOS.Application**
```bash
dotnet add src/PropertyOS.Application package MediatR
dotnet add src/PropertyOS.Application package FluentValidation
dotnet add src/PropertyOS.Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/PropertyOS.Application package Mapster
```

**PropertyOS.Infrastructure**
```bash
dotnet add src/PropertyOS.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/PropertyOS.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/PropertyOS.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/PropertyOS.Infrastructure package Hangfire.Core
dotnet add src/PropertyOS.Infrastructure package Hangfire.PostgreSql
dotnet add src/PropertyOS.Infrastructure package Microsoft.AspNetCore.SignalR.Common
dotnet add src/PropertyOS.Infrastructure package AWSSDK.S3
dotnet add src/PropertyOS.Infrastructure package Konscious.Security.Cryptography.Argon2
dotnet add src/PropertyOS.Infrastructure package Serilog.AspNetCore
```

**PropertyOS.Api**
```bash
dotnet add src/PropertyOS.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/PropertyOS.Api package Asp.Versioning.Http
dotnet add src/PropertyOS.Api package Asp.Versioning.Mvc.ApiExplorer
dotnet add src/PropertyOS.Api package Swashbuckle.AspNetCore
dotnet add src/PropertyOS.Api package Scalar.AspNetCore
dotnet add src/PropertyOS.Api package Sentry.AspNetCore
dotnet add src/PropertyOS.Api package Serilog.AspNetCore
dotnet add src/PropertyOS.Api package Hangfire.AspNetCore
dotnet add src/PropertyOS.Api package Microsoft.AspNetCore.SignalR
dotnet add src/PropertyOS.Api package AspNetCore.HealthChecks.NpgSql
dotnet add src/PropertyOS.Api package AspNetCore.HealthChecks.Hangfire
```

**PropertyOS.Tests.Unit**
```bash
dotnet add tests/PropertyOS.Tests.Unit package NSubstitute
dotnet add tests/PropertyOS.Tests.Unit package FluentAssertions
```

**PropertyOS.Tests.Integration**
```bash
dotnet add tests/PropertyOS.Tests.Integration package Testcontainers.PostgreSql
dotnet add tests/PropertyOS.Tests.Integration package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/PropertyOS.Tests.Integration package FluentAssertions
dotnet add tests/PropertyOS.Tests.Integration package Respawn
```

No package is installed speculatively (no Redis client — deferred per the stated "introduced later only when justified" instruction; no AutoMapper alongside Mapster; no generic-repository package, per §5's explicit rejection of that pattern).

---

## 22. Architecture Self-Review

- **Clean Architecture dependency violations:** none — verified by the reference graph in §21.2 (`Domain` has zero project references; `Application` references only `Domain`; `Infrastructure` references `Application` + `Domain`; `Api` references `Application` + `Infrastructure`, never bypassing `Application` to call `Infrastructure` repositories directly from a controller). `PropertyOS.Tests.Unit/Architecture/DependencyRuleTests.cs` asserts this mechanically at build/test time (via reflection over each assembly's `AssemblyName.GetReferencedAssemblies()`), so a future accidental violation fails CI rather than being caught only in review.
- **Unnecessary abstractions:** the generic-repository temptation was explicitly rejected in §5 in favor of aggregate-specific repositories, with the *one* justified generic exception (`IReadOnlyLookupRepository<T>` for genuinely interchangeable reference tables) called out rather than silently generalized further. Application/Api DTO separation (§16) was considered against "just reuse Application DTOs at the API boundary" and kept separate specifically because the DB docs' own module boundaries (11 independently evolving modules) make a transport-format change in one module a real, non-hypothetical future event.
- **Overengineering:** no Event Sourcing, no separate CQRS read-model database, no generic repository — each rejected explicitly with reasoning in §3/§5, matching the DB design's own consistently pragmatic posture (table-per-type over polymorphism, layered defense over premature DB-trigger-everywhere). Redis is explicitly deferred, not pre-integrated speculatively, per the brief's own instruction.
- **EF Core performance risks:** addressed directly in §6 — `AsNoTracking()` enforced by analyzer on every query handler, keyset pagination mandated for every high-volume list endpoint (matching the DB's own composite-index design intent), split queries mandated for multi-collection includes, projection-to-DTO via Mapster rather than full-entity materialization for list/search queries. The one residual risk flagged rather than hidden: compiled queries are deliberately **not** applied broadly, only to the two proven-hot paths — if production telemetry surfaces additional hot paths, this list should grow, and that's noted here as a live follow-up rather than a closed decision.
- **Multi-tenant leakage risks:** the single highest-risk area, addressed with two independent layers (EF query filter + RLS) exactly matching the DB docs' own explicit "application filtering alone is NOT sufficient" instruction, plus the `SET LOCAL`-inside-a-transaction discipline in §7 specifically closing the connection-pooling leakage vector that a naive `SET`-based implementation would open. `RlsIsolationTests` (§19) is the concrete verification that this isn't just a design intention.
- **RLS compatibility:** every raw-SQL-requiring mechanism the DB docs themselves flag as ORM-inexpressible (partial indexes, GIN trigram indexes, RLS policies) is explicitly routed through hand-appended raw SQL in EF Core migrations (§6/§20), matching the identical "hand-edited migration SQL" approach the DB docs already mandate for Prisma — this architecture does not pretend EF Core's fluent API can express what the DB docs already say Prisma's DSL cannot; it names the same gap and closes it the same way.
- **Transaction safety:** every DB-docs-named multi-table transaction has a named, one-to-one orchestrator (§12's table) rather than being left as an implicit expectation scattered across handler code — this was specifically checked against the DB docs' own list of application-orchestrated transactions to ensure none were missed (lease activation, renewal, termination, marketplace reconciliation, payment allocation, cheque bounce reversal, receipt issuance, eFAWATEERcom processing — all eight present).
- **Financial integrity:** the backend deliberately does **not** attempt to re-implement the over-allocation check in C# as the primary defense — the DB trigger (already built per Module 6 §6.3, not deferred to "Phase 8") remains authoritative, and the backend's role is to catch the resulting Postgres exception and surface it as a clean `409` (§16) rather than duplicate the business rule in two places that could drift out of sync. This mirrors the DB docs' own "don't rely solely on application-layer discipline for a financial-integrity invariant" posture (§7.8 of Module 7) applied in the other direction — here, the backend defers to the DB rather than the DB deferring to the backend.
- **Maintainability:** the module-mirrored-folder structure (§2) was checked against the specific risk called out in the brief ("avoid one giant global folder") — confirmed every one of the 11 DB modules plus the 3 cross-cutting concerns (`Identity`/`Audit`, `Files`, `Reports`) has its own folder in all four projects, with no shared `Services`/`Controllers`/`DTOs` catch-all folder anywhere in the tree shown in §20.

---

*End of PropertyOS Backend Architecture. This document is the implementation source of truth for the backend team; no controllers, entities, EF Core configurations, or migrations have been written — this phase is architecture and solution scaffolding only, per the stated scope.*

# Phase 04 — Rent Payments & Cheques (Module 6) + shared Financials core

Covers the Module 6 side of the shared `Financials` code family; Module 7 specifics in `05-financial-operations.md`.

## Initial State
Domain/Application/Infrastructure complete on paper (16 commands, 13 queries, 4 repositories, RLS policies, partial unique indexes, numeric(12,3) money). No API surface, no background jobs. Docs §6.0–§6.6 define the allocation ledger contract: `payment_allocations` is the source of truth; `rent_payments.amount_paid`/`due_date_status` are caches; over-allocation must be impossible at DB level.

## Requirements Reviewed
`docs/PropertyOS_Module6_RentPayments_FINAL.md` (§6.0 design decisions, §6.1 rent_payments, §6.2 cheque lifecycle + bounce reversal, §6.3 payment_allocations + over-allocation trigger mandate, §6.4–6.6); Module 7 §7.4 receipt sequences; Architecture doc transaction/exception conventions.

## Problems Found
1. **Fatal:** `ReceiveEfawateercomCallbackCommandHandler` dispatched the nested allocation command with `ReceivingPaymentId = Guid.Empty` (IDs were DB-generated, assigned only at SaveChanges) — every successful gateway callback would throw and roll back in production. Both existing tests masked it (unit FakeMediator compared `Guid.Empty == Guid.Empty`; integration FakeMediator used change-tracker `FindAsync`).
2. **Critical:** `amount_paid` and `due_date_status` were configured `ValueGeneratedOnAddOrUpdate` (anticipating a DB maintenance trigger that never existed) — EF silently dropped **every** application write to them: allocation sync, reversal recompute, bounce cascade, and `RentPayment.Cancel` never persisted. Masked end-to-end by fakes.
3. **Critical:** no concurrency control on allocation writes — two concurrent allocators against the same obligation both passed the over-allocation check (unlocked read-modify-write over `SUM(allocated_amount)`); no DB backstop trigger existed despite the doc §6.3 mandate.
4. Batch-internal double counting: repeated `ObligationPaymentId` entries in one command re-read stale DB sums.
5. Allocation totals and write-path loads included soft-deleted rows (`DeletedAt` unfiltered); `HasScheduledInstallmentAsync` disagreed with its partial unique index.
6. No currency-equality guard between receiving payment and obligation.
7. Handlers used raw BCL exceptions (→ HTTP 500s).
8. Receipts created inside aggregates (`IssueReceipt`/`AttachReceipt`) with client-generated keys were tracked as `Modified` by EF navigation discovery → `DbUpdateConcurrencyException` (surfaced once IDs became client-generated).
9. Pro-rated installment amounts stored unrounded (PostgreSQL rounds on insert, implicitly).
10. Missing entirely: manual payment recording (cash/bank/cheque — `ChequeDetails.Create` had zero production callers), manual receipt issuance, payment cancellation, overdue sweep, installment-generation scheduling, eFAWATEERcom expiry sweep, all API endpoints, pagination validators on 3 paged queries.

## Implementation Completed
(1st increment — verified) Client-generated `Guid.CreateVersion7()` across all 8 Financials factories; FOR UPDATE locking protocol via `IRentPaymentRepository.GetByIdsForUpdateAsync` (single sorted `id = ANY(...) FOR UPDATE` query; every allocation writer acquires payment-row locks before reading sums); rewritten `RecordPaymentAllocation` (running per-obligation totals, currency guard, exception model), `ReversePaymentAllocation`, `RecordChequeStatusChange` (locked, re-read cascade); `AllocationSettlement.DeriveStatus` shared helper; soft-delete filters on write-path loads; `amount_paid`/`due_date_status` made app-maintained (removed `ValueGeneratedOnAddOrUpdate`); `IssueReceipt` returns the receipt + explicit `AddReceiptAsync` (both repositories); migration `20260726150948_Module6_PaymentAllocationIntegrityTriggers` — `trg_payment_allocations_enforce_limits` (BEFORE INSERT OR UPDATE; obligation side with `adjustment` exemption + receiving side; ERRCODE 23514); migration `20260726151426_Module6_AmountPaidAppMaintained` (snapshot sync, schema no-op).
(2nd increment — in flight via workflow) Exception sweep of remaining 13 handlers + pagination validators + explicit 3-dp rounding; new commands `RecordManualRentPayment` (incl. cheque registration + optional immediate allocation), `IssueRentPaymentReceipt`, `CancelRentPayment` with validators and unit tests.

## Files / Areas Changed
Domain: 8 Financials entities (IDs), `RentPayment.IssueReceipt` signature. Application: 3 allocation/cheque handlers rewritten, `AllocationSettlement.cs`, `IRentPaymentRepository` (+2 methods), `IExpenseRepository` (+1). Infrastructure: `RentPaymentRepository` (locking + filters), `ExpenseRepository`, `RentPaymentConfiguration`, 2 migrations. Tests: 6 unit fake updates, `PaymentAllocationIntegrityTests` (5 tests incl. concurrency), `SqlLoggingExtensions` (opt-in EF SQL capture via `PROPERTYOS_TEST_SQL_LOG`), fixture hook.

## Database Impact
Two new migrations, **generated but NOT applied to the local AqariOS DB** (user applies; Testcontainers applies automatically in integration runs): the allocation-limits trigger and an empty snapshot-sync migration. No schema changes beyond the trigger/function.

## Security Review
No new endpoints yet. Cross-tenant reads masked as 404 in rewritten handlers. RLS untouched; locking queries filter `deleted_at` and operate under existing tenant policies. Permission constants for the coming API exist in the catalog (`payments.approve`, `expenses.create`, `expenses.approve`, `receipts.issue`).

## Multi-Tenancy / RLS Review
Allocation writers operate inside `TransactionBehavior` transactions → `TenantSessionInterceptor` sets RLS context. `company_id` denormalization untouched. RLS integration coverage for the remaining financial tables is tracked for Phase 12.

## Transaction Review
All flows remain single-transaction via `TransactionBehavior` (nested commands join the outer transaction — verified by the callback rollback integration test, which now exercises the true path: gateway status + unallocated payment + allocation + receipt + sequence counter all roll back together).

## Performance Review
Lock scope is per-involved-payment rows only, sorted to prevent deadlocks. Batch totals kept in memory (no per-item re-query). The doc's write-path-critical indexes (`idx_payment_allocations_obligation_payment_id` etc.) pre-exist. Pagination validators being added for the 3 unbounded paged queries.

## Tests Added / Updated
`PaymentAllocationIntegrityTests`: trigger rejects direct-SQL over-allocation (obligation + receiving sides), tracked-entity persistence diagnostic, single-allocation cache persistence, **concurrent allocators — exactly one succeeds and `amount_paid == SUM(active allocations) <= amount_due`**. Callback rollback test now meaningful. Exception-type updates across allocation/cheque tests. Wave-1 workflow adds tests for the three new commands.

## Verification Results
Final (all increments integrated — waves 1/2, grace-matrix refit, jobs, API, wiring): `dotnet build PropertyOS.sln` **0 warnings / 0 errors**; unit **646/646 passed**; integration **110 passed / 0 failed / 4 pre-existing Module 3 skips**. Includes: direct-SQL trigger rejection tests, exactly-one-wins concurrency test, callback rollback test exercising the real settlement path, §6.1 grace-matrix unit tests (`AllocationSettlementTests`), and 30+ new job/command tests.

## Completion Addendum
All planned items landed and verified: 3 background jobs (installment generation daily 01:00 Amman, overdue marking daily 00:45, eFAWATEERcom expiry hourly — registered in Program.cs, DI-wired with config-driven staleness window); `MarkRentPaymentOverdue` command using the shared locking protocol; the full Modules 6–7 API (6 controllers, 14 request models, 4 authorization policies on catalog permissions); and the §6.1 **grace-period-aware status matrix** — `AllocationSettlement.DeriveStatus(paid, due, dueDate, today, graceDays)` with `rent_grace_period_days` fetched per company, `OverdueUnpaid` (zero-paid past grace) vs `Late` (partially-paid past grace), null-due-date rows never late/overdue, and sticky `Cancelled` guarded at every recompute site (reversal, bounce cascade, sweep).

## Remaining Issues
- Bounce fee recorded on the cheque but not receivable (no `Adjustment` row generated) — product decision documented, not implemented.
- Real eFAWATEERcom gateway (outbound initiation, real payload contract) — external blocker; webhook ships fail-closed (503 without configured secret).

## Phase Result
COMPLETE — build 0W/0E, unit 646/646, integration 110/0.

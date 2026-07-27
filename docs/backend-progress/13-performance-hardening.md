# Phase 13 — Performance Hardening

Scale anchors from the approved docs: rent_payments 2M+, notification_deliveries 150M+, notifications ~tens of millions; keyset-over-OFFSET is the platform convention.

## Findings Fixed
1. **Unbounded reads** (worst offenders): empty-term payment search (whole-table materialization), outstanding payments, cheques-by-status, contract search/histories, company/user notification lists, failed deliveries (150M table, previously change-tracked entities) — all now `AsNoTracking`, DTO-projected, capped (≤200) with stable ordering/keyset cursors aligned to the pre-designed indexes (`idx_notification_deliveries_company_status_sent_at`, `idx_notifications_recipient_created`, …).
2. **Dispatch-candidate query**: the OR/EXISTS predicate (unindexable, executed per company every 5 minutes) split into two index-friendly ID queries (Pending; retryable-Failed via deliveries) unioned under the cursor contract.
3. **Sweep-job exclusion sets**: the `NOT IN (attempted…)` lists grew with the attempted set within a sweep — all five jobs converted to keyset cursor advancement (`Id > afterId`), with only the small poison set retained client-side.
4. **Installment generation N+1**: the per-billing-period `EXISTS` probe (13 round trips per annual contract) replaced with a single existing-periods query diffed in memory.
5. **Startup**: permission-catalog reconciliation rewrote from O(companies × permissions) tracked-entity loading to set-based SQL (anti-join `INSERT … ON CONFLICT DO NOTHING`) — converged systems do no per-row work at boot.

## Verified Clean (audit evidence)
Maintenance list (true keyset + trigram-friendly ILIKE), all four Marketplace list queries (keyset, hard 50-cap, SQL-translated image subqueries — no N+1), Financials receipt/expense/eFAWATEERcom paged reads, allocation write path (running totals — no per-item re-query), FOR UPDATE lock scope (involved rows only, sorted). Recurring-job registration at startup is O(jobs).

## Deliberately Not Done
- No speculative micro-optimization (per mission §12): no caching layers, no read replicas, no batch-size tuning without measurements.
- rent_payments/notifications RANGE partitioning: designed in the docs for future scale; deferred until operationally warranted (the query patterns and indexes are already partition-aligned).
- Channel-provider I/O inside the dispatch transaction: acceptable while providers are local; flagged for claim-then-send redesign when network gateways land (see 12-security-hardening.md).

## Phase Result
COMPLETE (final suite timings/numbers in `14-production-readiness.md`).

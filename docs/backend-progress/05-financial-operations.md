# Phase 05 — Financial Operations (Module 7)

Module 7 shares the `Financials` code family with Module 6; the allocation/settlement core is documented in `04-rent-payments.md`. This report covers the Module 7 specifics: expenses, receipts, the per-company receipt sequence, and eFAWATEERcom.

## Initial State
Domain/Application/Infrastructure complete; no API. Receipt numbering already implemented correctly: `CompanyReceiptSequenceRepository.ReserveAndFormatNextReceiptNumberAsync` is a single atomic `UPDATE … RETURNING` with reset-policy handling inside the statement — race-free by row lock, transaction-enlisted (counter rolls back with the business transaction), backstopped by partial unique indexes on `(company_id, receipt_number)` for both receipt tables. Verified by pre-existing concurrency integration tests.

## Requirements Reviewed
`docs/PropertyOS_Module7_FinancialOperations_Updated.md` §7.1–7.5 (expenses, expense_receipts, rent_payment_receipts, company_receipt_sequences, efawateercom_transactions), §7.8 security.

## Problems Found
1. The eFAWATEERcom callback settlement path was production-fatal (Guid.Empty dispatch — fixed in Phase 04's core; the callback now runs end-to-end in the integration rollback test).
2. Receipts created inside aggregates were tracked as Modified (fixed: explicit `AddReceiptAsync` on both repositories).
3. `CreateExpense`/`AttachExpenseReceipt` returned `Guid.Empty` to callers (fixed by client-generated IDs).
4. BCL exceptions → 500s across all Module 7 handlers (fixed in Wave 1: NotFound/BusinessRule codes — `EXPENSE_RECEIPT_DUPLICATE_FILE`, `EFAWATEERCOM_INVALID_TRANSITION`, `RECEIPT_ISSUE_INVALID_STATE`).
5. Unbounded `PageSize` on 3 paged queries (fixed: validators, 1–200).
6. `IEfawateercomGateway` has no outbound "initiate payment" operation and only a Null implementation — REMAINS by design until real gateway credentials/specs exist; the poll path is a safe no-op.
7. No webhook endpoint existed; the incoming-callback trust model was undefined (Wave 2 adds a fail-closed HMAC-verified webhook: 503 when no `Efawateercom:WebhookSecret` configured, 401 on bad signature; payload contract documented as provisional).
8. No transaction-expiry sweep (Wave 2 adds the job).
9. `CreateExpenseCommandHandler` hardcodes `"JOD"` currency — accepted for now (platform default; single-currency market), noted for a future multi-currency pass.

## Implementation Completed
See `04-rent-payments.md` for the shared core. Module 7 specific: exception sweep + validators (Wave 1); expenses/receipts/sequence/eFAWATEERcom controllers, webhook, `FinancialsPermissions`, expiry job (Wave 2, integrating).

## Files / Areas Changed
Wave 1: 12 Module 7 handler files + 3 validators + 2 test files. Wave 2 (integrating): `Api/Financials/*`, `Api/Models/Financials/*`, `Application/Financials/Security/FinancialsPermissions.cs`, `Infrastructure/Financials/Jobs/*`.

## Database Impact
None beyond Phase 04's two generated migrations. Receipt-sequence schema untouched.

## Security Review
Permissions: `expenses.create`, `expenses.approve`, `receipts.issue`, `payments.approve` (all pre-existing in the catalog; policies wired at integration). Webhook is anonymous-but-signed, fail-closed without configuration. Receipt issuance requires full settlement (domain-enforced) and locks the payment row against double issuance; uniqueness backstopped at DB level.

## Multi-Tenancy / RLS Review
All 8 financial tables have RLS tenant policies (Module 6/7 consolidated migration). RLS integration-test coverage exists for `expenses` + `company_receipt_sequences`; the remaining financial tables are queued for the Phase 12 RLS test sweep.

## Transaction Review
Receipt number reservation shares the business transaction (proven by rollback test). Nested allocation from the callback joins the outer transaction. Jobs dispatch per-item commands so TransactionBehavior owns every mutation.

## Performance Review
Keyset pagination on all three list queries, now bounded. Sequence counter is one-row-per-company — no contention concern beyond the intended serialization.

## Tests Added / Updated
Wave 1 test updates (exception types, rounding); callback rollback test now exercises the true path; receipt-sequence concurrency tests pre-existing. Wave 2 adds job tests.

## Verification Results
Final: build **0 warnings / 0 errors**; unit **646/646**; integration **110/0** (4 pre-existing skips). Expenses/receipts/sequence/eFAWATEERcom controllers live; webhook fail-closed verified by code review (secret unset → 503; HMAC-SHA256 FixedTimeEquals).

## Remaining Issues
- Real eFAWATEERcom gateway integration (outbound initiation, real payload contract, credentials) — external blocker, tracked.
- Multi-currency expenses.
- Amount-vs-outstanding verification policy for callbacks (currently allocates `min` implicitly via allocation guards; over-amount callbacks fail the allocation → transaction stays Success with unallocated remainder on the receipt row — acceptable, documented).

## Phase Result
COMPLETE — build 0W/0E, unit 646/646, integration 110/0. Remaining external-blocker items (real gateway integration) tracked above.

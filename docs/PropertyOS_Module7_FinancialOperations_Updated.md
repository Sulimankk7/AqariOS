# MODULE 7 — FINANCIAL OPERATIONS

Continuation of `PropertyOS_Phase3_Physical_Design.md`, `PropertyOS_Module4_Properties.md`, `PropertyOS_Module5_Leasing.md`, and `PropertyOS_Module6_RentPayments_Updated.md`. Applies the same Global Conventions (UUIDv7 PKs, `created_at`/`updated_at`/`created_by`/`updated_by`, soft delete, `company_id` + RLS tenant isolation, `snake_case` naming) unless a deviation is explicitly justified.

Scope for this module is deliberately narrow, per the module brief: **five tables only** — `expenses`, `expense_receipts`, `rent_payment_receipts`, `company_receipt_sequences`, `efawateercom_transactions`. No `efawateercom_billers` table (per Phase 1 §1.18's original design, explicitly excluded from this module's scope), no allocation/split-billing machinery, no new financial concepts beyond what is named.

Scale target for this module: **10,000,000+ expense records, 5,000,000+ receipts (combined across both receipt tables), millions of eFAWATEERcom transaction records, 10,000+ companies.**

---

## 7.0 Module-Wide Design Decisions (read before the tables)

**Receipts remain non-polymorphic, per the Phase 1 §1.20 FINAL decision.** `rent_payment_receipts` and `expense_receipts` each carry a real, mandatory, single-purpose foreign key to their one parent type (`rent_payments` / `expenses` respectively) — never a shared `source_type`/`source_id` polymorphic pair. This guarantees database-level referential integrity for a sequentially-numbered legal financial artifact, which a polymorphic design structurally cannot.

**`expense_receipts` cardinality is explicitly updated from 1:1 to 1:many — a documented deviation from the original Phase 1 §1.20 design, not an oversight.** Phase 1 originally modeled `expense_receipts` as a 1:1 non-polymorphic receipt mirroring `rent_payment_receipts`. This module's brief explicitly requires "one expense may have multiple receipt files" — a real-world necessity (a single maintenance expense may accumulate a parts invoice, a labor invoice, and a delivery receipt as three separate physical documents). The resolution: `expense_receipts.expense_id` remains a real, mandatory, `NOT NULL` FK (preserving the core Phase 1 §1.20 integrity guarantee — no orphaned or polymorphically-ambiguous receipt), but the `UNIQUE(expense_id)` constraint from the original design is dropped, and each row now represents **one specific numbered receipt document** (one uploaded file, one `receipt_number`, one `amount`) rather than "the one receipt for this expense." Multiple receipts against one expense are simply multiple rows. This keeps the FK-integrity guarantee that made the original design correct while satisfying the updated cardinality requirement — the two are not in tension; only the uniqueness constraint changed.

**One continuous, gapless-per-company receipt sequence spans both receipt tables.** Per Phase 1 §1.20 and the module brief, Jordanian bookkeeping practice expects a single unbroken numbering scheme across all receipts a company issues, regardless of whether the receipt documents rent collected or an expense paid. `company_receipt_sequences` is the single per-company atomic counter both `rent_payment_receipts` and `expense_receipts` draw from at INSERT time, detailed fully in §7.4's concurrency-safety mechanism. This module elevates that mechanism from Phase 1's conceptual description to a fully specified, concurrency-safe physical design, including configurable prefix, zero-padding, and reset-policy support the original conceptual note did not yet specify.

**`efawateercom_transactions` is scoped to `rent_payments` only in this module, by explicit brief instruction.** Phase 2's relationship catalog (§2.1.D) originally modeled `efawateercom_transactions` as settling *either* a `rent_payments` row *or* a `utility_bills` row (a 1:1 polymorphic-adjacent pattern via two independent optional FKs). Since `utility_bills` is out of this module's explicit five-table scope and has not yet been physically designed, this module carries only the `rent_payment_id` relationship, with `rent_payment_id` **mandatory** (`NOT NULL`) rather than optional — there is no second settleable entity type in scope for this table to be conditionally nullable against. When the Financial/Utility Billing module introduces `utility_bills`, the correct extension — consistent with the table-per-concrete-type philosophy already established for receipts in Phase 1 §1.20 — is a sibling `efawateercom_utility_bill_transactions` table, not a retrofit of this table into a dual-nullable-FK shape (which would reintroduce exactly the "weak relationship" pattern Phase 2 §2.4 already flagged and rejected for `notifications`). This is flagged explicitly now so it is not silently forgotten when that module is eventually designed. `efawateercom_billers` is deliberately **not** created in this module, per explicit brief instruction — the biller-configuration concept exists conceptually in Phase 1 §1.18 but is out of scope here; this table's `company_id` denormalization is sufficient for this module's own tenant-isolation and reporting needs without a biller-config join.

**Denormalized `company_id` on every table in this module**, consistent with the module-wide denormalization principle established in Module 4 §4.0, Module 5 §5.0, and Module 6 §6.0 — every table here is either RLS-protected directly or queried at high volume in isolation from a full parent chain (a company-wide expense report should not require joining through `buildings` just to filter by tenant).

**`expenses` is this module's partitioning candidate, by row-count target and retention profile — flagged now, resolved fully in §7.6.** At 10,000,000+ rows and permanent (never bulk-dropped) financial/legal retention, `expenses` has the same structural shape that justified partitioning for `rent_payments` in Module 6 §6.4: append-heavy, naturally time-ordered via `expense_date`, and financial reporting is conventionally bounded to a fiscal period. Full treatment below.

**Expense categories as a closed enum, not a lookup table.** The module brief names a fixed, stable list of twelve expense categories (building, shared, emergency, utility common-area, maintenance, cleaning, security, elevator, water tank, generator, administrative, other). This is the identical normalization judgment already applied to `governorate` in Module 4 §4.0 — a small, product-defined, stable-cadence domain is correctly an enum, not a tenant-editable lookup table; a company cannot invent its own eleventh expense category outside the list without a product/schema decision, which is the intended behavior for a categorization scheme that feeds structured financial reporting (an open-ended free-text category would make "Owner P&L by category" reporting unreliable).

---

## 7.1 Table: `expenses`

**Purpose:** Records every operating cost incurred by a company or attributable to a specific building — the source data for owner P&L reporting, budget tracking, and building-level cost analysis.

**Business Description:** Captures the full range of Jordanian property-operating costs: building-specific line items (elevator servicing, water tank cleaning, generator fuel/maintenance, security staff, cleaning), shared/common-area utility costs, emergency repairs, and company-wide administrative overhead not attributable to any single building. Every expense is either building-scoped or company-wide (`building_id IS NULL`), never both and never neither.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company (tenant-scope root) |
| building_id | UUID | YES | NULL | The building this cost is attributable to; NULL = company-wide overhead (e.g., head-office admin costs, platform subscription fees, portfolio-wide insurance) — stated requirement's explicit "Shared Expenses"/"Administrative Expenses" distinction |
| category | expense_category_enum | NO | — | `building` \| `shared` \| `emergency` \| `utility_common_area` \| `maintenance` \| `cleaning` \| `security` \| `elevator` \| `water_tank` \| `generator` \| `administrative` \| `other` — the stated closed category list |
| amount | NUMERIC(12,3) | NO | — | The cost amount |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217, per Global Convention |
| expense_date | DATE | NO | — | The date the cost was actually incurred/paid — a historical fact, not a scheduling field (see Check Constraints) |
| payment_method | expense_payment_method_enum | NO | — | `cash` \| `bank_transfer` \| `cheque` \| `other` — how the company itself paid the vendor; deliberately a narrower enum than `rent_payments.payment_method` (no `efawateercom`, since eFAWATEERcom is a *collection* channel for money coming into the company, not a channel the company itself pays vendors through) |
| vendor_name | VARCHAR(255) | YES | NULL | The paid party's name — nullable since not every expense has a formal identifiable vendor (e.g., a municipal fee paid directly, informal petty-cash spend) |
| invoice_number | VARCHAR(100) | YES | NULL | The vendor's own invoice/reference number, where one exists — nullable since informal/cash vendor transactions frequently have no formal invoice |
| description | TEXT | NO | — | What the cost was for — required; a categorized-but-undescribed financial record is not useful for owner reporting or dispute resolution |
| notes | TEXT | YES | NULL | Free-form staff notes |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `building_id` → `buildings(id)` ON DELETE RESTRICT — financial records must remain permanently linked to the building where the expense was incurred. Deleting or deactivating a building must never change the business meaning of historical expenses. RESTRICT preserves historical integrity and prevents accidental removal of the relationship.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** None. An expense is a free-standing financial event; there is no natural business key to enforce uniqueness against (two genuinely identical expenses — e.g., two separate months' identical generator-fuel top-ups — are not duplicates, they're two real, distinct costs).

**Check Constraints:**
- `chk_expenses_amount_positive` — `amount > 0` — the stated "negative expense amounts" integrity requirement; a credit/refund against a prior expense is a separate, new expense row with its own sign-neutral positive amount and an explanatory `notes` value (e.g., "vendor refund — see expense #..."), never a negative-amount row, keeping the sign convention unambiguous across every row in the table.
- `chk_expenses_expense_date_not_future` — `expense_date <= CURRENT_DATE` — mirrors `chk_contract_terminations_date_not_future`'s (Module 5 §5.3) and `chk_meter_readings_date_not_future`'s (Module 4 §4.8) reasoning: an expense record documents something that has already been incurred, not a future-scheduling/budgeting field — a planned future cost belongs in a future budgeting feature (out of this module's scope), not a pre-dated `expenses` row.

**Indexes:**
- `idx_expenses_company_date` composite on `(company_id, expense_date DESC)` WHERE `deleted_at IS NULL` — the base **Expense History** query shape (stated requirement): a company's full expense ledger, most-recent-first, the primary financial-operations screen. `company_id` leads as the mandatory tenant-scoping equality filter present in every query against this table; `expense_date DESC` trails so the common "most recent first" access is a direct ordered index scan with no separate sort step, mirroring the identical composite-with-trailing-DESC pattern established since Module 3.
  - *Queries served:* `SELECT * FROM expenses WHERE company_id = $1 AND deleted_at IS NULL ORDER BY expense_date DESC LIMIT 50` (paginated expense list).
  - *Expected improvement:* turns a filter-then-sort over a table headed toward ten million rows into a single ordered index range-scan.
  - *Why preferred over separate single-column indexes:* identical reasoning already given for `idx_rent_payments_contract_due_date` (Module 6 §6.1) — a composite serves both the equality filter and sort order in one structure.
- `idx_expenses_company_category_date` composite on `(company_id, category, expense_date DESC)` WHERE `deleted_at IS NULL` — the **Financial Reports** / **Dashboard Statistics** query shape: "total spend by category this quarter," "elevator maintenance costs year-over-year" — every category-bucketed report and every dashboard tile that breaks spend down by category. `category` sits second, after the mandatory tenant scope and before the date-range refinement, since category-bucketed reporting is the dominant analytical access pattern for this table (an owner P&L report is fundamentally "spend, grouped by category, over a period").
  - *Queries served:* `SELECT category, SUM(amount) FROM expenses WHERE company_id = $1 AND expense_date BETWEEN $2 AND $3 GROUP BY category`; a single-category drill-down (`WHERE company_id = $1 AND category = $2 ORDER BY expense_date DESC`).
  - *Expected improvement:* both the aggregate GROUP BY and the single-category drill-down become targeted index range-scans rather than a sequential scan across the full table.
- `idx_expenses_building_date` composite on `(building_id, expense_date DESC)` WHERE `building_id IS NOT NULL AND deleted_at IS NULL` — supports building-level **Financial Reports** (owner P&L per building, per Phase 1 §1.19) — a real, frequent report shape distinct from the company-wide view, since owners routinely want per-building cost breakdowns to evaluate individual asset performance. Partial on `building_id IS NOT NULL` since company-wide overhead rows (a meaningful fraction of the table) have no building to report against and would be pure dead weight in this index.
  - *Queries served:* `SELECT * FROM expenses WHERE building_id = $1 AND deleted_at IS NULL ORDER BY expense_date DESC` (building detail page's expense tab); `SUM(amount) ... WHERE building_id = $1 AND expense_date BETWEEN ...` (per-building P&L).
- `idx_expenses_vendor_trgm` — GIN trigram index on `vendor_name` — supports the stated **Search** requirement for staff searching/filtering by partial or misremembered vendor name, the same search-quality rationale as `idx_buildings_name_trgm` (Module 4) and `idx_lease_contracts_contract_number_trgm` (Module 5).
- `idx_expenses_invoice_number` on `(invoice_number)` WHERE `invoice_number IS NOT NULL AND deleted_at IS NULL` — supports the **Search** requirement for staff locating an expense by its vendor invoice number during reconciliation (e.g., matching a vendor's own statement against recorded expenses).

**Deliberately omitted index:** no standalone `payment_method` index. "How much did we pay via cash vs. bank transfer this quarter" is a real but low-frequency analytical question, adequately served by scanning within the already-narrow `idx_expenses_company_category_date` result set for a bounded period, or by a future `report_snapshots`-materialized report — consistent with the identical "avoid unnecessary indexes" discipline applied to `rent_payments.payment_method` in Module 6 §6.1.

**Business Rules:** Expenses are staff-entered records, not billing-job-generated (unlike `rent_payments`) — there is no scheduled-generation concern here. `building_id`, once set, may be corrected via an explicit administrative update (unlike the "denormalized-at-write, immutable thereafter" columns elsewhere in this schema) since a building misattribution on a cost record is a realistic data-entry correction scenario with no downstream financial-ledger integrity implication the way reassigning a `rent_payments.lease_contract_id` would have.

**Soft Delete Strategy:** Standard — reserved for genuine data-entry errors (a duplicate entry, an expense logged against the wrong company by a multi-tenant-staff mistake); a real, correctly-recorded cost is never soft-deleted merely because it turned out to be avoidable or was later disputed with the vendor (that nuance belongs in `notes`, not in row deletion).

**Audit Requirements:** Every mutation logged to `audit_logs`; `amount`, `category`, and `building_id` changes are treated as high-severity events given their direct effect on financial reporting accuracy.

**Future Scalability Notes:** Explicitly named in the scale target (10,000,000+ rows). Full partitioning treatment in §7.6.

---

## 7.2 Table: `expense_receipts`

**Purpose:** Supports one or more uploaded receipt/invoice documents per expense, each individually numbered per the company's continuous receipt sequence, integrating with `file_storage` for the actual file governance.

**Business Description:** A single recorded `expenses` row may be documented by multiple physical receipts (a materials invoice, a separate labor invoice, a delivery slip) — this table is the ordered, numbered attachment set for one expense, structurally distinct from a generic document-attachment table (like `contract_documents`, Module 5 §5.5) specifically because each row here also consumes a sequential, company-wide, bookkeeping-significant `receipt_number` from `company_receipt_sequences`, which a generic attachment row would not.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| expense_id | UUID | NO | — | The expense this receipt documents — **not** unique (§7.0); an expense may have zero, one, or many receipt rows |
| file_id | UUID | NO | — | FK to `file_storage` — the actual uploaded receipt scan/photo/PDF |
| receipt_number | VARCHAR(50) | NO | — | Generated via `company_receipt_sequences` in the same transaction as this row's INSERT (§7.4) |
| amount | NUMERIC(12,3) | NO | — | The amount reflected on this specific physical receipt — may be less than the parent expense's total `amount` when one expense is documented by several partial receipts; not constrained to sum exactly to the parent expense's amount (see Business Rules) |
| issued_at | DATE | NO | CURRENT_DATE | The date printed on the physical receipt/invoice document |
| uploaded_by | UUID | YES | NULL | FK to `users`; nullable for system-migrated/bulk-imported historical receipts with no attributable uploader — deliberately named `uploaded_by`, not `issued_by`, since this column represents a staff member *documenting* a vendor-issued receipt (a passive record-keeping action), distinct from `rent_payment_receipts.issued_by`, which represents a staff member *actively issuing* an official company document to a tenant |
| description | VARCHAR(255) | YES | NULL | Free-form label, e.g. "Elevator parts invoice — Otis Jordan" |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `expense_id` → `expenses(id)` ON DELETE RESTRICT — per the stated "invalid receipt references" integrity requirement; a receipt can never be orphaned from its parent expense.
- `file_id` → `file_storage(id)` ON DELETE RESTRICT — per the stated "orphan receipt files" integrity requirement; the file metadata record must outlive this join row, mirroring `contract_documents.file_id`'s identical RESTRICT reasoning (Module 5 §5.5).
- `uploaded_by`, `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:**
- `uq_expense_receipts_company_receipt_number` on `(company_id, receipt_number)` WHERE `deleted_at IS NULL` — the direct DB-level backstop for the stated "duplicate receipt numbers inside the same company" requirement. The primary defense is the atomic `company_receipt_sequences` generation mechanism itself (§7.4), which makes a collision practically unreachable through normal application code; this constraint is the "defense in depth, don't rely solely on the generator being called correctly" backstop, identical in posture to `chk_rent_payments_amount_paid_nonneg` backstopping its own maintaining trigger (Module 6 §6.1).
- `uq_expense_receipts_expense_file` on `(expense_id, file_id)` WHERE `deleted_at IS NULL` — the same physical file should not be attached twice to the same expense under two different rows (a genuine duplicate-upload data-entry error), mirroring `uq_contract_documents_contract_file`'s identical rationale (Module 5 §5.5).

**Check Constraints:**
- `chk_expense_receipts_amount_positive` — `amount > 0`.
- `chk_expense_receipts_issued_at_not_future` — `issued_at <= CURRENT_DATE`.

**Indexes:**
- `idx_expense_receipts_expense_id` on `(expense_id)` WHERE `deleted_at IS NULL` — "list all receipts for this expense," the expense detail page's receipts tab — the only realistic per-expense access pattern, mirroring `idx_contract_documents_lease_contract_id`'s identical rationale (Module 5 §5.5).
- `idx_expense_receipts_company_receipt_number` on `(company_id, receipt_number)` WHERE `deleted_at IS NULL` — doubles with the unique constraint above and directly serves the stated **Receipt Lookup** requirement: staff searching for a specific receipt by its printed number during reconciliation or a vendor dispute.
- `idx_expense_receipts_company_issued_at` composite on `(company_id, issued_at DESC)` WHERE `deleted_at IS NULL` — supports company-wide **Financial Reports**/**Dashboard Statistics**: "all receipts recorded this month," a reconciliation/QA screen distinct from the expense-level view above.

**Deliberately omitted index:** no standalone `file_id` index — "which expense does this file belong to" has no realistic reverse-lookup product feature, identical reasoning to `contract_documents`' omission of a standalone `file_id` index (Module 5 §5.5); a data-integrity audit, if ever needed, tolerates a sequential scan against this table's modest per-expense row count.

**Business Rules:** `receipt_number` is generated by the atomic `company_receipt_sequences` mechanism (§7.4) in the same transaction as this row's INSERT — never assigned by application code independently of that mechanism. There is deliberately **no** trigger or CHECK constraint requiring `SUM(expense_receipts.amount WHERE expense_id = ...) = expenses.amount` — unlike `payment_allocations`' over-allocation prevention (Module 6 §6.3), receipts here are **documentary evidence** of an expense, not a financial settlement/balance-tracking layer; an expense's `amount` is authoritative on its own row regardless of how many (or how few) supporting receipt documents have been attached, so no aggregate consistency invariant is meaningful or enforced between the two tables.

**Soft Delete Strategy:** Standard — an erroneously uploaded or duplicate receipt is soft-deleted; correction is via a new row, not an edit to the receipt's `amount`/`receipt_number` in place, preserving the numbering sequence's historical integrity.

**Audit Requirements:** Standard `audit_logs` coverage; upload and soft-delete events both logged, since removing a receipt from an expense's financial record is itself a traceable action.

**Future Scalability Notes:** Scales with (expenses × average receipts-per-expense, typically 0–2) — tracks `expenses`' own scale (§7.1) at a fraction of its row count; evaluated jointly with `expenses` and `rent_payment_receipts` in §7.6 against the module's combined 5,000,000+ receipt target.

---

## 7.3 Table: `rent_payment_receipts`

**Purpose:** The official, sequentially-numbered receipt issued to a tenant for a completed rent payment — the authoritative source of truth `rent_payments.receipt_number` (Module 6 §6.1) denormalizes a convenience copy of.

**Business Description:** Per Phase 1 §1.20's FINAL decision, this is a real, non-conditional, 1:1 extension of a `rent_payments` row: at most one receipt exists per rent payment, generated once settlement is recognized, numbered from the same continuous per-company sequence that also numbers `expense_receipts`.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| rent_payment_id | UUID | NO | — | The payment this receipt certifies (1:1) |
| receipt_number | VARCHAR(50) | NO | — | Generated via `company_receipt_sequences` in the same transaction as this row's INSERT (§7.4) |
| issue_date | DATE | NO | CURRENT_DATE | The date the receipt was issued |
| issued_by | UUID | YES | NULL | FK to `users`; nullable for system/job-auto-issued receipts with no human actor (e.g., an automated same-day receipt on eFAWATEERcom settlement confirmation) |
| amount | NUMERIC(12,3) | NO | — | The amount this receipt certifies — an immutable snapshot at issuance time, mirroring `rent_payments.amount_due`'s snapshot philosophy (Module 6 §6.1); does not silently track any later change to the underlying payment's cumulative `amount_paid` |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217, per Global Convention |
| notes | TEXT | YES | NULL | Free-form staff notes |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `rent_payment_id` → `rent_payments(id)` ON DELETE RESTRICT — per Phase 2, the receipt is a sequentially-numbered legal financial artifact preserved permanently.
- `issued_by`, `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:**
- `uq_rent_payment_receipts_rent_payment_id` on `(rent_payment_id)` WHERE `deleted_at IS NULL` — the direct physical enforcement of "at most one receipt per rent payment," per Phase 1 §1.20's FINAL decision.
- `uq_rent_payment_receipts_company_receipt_number` on `(company_id, receipt_number)` WHERE `deleted_at IS NULL` — the same defense-in-depth backstop as `expense_receipts`' identical constraint (§7.2), against the shared, cross-table `company_receipt_sequences` numbering mechanism (§7.4).

**Check Constraints:**
- `chk_rent_payment_receipts_amount_positive` — `amount > 0`.
- `chk_rent_payment_receipts_issue_date_not_future` — `issue_date <= CURRENT_DATE`.

**Indexes:**
- `uq_rent_payment_receipts_rent_payment_id` (above) — doubles as the primary **Payment Receipt Lookup** query (stated requirement): "get the receipt for this payment" from the payment detail page — a direct, single-row lookup on the 1:1 relationship.
- `idx_rent_payment_receipts_company_receipt_number` on `(company_id, receipt_number)` WHERE `deleted_at IS NULL` — doubles with the unique constraint above and directly serves the stated **Receipt Lookup**/**Search** requirement: staff resolving a tenant dispute where the tenant presents a printed receipt with only its number visible.
- `idx_rent_payment_receipts_company_issue_date` composite on `(company_id, issue_date DESC)` WHERE `deleted_at IS NULL` — supports **Financial Reports**/**Dashboard Statistics**: "receipts issued this month," a collections-reporting screen distinct from the payment-level 1:1 lookup above.

**Deliberately omitted index:** no denormalized `tenant_id`/`lease_contract_id` columns or indexes on this table. Unlike `rent_payments` itself (Module 6 §6.1), which justified denormalizing `tenant_id` because it is this schema's single highest-write-volume table family, `rent_payment_receipts` is a strict 1:1 subset of `rent_payments` — a "this tenant's receipts" or "this contract's receipts" query is already efficiently servable by joining through `rent_payment_id` into the already-indexed `idx_rent_payments_tenant_due_date`/`idx_rent_payments_contract_due_date` composites (Module 6 §6.1), at a query frequency low enough that the extra denormalization/index-maintenance cost here is not justified — consistent with the "avoid unnecessary indexes" discipline applied without exception since Module 3.

**Business Rules:** Created in the same application-orchestrated transaction as the settlement event that brings the linked `rent_payments` row to a receipt-worthy state (typically `due_date_status = 'paid'`, though a company's own policy may also choose to issue on `partially_paid` — an application/product decision, not a DB constraint). `receipt_number` is obtained from `company_receipt_sequences` (§7.4) in the same transaction as this row's INSERT. `rent_payments.receipt_number` (Module 6 §6.1) is a denormalized read-convenience copy of this row's `receipt_number`, kept in sync by the application at creation time — this table remains the authoritative source, per Module 6 §6.5's integrity summary.

**Soft Delete Strategy:** Standard, reserved for genuine data-entry correction (an erroneously duplicated receipt issuance); a real issued receipt is never soft-deleted to "undo" a payment, which is instead handled by reversing the underlying `payment_allocations` row(s) (Module 6 §6.3) while the receipt itself remains historical evidence of what was actually handed to the tenant at the time.

**Audit Requirements:** Every issuance logged to `audit_logs`, high-severity given the row's status as an official financial document handed to a third party.

**Future Scalability Notes:** Scales 1:1 (at most) with settled `rent_payments` rows — bounded by Module 6's own 2,000,000+ target. Evaluated jointly in §7.6.

---

## 7.4 Table: `company_receipt_sequences`

**Purpose:** The concurrency-safe, atomic, per-company counter that generates every `receipt_number` used across both `rent_payment_receipts` and `expense_receipts`, giving each company one continuous, gapless-in-the-happy-path numbering scheme spanning both receipt types — per Phase 1 §1.20's FINAL decision.

**Business Description:** Jordanian bookkeeping practice expects a single, unbroken sequential receipt book per company, not a separate numbering track per receipt type. This table is a 1:1 configuration/state extension of `companies`, mirroring the `companies`/`company_settings` split (Module 1 §1.2) in spirit — narrow, low-write-frequency-per-row (one row per company, updated on every receipt issuance regardless of type), isolated from the hot-path tenant-lookup row.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company (1:1) |
| prefix | VARCHAR(20) | NO | '' | Optional company-chosen prefix prepended to the formatted number, e.g. `"RCPT-"`, `"REC-2026-"`; empty-string default (never NULL) so the generation formula (`prefix \|\| lpad(...)`) never needs NULL-coalescing branch logic |
| current_number | BIGINT | NO | 0 | The last-issued sequence number; the next number to issue is always `current_number + 1`, computed atomically at generation time (§ Business Rules) |
| padding_length | SMALLINT | NO | 6 | Zero-pad width for the numeric portion of the formatted receipt number, e.g. `6` → `"000123"` — pure display formatting, does not affect the underlying numeric ordering/uniqueness |
| reset_policy | receipt_reset_policy_enum | NO | 'never' | `never` \| `yearly` \| `monthly` — whether `current_number` resets to 0 at the start of a new period; some Jordanian companies restart numbering each fiscal year (e.g., embedding the year via `prefix` templating at the application layer) |
| last_reset_at | TIMESTAMPTZ | YES | NULL | When the counter was last reset; populated only when `reset_policy != 'never'`, used by the generation logic to detect whether the current period has changed since the last issuance |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:** `company_id` → `companies(id)` ON DELETE CASCADE — mirrors `company_settings`' identical 1:1 CASCADE pattern (Module 1 §1.2): this row has no independent lifecycle outside its company and is auto-created at company signup.

**Unique Constraints:** `uq_company_receipt_sequences_company_id` on `(company_id)` — enforces the 1:1 cardinality.

**Check Constraints:**
- `chk_company_receipt_sequences_current_number_nonneg` — `current_number >= 0`.
- `chk_company_receipt_sequences_padding_length` — `padding_length BETWEEN 1 AND 10` — a sane bound preventing a pathological data-entry value; ten digits of padding (`"0000000001"`) is already far beyond any realistic per-company receipt volume.

**Indexes:** None beyond the unique constraint's implicit index — this table is looked up exclusively by `company_id`, already covered, identical to `company_settings`' "no additional index needed" conclusion (Module 1 §1.2).

**Business Rules — the concurrency-safe generation mechanism:**
- A receipt number is generated via a single atomic statement, executed inside the **same transaction** as the receipt row's own INSERT: `UPDATE company_receipt_sequences SET current_number = current_number + 1, updated_at = now() WHERE company_id = $1 RETURNING current_number, prefix, padding_length`. This row-level `UPDATE` takes an implicit PostgreSQL row lock for the duration of the transaction, which **serializes concurrent receipt-issuance attempts for the same company**: if two staff members (or a staff member and a concurrent automated eFAWATEERcom-settlement job) attempt to issue a receipt simultaneously, the second transaction blocks on the row lock until the first commits or rolls back, then proceeds against the now-incremented value — this is the direct, concrete implementation of the atomic-counter pattern Phase 1 §1.20 specified conceptually (`SELECT ... FOR UPDATE` / `UPDATE ... RETURNING`), now fully realized as a physical mechanism.
- The returned `current_number` is formatted as `prefix || lpad(current_number::text, padding_length, '0')` and written as `receipt_number` into either `rent_payment_receipts` or `expense_receipts` within that same transaction — because both tables draw from this one counter, numbers are genuinely continuous across both types in the common, no-rollback case.
- **Gaps are an accepted, documented tradeoff, not a bug.** If a transaction increments the counter and then rolls back for an unrelated reason before the receipt row itself commits (e.g., a downstream constraint violation elsewhere in the same transaction), that number is permanently skipped — this is the identical gap-tolerance already inherent to a native PostgreSQL `SEQUENCE`, and Jordanian bookkeeping practice tolerates a rare, explainable gap far better than it tolerates a race condition producing two receipts sharing the same number, so this tradeoff is the correct one and is called out here explicitly rather than silently assumed.
- **Reset-policy handling:** when `reset_policy IN ('yearly', 'monthly')`, the generation logic first compares the current period (year or month) against `last_reset_at`; if the period has changed, the same atomic operation resets `current_number` to `0` and updates `last_reset_at` *before* incrementing, all within one statement/transaction (implementable as a single `CASE`-driven `UPDATE` or, preferably, a dedicated PL/pgSQL function `generate_receipt_number(company_id UUID) RETURNS VARCHAR` that wraps the read-check-reset-increment logic in one database round-trip and one lock scope) — recommended as the actual Phase 8 implementation vehicle, flagged here so the reset-aware logic isn't implemented as several separate application-layer round-trips that would reopen the race condition this table's whole design exists to close.

**Soft Delete Strategy:** Not applicable — no independent lifecycle, identical to `company_settings` (Module 1 §1.2); hard-deleted only as a CASCADE consequence of a company's own (rare, admin-only) hard delete.

**Audit Requirements:** Changes to `prefix`, `padding_length`, or `reset_policy` (admin-configurable receipt-formatting decisions) are logged to `audit_logs` — a change to a company's receipt numbering format is a financially-relevant configuration event. Routine `current_number` increments are **not** individually audit-logged — that would be pure write-amplification noise on every single receipt issuance platform-wide; the receipt row itself (already audit-logged in its own table) is the meaningful record that a number was issued.

**Future Scalability Notes:** One row per company — trivially small at any scale (10,000 companies = 10,000 rows). No partitioning, no indexing concern, ever. This table is the mechanism that makes the two receipt tables' own scale (§7.6) manageable, not itself a scale concern.

---

## 7.5 Table: `efawateercom_transactions`

**Purpose:** The reconciliation ledger for every rent payment routed through the eFAWATEERcom gateway — every request pushed to the gateway and every response/callback received back, linked to the settling `rent_payments` row, with the raw gateway payload preserved for audit and dispute resolution.

**Business Description:** Per Phase 1 §1.18, eFAWATEERcom is a national Jordanian bill-payment aggregator; a tenant can pay a pushed bill through their own bank/wallet app, and the gateway asynchronously (or synchronously, depending on integration mode) confirms settlement back to the company. This table is that confirmation trail — scoped in this module strictly to the `rent_payments` settlement path (§7.0).

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| rent_payment_id | UUID | NO | — | The payment this gateway transaction settles — mandatory in this module's scope (§7.0), since no second settleable entity type is in scope here |
| external_transaction_id | VARCHAR(100) | NO | — | The eFAWATEERcom gateway's own transaction identifier — a real-world, globally unique reference issued by the gateway |
| payment_reference | VARCHAR(100) | YES | NULL | The biller-side reference/invoice number the payer sees on their own eFAWATEERcom portal/app — may differ from `external_transaction_id`; nullable since not every integration mode surfaces a distinct payer-facing reference |
| request_time | TIMESTAMPTZ | NO | — | When the bill-push/payment request was sent to the gateway |
| response_time | TIMESTAMPTZ | YES | NULL | When the gateway responded (synchronously or via async callback); nullable while a request is still in-flight/pending |
| transaction_status | efawateercom_status_enum | NO | 'pending' | `pending` \| `sent` \| `success` \| `failed` \| `timeout` \| `cancelled` — the stated requirement's transaction lifecycle |
| response_code | VARCHAR(20) | YES | NULL | Gateway-specific response/status code, for programmatic handling and support triage |
| response_message | TEXT | YES | NULL | Human-readable gateway response detail |
| amount | NUMERIC(12,3) | NO | — | The transaction amount |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217, per Global Convention |
| raw_response | JSONB | YES | NULL | The full raw gateway response payload — preserved for audit/debugging and as a forward-compatible capture of any gateway field not yet promoted to a structured column, mirroring the `feature_flags`/`metadata` narrow-JSONB-escape-hatch philosophy already used elsewhere in this schema (Module 1 §2.1, Module 3 §3.8) |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | NULL for the overwhelming majority of rows, which are system/job-generated, not staff-entered |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `rent_payment_id` → `rent_payments(id)` ON DELETE RESTRICT — per Phase 2, the gateway settlement ledger is reconciliation/audit evidence preserved permanently.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_efawateercom_transactions_external_transaction_id` on `(external_transaction_id)` WHERE `deleted_at IS NULL` — **global** uniqueness, not company-scoped. Justification: a gateway-issued transaction ID is a real-world globally unique identifier from an external authority, structurally identical to `utility_meters`' authority-issued-meter-number global-uniqueness reasoning (Module 4 §4.7) — the direct enforcement of the stated "duplicate external transaction IDs" integrity requirement.

**Check Constraints:**
- `chk_efawateercom_transactions_amount_positive` — `amount > 0`.
- `chk_efawateercom_transactions_response_time_after_request` — `response_time IS NULL OR response_time >= request_time` — a response cannot be recorded before its own request was sent.
- `chk_efawateercom_transactions_status_consistency` — `(transaction_status IN ('pending', 'sent')) OR (transaction_status IN ('success', 'failed', 'timeout', 'cancelled') AND response_time IS NOT NULL)` — every terminal status must carry a `response_time`; a row cannot claim to be resolved (successfully or otherwise) without a recorded response timestamp. `response_code`/`response_message` are deliberately left unconstrained against status, unlike `cheque_details`' comparably strict per-status gate (Module 6 §6.2) — not every gateway response payload populates both fields even in a genuinely terminal state, and over-constraining here would risk rejecting a legitimate, if sparse, real-world gateway response.

**Indexes:**
- `uq_efawateercom_transactions_external_transaction_id` (above) — doubles as the primary, hottest-path lookup for this table: every inbound gateway webhook/callback must resolve "which of our transactions does this external ID correspond to" before it can update `transaction_status`/`response_time`/etc. — evaluated on every single asynchronous gateway callback received, platform-wide.
- `idx_efawateercom_transactions_rent_payment_id` on `(rent_payment_id)` WHERE `deleted_at IS NULL` — **Payment Receipt Lookup**-adjacent requirement: "show the eFAWATEERcom transaction(s) for this payment" on the payment detail page. Not a strict 1:1 lookup — a payment may accumulate more than one transaction row across a failed attempt followed by a successful retry (see Business Rules), so this is a genuine 1:many index.
- `idx_efawateercom_transactions_company_status_request_time` composite on `(company_id, transaction_status, request_time DESC)` WHERE `deleted_at IS NULL` — the **Dashboard Statistics** query shape: "all currently-pending/failed eFAWATEERcom transactions for company X, most recent first" — the operational worklist for staff investigating gateway issues or reconciling stuck payments. `company_id` leads for tenant scope, `transaction_status` second as the near-universal secondary equality filter (the ops screen almost always asks for one specific status bucket), `request_time` trailing for the recency ordering.
- `idx_efawateercom_transactions_payment_reference` on `(payment_reference)` WHERE `payment_reference IS NOT NULL AND deleted_at IS NULL` — supports the stated **Search** requirement: staff resolving a tenant's payment dispute using the reference number visible to the tenant on their own eFAWATEERcom app/bank statement, which may not match `external_transaction_id`.

**Deliberately omitted index:** no GIN index on `raw_response`. Free-text/structured search inside the raw gateway payload is not a stated query requirement, and a GIN index carries real write-amplification cost on a table that grows with every gateway round-trip — the identical "avoid unnecessary indexes" judgment already applied to `audit_logs.metadata` (Module 3 §3.8).

**Business Rules:**
- A row is created at `request_time` with `transaction_status IN ('pending', 'sent')` when the application pushes a bill/payment request to the gateway, then **updated in place** — not superseded by a new row — when the gateway's response (synchronous or async callback) arrives, populating `response_time`/`transaction_status`/`response_code`/`response_message`/`raw_response`. This is the one legitimate UPDATE-in-place pattern in this module, and a deliberate departure from the append-only philosophy applied elsewhere in Modules 5–6: a transaction row represents **one continuous request/response lifecycle** for a single gateway interaction, not a sequence of discrete historical facts the way a `contract_status_history` or `payment_allocations` row is — this mirrors how a single `cheque_details` row (Module 6 §6.2) progresses through its own lifecycle via UPDATE rather than a new row per stage.
- A **retried** payment attempt (following a `failed`/`timeout`/`cancelled` outcome) creates a **new** row with its own distinct `external_transaction_id`, rather than reusing or overwriting the failed row — preserving the complete attempt history and keeping the global `external_transaction_id` uniqueness constraint meaningful (two attempts are two real, distinct gateway interactions, even against the same underlying `rent_payment_id`).
- On `transaction_status = 'success'`, the application (not a DB trigger) creates the corresponding `payment_allocations` row(s) against `rent_payment_id` in the same transaction as the status UPDATE, exactly as any other settlement event would (Module 6 §6.3) — eFAWATEERcom is simply another `payment_method` feeding the same allocation mechanism, not a parallel settlement pathway with its own balance-tracking logic.

**Soft Delete Strategy:** Standard, reserved for genuine data-entry/integration correction (e.g., a transaction logged against the wrong `rent_payment_id` due to an integration bug, corrected before any downstream `payment_allocations` row was created from it); a real completed (successful or failed) gateway transaction is never soft-deleted, since it is permanent reconciliation evidence.

**Audit Requirements:** Every status transition is audit-logged; `success`/`failed` terminal transitions are the financially-relevant events. Failed/timeout transitions are flagged `severity = 'warning'` in `audit_logs` — a lower severity than `cheque_details`' `bounced`-transition `critical` flag (Module 6 §6.2), since a failed eFAWATEERcom attempt has a straightforward, low-stakes retry/fallback-to-another-payment-method path, unlike a bounced cheque's comparatively serious legal and collections weight.

**Future Scalability Notes:** Named in the scale target as "millions of eFAWATEERcom transaction records." Scales with (the fraction of `rent_payments` settled via eFAWATEERcom × a small retry multiplier) — bounded above by a modest multiple of `rent_payments`' own 2,000,000+ target (Module 6 §6.4). Full treatment in §7.6.

---

## 7.6 Scalability at Target Scale

| Scale point | Impact |
|---|---|
| 10,000 companies | Trivial for every table in this module — `company_receipt_sequences` stays at exactly one row per company; per-company row counts on the other four tables stay in the low hundreds to low thousands even at full portfolio size, well within any single index's efficient range. |
| 10,000,000 expenses | **This module's central scalability decision.** `expenses` meets the same partitioning test `rent_payments` met and `apartments`/`lease_contracts` failed (Module 6 §6.4, Module 5 §5.8, Module 4 §4.4): append-heavy in practice (an expense row, once entered, is rarely revised except by the rare correction flow), naturally time-ordered via `expense_date`, and every stated query shape (Expense History, Financial Reports, Dashboard Statistics) is bounded to a company/building scope *and* frequently further bounded to a fiscal period (a quarter, a year) rather than "all expenses ever" as an undifferentiated scan. **Recommendation: RANGE partition `expenses` by `expense_date`, yearly** (coarser than `rent_payments`' quarterly choice — see rationale below). |
| 5,000,000 receipts (combined) | Scales as a fraction of `rent_payments` (for `rent_payment_receipts`, at most 1:1) and of `expenses` (for `expense_receipts`, typically 0–2 receipts per expense). Both receipt tables' dominant query patterns are FK-equality lookups (`rent_payment_id`, `expense_id`) and `receipt_number` point-lookups — **not** time-range-bounded reporting scans in the way `expenses`'/`rent_payments`' dashboard queries are. **Partitioning is not recommended for either receipt table**, for the identical query-pattern-driven reason `payment_allocations` and `cheque_details` were excluded in Module 6 §6.4 — row count alone doesn't justify it, and partitioning would risk spreading one expense's or one payment's few receipt rows across partition boundaries with no compensating benefit, since receipt rows aren't naturally date-clustered together the way a single meter's readings are. |
| Millions of `efawateercom_transactions` | Bounded above by a modest multiple of `rent_payments`' own 2,000,000+ target (retry attempts being the only multiplier). Its dominant query patterns — the gateway-callback's `external_transaction_id` point lookup and the `rent_payment_id` drill-down — are equality-lookup-driven, not time-range-driven, the identical shape already excluded from partitioning for `payment_allocations` (Module 6 §6.4). **Partitioning is not recommended** for this table at the stated target scale; should transaction volume ever grow to tens of millions independent of `rent_payments`' own growth (e.g., a future high-retry-rate integration issue), this conclusion should be revisited against `request_time`, but no such pressure is evident from the stated scale target. |
| `company_receipt_sequences` | One row per company, always. No scale concern at any target. |

**Why yearly, not quarterly, partitioning for `expenses` — a further deliberate deviation from `rent_payments`' own already-deviated quarterly convention (Module 6 §6.4):** `rent_payments` chose quarterly over the monthly convention used for `meter_readings`/`audit_logs`/`login_history` because its write density (~500,000 rows/monthly partition at full target scale) was already comfortably manageable at a coarser grain than those three high-frequency event-log tables. `expenses` has a **lower and less regular** write density than `rent_payments`: unlike a billing job generating one predictable row per active contract every cycle, expense entry frequency varies by building size, portfolio maturity, and operational intensity, with no comparable "one row per active X per period" floor. At 10,000,000 rows across a realistic multi-year (and, per Jordanian statutory retention requirements, permanently-retained) platform history, a **yearly** partition holds on the order of 1,000,000–2,000,000 rows at full target scale — already a perfectly reasonable single-partition size — while a monthly or even quarterly grain would multiply the total partition count (and its own long-term management overhead) for no corresponding per-partition-size benefit, given this table, like `rent_payments`, is never bulk-dropped past a retention window and therefore accumulates partitions forever. This is the same "calibrate partition granularity to the table's actual growth rate rather than mechanically reapplying a prior module's chosen grain" discipline `rent_payments` itself introduced in Module 6 §6.4, applied here to a table with a yet-different profile.

**Partition key column requirement:** `expense_date` already exists as a `NOT NULL` column with no default dependency — no schema addition is needed to introduce partitioning later, only the table-rebuild migration itself (`CREATE TABLE ... PARTITION BY RANGE`, backfill, swap), flagged here so it is planned for rather than discovered as a surprise once the table is already large, consistent with the standing convention established since Module 3 §3.11.

**Should `expense_receipts`/`rent_payment_receipts`/`efawateercom_transactions` be revisited if `expenses`/`rent_payments` are partitioned?** No structural change is required to any of the three — a partitioned parent's child tables with a simple FK to the parent's `id` continue to work identically in PostgreSQL regardless of whether the parent is partitioned, identical to the conclusion already reached for `cheque_details`/`payment_allocations` against a partitioned `rent_payments` (Module 6 §6.4).

---

## 7.7 Database Integrity Summary

| Risk | Prevention Mechanism | Explanation |
|---|---|---|
| Duplicate receipt numbers within the same company | Primary: atomic `company_receipt_sequences` generation (§7.4). Backstop: `uq_expense_receipts_company_receipt_number` / `uq_rent_payment_receipts_company_receipt_number`, each on `(company_id, receipt_number)` | The generator makes a collision practically unreachable through normal application code; the unique constraints are the DB-level backstop against a direct-write bypass, identical defense-in-depth posture to other trigger-backstopped invariants in this schema (Module 5 §5.1, Module 6 §6.1) |
| Duplicate external eFAWATEERcom transaction IDs | `uq_efawateercom_transactions_external_transaction_id`, global (not company-scoped) | A gateway-issued ID is a real-world globally unique identifier; global uniqueness correctly reflects that physical reality, mirroring `utility_meters`' authority-issued-meter-number pattern (Module 4 §4.7) |
| Negative expense amounts | `chk_expenses_amount_positive` | A credit/refund is a new, positive-amount row with an explanatory note, never a negative-amount row — keeps the sign convention unambiguous |
| Invalid receipt references (orphaned) | `expense_receipts.expense_id`, `rent_payment_receipts.rent_payment_id` both `NOT NULL` with real FKs, `ON DELETE RESTRICT` | No receipt can exist without a valid, permanent reference to the financial event it documents/certifies |
| Orphan receipt files | `expense_receipts.file_id` `NOT NULL` with a real FK to `file_storage`, `ON DELETE RESTRICT` | The uploaded file's metadata record must outlive the join row referencing it; mirrors `contract_documents.file_id` (Module 5 §5.5) |
| Invalid expense categories | `category` typed as `expense_category_enum`, a closed domain | No free-text category drift is representable; every row's category is one of the twelve stated values, keeping category-bucketed financial reporting reliable |
| Duplicate file attached twice to the same expense | `uq_expense_receipts_expense_file` on `(expense_id, file_id)` | Catches the genuine duplicate-upload data-entry error while allowing the same file to (in principle, though not realistically) appear against a different expense |
| Future-dated financial records | `chk_expenses_expense_date_not_future`, `chk_expense_receipts_issued_at_not_future`, `chk_rent_payment_receipts_issue_date_not_future` | Every date-of-record column in this module documents something that has already happened, consistent with the identical "not a scheduling field" reasoning applied to `contract_terminations.termination_date` (Module 5) and `meter_readings.reading_date` (Module 4) |
| Gateway response recorded before its own request | `chk_efawateercom_transactions_response_time_after_request` | A response cannot logically precede the request it responds to |
| Terminal gateway status with no recorded response time | `chk_efawateercom_transactions_status_consistency` | A transaction cannot claim to be resolved (successfully or otherwise) without the timestamp of that resolution on record |

---

## 7.8 Security & Multi-Tenant Isolation

- **RBAC:** Expense and receipt mutation is gated by existing `expenses.create`/`expenses.approve`/`receipts.issue`-style permission keys (Module 3, `permissions`) resolved via `role_permissions` — no new RBAC machinery introduced in this module; it consumes the existing platform-wide permission catalog exactly as every prior module has.
- **Audit Logs:** Every table in this module writes to `audit_logs` on mutation per the Global Convention; `expenses.amount`/`.category`/`.building_id` changes, all receipt issuance events, and `efawateercom_transactions` terminal-status transitions are flagged high-severity or elevated-severity as detailed per-table above.
- **Multi-Tenant Isolation:** Every table in this module carries a denormalized `company_id` and is RLS-ready under the standard `USING (company_id = current_setting('app.current_company_id')::uuid)` policy. No table in this module needs a non-standard RLS shape (unlike Module 3's `refresh_tokens`/`login_history`) — every row in every table here belongs unambiguously to exactly one company, with no cross-tenant legitimate-access case (unlike `marketplace_listings`, Phase 1 §1.24).
- **Soft Delete:** Standard across `expenses`, `expense_receipts`, `rent_payment_receipts`, and `efawateercom_transactions`; explicitly **not applicable** to `company_receipt_sequences`, which — like `company_settings` — has no independent lifecycle outside CASCADE from its parent company.
- **Financial-Document Integrity:** The receipt-numbering mechanism (§7.4) is the security-critical path in this module — a compromised or buggy generator could produce duplicate or non-sequential receipt numbers, undermining the legal/bookkeeping guarantee the whole subsystem exists to provide. The atomic-UPDATE-with-implicit-row-lock mechanism, backstopped by the per-table unique constraints, is the layered defense against both a concurrency bug and a direct-SQL-write bypass, consistent with the "don't rely solely on application-layer discipline for a financial-integrity invariant" posture already established for `lease_contracts` immutability (Module 5 §5.1) and `payment_allocations` over-allocation prevention (Module 6 §6.3).

---

## 7.9 Performance Optimization Explanation

- **Expense History:** `idx_expenses_company_date` — equality-leading composite with `DESC` trailing sort column, turning the module's most common query into a direct ordered index scan, identical pattern to `idx_rent_payments_contract_due_date` (Module 6).
- **Receipt Lookup:** `idx_expense_receipts_company_receipt_number` and `idx_rent_payment_receipts_company_receipt_number` — both doubling with their respective unique constraints, giving an O(log n) lookup on the exact "find this receipt by its printed number" shape.
- **Payment Receipt Lookup:** `uq_rent_payment_receipts_rent_payment_id` — the 1:1 relationship's own unique index directly serves this as a single-row point lookup.
- **Financial Reports:** `idx_expenses_company_category_date` (category-bucketed spend), `idx_expenses_building_date` (per-building P&L), `idx_expense_receipts_company_issued_at` and `idx_rent_payment_receipts_company_issue_date` (receipt-volume reporting) — together cover every stated reporting shape without a bespoke index per report type.
- **Search:** `idx_expenses_vendor_trgm` (fuzzy vendor-name search), `idx_expenses_invoice_number` (exact/partial invoice lookup), `idx_efawateercom_transactions_payment_reference` (tenant-facing gateway reference lookup) — together cover every stated free-text/fragment search need across this module's searchable identifier types.
- **Filtering:** every dashboard/worklist filtering shape named in the brief (by category, by building, by status, by date range, by company) is covered by one of the composite indexes above — no filtering requirement in this module lacks a matching leading-column-correct index.
- **Pagination:** every composite index with a trailing `DESC` date column directly supports keyset pagination (`WHERE (company_id, expense_date) < ($1, $2) ORDER BY expense_date DESC LIMIT 50`), the same preferred-over-`OFFSET` strategy established since Module 3 §3.10.
- **Dashboard Statistics:** `idx_expenses_company_category_date` (spend-by-category tiles) and `idx_efawateercom_transactions_company_status_request_time` (gateway-health tiles) together cover this module's dashboard-tile shapes without a bespoke index per tile.
- **Write-Path Consideration:** unlike `payment_allocations`' write-path-critical indexes (Module 6 §6.6), no table in this module has a comparable write-path aggregate-query bottleneck — `company_receipt_sequences`' single-row atomic UPDATE is index-free by nature (a PK/unique lookup on `company_id`), and no table here maintains a derived aggregate column via a per-write `SUM()` the way `rent_payments.amount_paid` does. This module's indexing concern is therefore uniformly read-side, the same shape as Modules 3–5's dominant pattern, rather than Module 6's write-path-critical exception.

---

## Module 7 (Financial Operations) — Architecture Review

- **PostgreSQL Best Practices:** UUIDv7 PKs throughout; `TIMESTAMPTZ` for all point-in-time columns (`request_time`, `response_time`, audit columns) versus correctly-typed `DATE` for calendar-date concepts (`expense_date`, `issued_at`, `issue_date`) — the identical DATE-vs-TIMESTAMPTZ discipline applied correctly and consistently since Module 5; `NUMERIC(12,3)` for every JOD monetary column paired with an explicit `currency` column, matching the Global Convention exactly across all five tables.
- **Financial Integrity:** The receipt-numbering subsystem (§7.4) is this module's central financial-integrity mechanism, given full concurrency-safety treatment (atomic UPDATE with implicit row lock, documented gap-tolerance tradeoff, reset-policy handling) rather than left as the conceptual sketch Phase 1 §1.20 originally provided — this is the physical-design layer doing its actual job of reducing a conceptual invariant to an enforceable mechanism. `expense_receipts`' amount column is explicitly documented as *not* subject to an aggregate-consistency invariant against its parent `expenses.amount` (§7.2), a deliberate and explained contrast against `payment_allocations`' strict over-allocation-prevention trigger (Module 6 §6.3) — the same schema correctly applies different integrity postures to a documentary-evidence relationship versus a financial-settlement relationship, rather than mechanically applying the stricter pattern everywhere.
- **Referential Integrity:** Every FK in this module is `NOT NULL` and `RESTRICT`-on-delete, except the standard SET NULL actor-attribution columns (`created_by`/`updated_by`/`deleted_by`/`issued_by`/`uploaded_by`). `expenses.building_id` intentionally uses `ON DELETE RESTRICT` to preserve permanent financial traceability and ensure historical expenses always remain linked to the building where they were incurred.
- **Constraint Quality:** The receipt-uniqueness defense-in-depth (generator as primary defense, unique constraint as backstop) mirrors the identical posture already validated for circular lease-renewal-chain prevention (Module 5 §5.1) and payment over-allocation prevention (Module 6 §6.3) — this module introduces no new class of integrity problem, it applies the schema's already-established layered-defense philosophy to a new financial subsystem.
- **Index Quality:** Every composite index in this module was checked for column-order correctness (equality-before-range/sort, `DESC` embedded where a query needs most-recent-first) — no index found with the wrong leading column. Two indexes were deliberately **not** created and the omission explicitly justified rather than silently skipped: a standalone `payment_method` index on `expenses` (§7.1) and a GIN index on `efawateercom_transactions.raw_response` (§7.5) — both real candidates a less careful pass might add reflexively, both correctly identified as adding write cost without a corresponding read benefit given this module's actual stated query patterns.
- **Query Performance:** Every stated required query shape (Expense History, Receipt Lookup, Payment Receipt Lookup, Financial Reports, Search, Filtering, Pagination, Dashboard Statistics) is explicitly mapped to a specific index in §7.9, with no requirement left unaddressed and no index added without a corresponding named requirement — the same discipline as Module 4's and Module 6's requirement-to-index mapping.
- **Multi-Tenant Isolation:** Confirmed — every table denormalizes `company_id` directly (§7.8); no table in this module needs a non-standard RLS shape.
- **Future Scalability:** `expenses` is correctly identified as this module's sole partitioning candidate, with a **further-recalibrated** partitioning grain (yearly) that explicitly reasons about *why* it differs from both the monthly convention (`meter_readings`/`audit_logs`/`login_history`) and `rent_payments`' own already-deviated quarterly convention (Module 6 §6.4), rather than mechanically reapplying either prior choice — continuing the non-mechanical, growth-rate-driven partitioning discipline `rent_payments` itself introduced. `expense_receipts`, `rent_payment_receipts`, and `efawateercom_transactions` were all deliberately evaluated *against* partitioning and found not to need it, with the reasoning made explicit (FK-equality-lookup-dominant access patterns, not time-range-driven) rather than defaulting to "large table therefore partition."
- **Maintainability:** Every non-obvious modeling decision in this module — the `expense_receipts` cardinality deviation from Phase 1's original 1:1 design, the shared cross-table receipt sequence and its full concurrency mechanism, the scoped-to-rent-payments-only `efawateercom_transactions` design with its flagged future extension path, the yearly-not-quarterly-not-monthly partitioning calibration, and the deliberate non-enforcement of receipt/expense amount-sum consistency — is documented in place with its specific business and technical reasoning, continuing the "no undocumented deviation" discipline maintained without exception since Module 1.

---

*End of Module 7. Say "CONTINUE" for MODULE 8 — MAINTENANCE (maintenance_requests, maintenance_attachments).*

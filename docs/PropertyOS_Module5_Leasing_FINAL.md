# MODULE 5 — LEASING

Continuation of `PropertyOS_Phase3_Physical_Design.md` and `PropertyOS_Module4_Properties.md`. Applies the same Global Conventions (UUIDv7 PKs, `created_at`/`updated_at`/`created_by`/`updated_by`, soft delete, `company_id` + RLS tenant isolation, `snake_case` naming) unless a deviation is explicitly justified.

Scale target for this module: **500,000+ lease contracts, 2,000,000+ payment records (Module 6, dependent on this module's PK), 10,000+ companies.**

---

## 5.0 Module-Wide Design Decisions (read before the tables)

**`lease_contracts` is the true center of gravity of the entire schema.** Per Phase 1 §1.13, every financial, operational, and legal child entity in PropertyOS ultimately FKs to a specific `lease_contracts.id` — `rent_payments`, `utility_bills`, `contract_terminations`, `parking_assignments`, `cheque_details`, and (this module) `contract_status_history`, `contract_documents`. This makes correctness and performance on this one table disproportionately important relative to its row count — 500,000 rows is trivial in absolute terms, but it sits at the hub of nearly every join in the system.

**Self-referencing renewal chain — no `contract_renewals` table.** Per the FINAL decision carried forward from Phase 1 §1.13 and reaffirmed in this brief: `lease_contracts.prior_contract_id` is a nullable, self-referencing FK. A renewal is a brand-new row, never a mutation of the row it supersedes. This section designs the concrete DDL-level enforcement of that chain, including the circular-renewal-chain prevention that Phase 1/2 flagged conceptually but did not yet reduce to a physical mechanism — that mechanism is specified in full below (§5.1, Check/Trigger Constraints).

**Denormalized `building_id` and `apartment_id` alongside `tenant_id`.** Consistent with Module 4's module-wide denormalization principle (§4.0), `lease_contracts` carries `company_id`, `building_id`, and `apartment_id` directly rather than requiring a join through `apartments → floors → buildings` to answer "which building is this lease in" — an extremely common filter for portfolio-level dashboards and owner reporting. `apartment_id` is the authoritative FK (per the stated requirement "every contract belongs to exactly one apartment"); `building_id` is denormalized from it at write time and never independently settable.

**Guarantors are out of scope for this module.** No `lease_guarantors` table (or any guarantor-tracking entity) is part of this module's physical design. Any prior reference to a guarantor sub-entity is superseded by this scope statement — a lease contract's parties, in the current physical design, are limited to the tenant and the company; guarantor tracking is not modeled.

**Status history and documents are child entities of a single contract, not shared across renewals.** A renewal is a new legal contract; its child tables (`contract_status_history`, `contract_documents`) are always created fresh against the new contract's own `id`, never re-parented from the predecessor — this preserves the principle that each contract's full legal packet (parties, documents, status history) is self-contained and independently auditable, matching how a Jordanian property manager actually files a renewal: as a new folder, not an amendment to the old one.

**Why `contract_status_history` is a separate table from `audit_logs`.** `audit_logs` (Module 3) already captures every `UPDATE` to `lease_contracts.status` generically, including the actor, timestamp, and JSONB before/after diff. `contract_status_history` is **not** redundant with this — it exists because status transitions in a lease lifecycle are a **first-class business workflow** queried directly and frequently by product features (a contract's timeline view, a "why was this contract terminated" support query, a compliance report on time-to-signature), not just a security/compliance audit trail. Querying `audit_logs` for this would require filtering a massive cross-entity table by `entity_name = 'lease_contracts' AND entity_id = $1 AND action = 'status_change'` and unpacking JSONB on every read — workable for forensics, wrong for a hot product feature. `contract_status_history` gives a purpose-built, narrow, directly-indexed table for that specific access pattern, with a business-meaningful `reason` column `audit_logs.metadata` was never designed to guarantee the presence of. Both tables are written in the same transaction on every status change (application-orchestrated), giving the redundancy of *coverage* without the redundancy of *query shape*.

---

## 5.1 Table: `lease_contracts`

**Purpose:** The central legal and financial entity of the entire platform — the binding agreement between a tenant and a company for a specific apartment over a defined term. Every downstream financial and operational record (payments, utility bills, terminations, parking, documents) traces back to a row here.

**Business Description:** Represents one contiguous lease term. Renewals are modeled as an entirely new row linked via `prior_contract_id`, never as a mutation of an existing row — once a contract is superseded, its financial terms are permanently immutable at the application layer. Captures Jordan-specific legal nuance (`legal_regime` for Old Rent Law tenancies) and the full commercial terms of the tenancy.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company (tenant-scope root) |
| building_id | UUID | NO | — | Denormalized from `apartment_id → floors → buildings`, see §5.0 |
| apartment_id | UUID | NO | — | The leased unit — every contract belongs to exactly one apartment (stated requirement) |
| tenant_id | UUID | NO | — | The renting party — every contract belongs to exactly one tenant (stated requirement) |
| prior_contract_id | UUID | YES | NULL | Self-referencing FK to `lease_contracts(id)` — NULL on the first contract in a renewal chain; populated on every renewal, pointing at the contract it succeeds |
| contract_number | VARCHAR(50) | NO | — | Human-facing legal reference number, e.g. "LC-2026-004821"; company-scoped sequence, generated at application layer |
| legal_regime | legal_regime_enum | NO | 'standard' | `standard` \| `old_rent_law` — flags Jordan's legacy قانون المالكين والمستأجرين tenancies, which carry different termination/eviction rules (Phase 1 §1.13) |
| tenant_type | tenant_type_enum | NO | 'personal' | `personal` \| `corporate` — future-ready for corporate leasing; corporate-specific fields live on `tenants`/a future `corporate_tenant_details` extension, not here |
| start_date | DATE | NO | — | — |
| end_date | DATE | NO | — | — |
| signed_date | DATE | YES | NULL | Date the physical/digital contract was actually signed — nullable since a contract can exist in `draft`/`pending_signature` status before signing |
| monthly_rent_amount | NUMERIC(12,3) | NO | — | The contract's base rent figure, always expressed as a **monthly** amount regardless of `payment_frequency` for consistent cross-contract comparison/reporting; the actual per-installment billed amount is derived at `rent_payments`-generation time from `payment_frequency` |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217, per Global Convention money-column pairing |
| security_deposit_amount | NUMERIC(12,3) | NO | 0 | — |
| payment_frequency | payment_frequency_enum | NO | 'monthly' | `monthly` \| `quarterly` \| `semi_annual` \| `annual` — Jordan market requirement; monthly is overwhelmingly standard for residential, quarterly/annual common for commercial |
| payment_due_day | SMALLINT | NO | 1 | Day-of-month (1–28, capped to avoid month-length ambiguity) on which each installment falls due |
| status | contract_status_enum | NO | 'draft' | `draft` \| `pending_signature` \| `active` \| `expired` \| `renewed` \| `terminated` \| `cancelled` \| `superseded` — see Business Rules for the distinction between `renewed`/`superseded` and `terminated`/`expired` |
| external_registration_ref | VARCHAR(100) | YES | NULL | Optional municipal digital-registration reference (Ejar-style pilot programs), per Phase 1 §1.13 |
| contract_document_id | UUID | YES | NULL | FK to `file_storage` — the primary signed-contract PDF; nullable until uploaded. Distinct from the fuller `contract_documents` child table (§5.5), which supports arbitrary multiple attachments; this column is a convenience pointer to *the* canonical signed document for fast access without a join, when one exists |
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
- `building_id` → `buildings(id)` ON DELETE RESTRICT.
- `apartment_id` → `apartments(id)` ON DELETE RESTRICT — per Phase 2, historical lease record is core financial/legal evidence; never hard-deleted while a lease references the unit.
- `tenant_id` → `tenants(id)` ON DELETE RESTRICT — a lease cannot exist without its tenant party, ever, including retroactively.
- `prior_contract_id` → `lease_contracts(id)` ON DELETE RESTRICT — self-referencing; a superseded contract can never be hard-deleted while a successor points at it (see Business Rules for cycle prevention).
- `contract_document_id` → `file_storage(id)` ON DELETE SET NULL (forward reference — Documents module).
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:**
- `uq_lease_contracts_company_contract_number` on `(company_id, contract_number)` WHERE `deleted_at IS NULL` — prevents duplicate contract numbers within the same company (stated integrity requirement); scoped to company, not global, since contract numbers are a company-internal legal-document sequence, not a platform-wide identifier — two different companies may both have a "LC-2026-001".
- `uq_lease_contracts_one_active_per_apartment` — **partial unique index** on `(apartment_id)` WHERE `status = 'active' AND deleted_at IS NULL` — the direct physical enforcement of the stated business rule "an apartment cannot have two ACTIVE lease contracts at the same time." This is the single most important constraint in this module; it is what makes `occupancy_status` in Module 4 a safely cacheable, trigger-maintained column, since the database itself guarantees the invariant the cache is built on. **This index is retained unchanged** — it remains the sole database-level structural guarantee against multiple simultaneously-`active` contracts on the same apartment. It is deliberately **not** widened to cover `draft`/`pending_signature` statuses (see the overlapping-non-active-contracts business rule below, which governs that broader case at the application layer instead).

**Check Constraints:**
- `chk_lease_contracts_dates` — `end_date > start_date` — rejects an inverted or zero-length lease term at the database level.
- `chk_lease_contracts_signed_date` — `signed_date IS NULL OR signed_date <= end_date` — a contract cannot be signed after its own term has already ended; deliberately **not** constrained against `start_date` (a contract is very commonly signed a few days *before* `start_date` in practice, and occasionally signed a short grace period *after* `start_date` for administrative-lag reasons — over-constraining this would reject legitimate real-world timing).
- `chk_lease_contracts_monthly_rent_positive` — `monthly_rent_amount > 0`.
- `chk_lease_contracts_deposit_nonneg` — `security_deposit_amount >= 0` — zero is valid (some negotiated leases waive deposit), negative is not.
- `chk_lease_contracts_payment_due_day` — `payment_due_day BETWEEN 1 AND 28` — capped at 28 specifically to avoid February/30-vs-31-day-month ambiguity in due-date calculation; a due day of 29–31 is a real-world data-entry footgun this constraint eliminates entirely rather than pushing the edge case into billing-job logic.
- `chk_lease_contracts_no_self_reference` — `prior_contract_id IS NULL OR prior_contract_id != id` — a contract can never be its own predecessor; the trivial one-row cycle case, caught immediately and cheaply at the row level (the general multi-row cycle case is handled separately, see below).
- `chk_lease_contracts_external_owner_tenant_type` — none imposed correlating `tenant_type = 'corporate'` with additional NOT NULL fields, deliberately: corporate-tenant-specific structured data (registration number, authorized signatory) is intentionally out of v1 scope per the brief's "future-ready" framing, and adding a half-built corporate schema now would be speculative; `tenant_type` exists today purely so the column and its downstream reporting/filtering logic already exist when a future module adds the corporate-specific extension table, avoiding a migration on this table later.

**Circular renewal chain prevention (beyond the trivial self-reference CHECK above):**
A general n-row cycle (`A → B → C → A`) **cannot** be prevented by a CHECK constraint alone, since CHECK constraints in PostgreSQL cannot inspect other rows. The chosen mechanism, consistent with how this exact class of problem is already handled elsewhere in this schema:
- **Application-layer invariant (primary defense):** creating a renewal is a single transaction that (1) validates `prior_contract_id` refers to a contract that is not already `superseded`/pointed-at by another row's `prior_contract_id`, and (2) walks the chain backward from the proposed `prior_contract_id` to confirm the new row's own (as-yet-unassigned) `id` does not already appear in that ancestry — trivially cheap in practice since chains are short (a handful of renewals over a tenancy's real-world lifetime, not thousands).
- **Structural backstop:** `uq_lease_contracts_prior_contract_id` — **UNIQUE** constraint on `(prior_contract_id)` WHERE `prior_contract_id IS NOT NULL AND deleted_at IS NULL` — guarantees at the database level that **at most one contract can claim to supersede any given prior contract**, which eliminates the possibility of a "diamond" renewal graph (two different contracts both claiming the same predecessor) and, combined with the trivial-self-reference CHECK, reduces the only remaining theoretical cycle risk to the application-layer chain-walk above. A true multi-node cycle would require every node in the cycle to have been inserted in a specific mutually-referencing order that the one-predecessor-per-contract uniqueness constraint already makes practically unreachable through normal renewal-creation application code (each renewal is always created after its predecessor already exists, since you cannot renew a contract that doesn't exist yet — the chain can only ever grow forward in time, never close a loop, as a direct consequence of `created_at` ordering).

**No prevention, at the database-constraint level, of overlapping non-active (`draft`/`pending_signature`) contracts on the same apartment — addressed instead as an application-layer business rule below.** The partial unique index above covers only `status = 'active'`. See "Overlapping lease contracts — draft/pending-signature" under Business Rules for the governing rule and enforcement point.

**Indexes:**
- `uq_lease_contracts_one_active_per_apartment` (above) — doubles as **the** "current active lease for apartment X" lookup (`WHERE apartment_id = $1 AND status = 'active'`), which is the single most frequent query in the leasing module (every apartment detail page, every occupancy check, every new-lease-eligibility check). Preferred over a plain composite index because it is simultaneously the integrity constraint *and* the performance index — one structure serving both purposes, with zero risk of the index and the constraint drifting out of sync.
- `idx_lease_contracts_apartment_history` composite on `(apartment_id, start_date DESC)` WHERE `deleted_at IS NULL` — supports "full lease history for this apartment" (stated requirement "Apartment History"), most-recent-first; distinct from the active-lease partial index above because this one must also return non-active (expired/terminated/superseded) rows, which the partial index deliberately excludes. This same index is also the natural scan path for the application-layer overlap check described below (all non-terminal rows for an apartment, filtered further by status in application code).
- `idx_lease_contracts_tenant_history` composite on `(tenant_id, start_date DESC)` WHERE `deleted_at IS NULL` — supports "full lease history for this tenant" (stated requirement "Tenant History") — a tenant's rental history screen, and the credit/reliability check a company performs before approving a new lease for a returning tenant.
- `idx_lease_contracts_company_status` composite on `(company_id, status)` WHERE `deleted_at IS NULL` — the primary dashboard-statistics query shape (stated requirement "Dashboard Statistics"): "count/list contracts by status for company X" — e.g., "how many active leases," "how many expiring this month." `company_id` leads as the mandatory tenant-scoping equality filter present in every query against this table; `status` second as the near-universal secondary equality filter layered on top.
- `idx_lease_contracts_expiration` composite on `(company_id, end_date)` WHERE `status = 'active' AND deleted_at IS NULL` — supports the stated "Contract Expiration" requirement directly: the scheduled job (and the "leases expiring in the next 30 days" dashboard widget) needs exactly this shape — active contracts for a company, ordered/filtered by `end_date`. Made partial on `status = 'active'` specifically, not just `deleted_at IS NULL`, because expiration-monitoring logic is only ever meaningful against currently-active contracts — an already-terminated or already-superseded contract's `end_date` is historical, not an upcoming event to monitor, so indexing those rows for this query shape would be pure dead weight.
- `idx_lease_contracts_prior_contract_id` — implicit index from `uq_lease_contracts_prior_contract_id` (above) — also directly serves the stated "Contract Renewal" requirement's reverse-chain-walk query (`WHERE prior_contract_id = :this_contract_id`, "what superseded this contract"), since the unique constraint's backing index supports that exact equality lookup without needing a separate index.
- `idx_lease_contracts_contract_number_trgm` — GIN trigram index on `contract_number` — supports the stated "Search" requirement for staff searching/filtering by partial or misremembered contract number (e.g., "lc-2026-48" typed into a search box); a plain B-tree serves only prefix search, and Jordanian back-office staff commonly search contract numbers by fragment rather than exact/prefix match when working from a printed folder label or a partial recollection.

**Deliberately omitted index:** no standalone index on `building_id` alone. Every realistic building-scoped leasing query (e.g., "all leases in building X") is either (a) already served as a natural extension of the apartment-level or company-level composites above via a join from `apartments`/`buildings` (which are themselves already indexed on `company_id`), or (b) infrequent enough (a portfolio-manager occasionally drilling into one building's lease list, not a per-request hot path) that a sequential scan filtered through the `idx_lease_contracts_company_status` composite's `company_id` leading column, refined by application-layer filtering on the small resulting set, is entirely adequate — adding a dedicated index here would be reflexive rather than query-pattern-justified, precisely the anti-pattern flagged and avoided consistently since Module 4.

**Business Rules:**
- The first contract in any tenancy has `prior_contract_id = NULL`.
- Renewal is **never** an UPDATE to an existing row — it is the INSERT of a brand-new `lease_contracts` row with its own dates, rent, deposit, and `prior_contract_id` pointing at its predecessor.
- **Overlapping lease contracts — draft/pending-signature (business rule; no schema change).** In addition to the `uq_lease_contracts_one_active_per_apartment` partial unique index (which enforces the invariant only for `status = 'active'`), the following business rule applies and is enforced at the application layer: for the same `apartment_id`, no two contracts whose `status` is in `('draft', 'pending_signature', 'active')` may have overlapping `[start_date, end_date)` date ranges. Before a contract transitions to `pending_signature` or to `active`, the application must check for another non-terminal contract (`draft`/`pending_signature`/`active`) on the same apartment with an overlapping date range, and must reject the transition if an overlap exists. For a renewal specifically, the new contract's `start_date` must be `>= ` the prior contract's `end_date` (i.e., `new_contract.start_date >= prior_contract.end_date`). This is an application-layer cross-row business invariant, not a database constraint — the same class of rule, and the same layered-defense posture, already established for circular-renewal-chain prevention immediately above (a CHECK constraint cannot inspect sibling rows, so this is necessarily enforced at the point of transition in application code). The existing `uq_lease_contracts_one_active_per_apartment` partial unique index is retained unchanged and continues to be the sole database-level guarantee against multiple simultaneously-`active` contracts; this business rule closes the adjacent gap of two competing non-active (`draft`/`pending_signature`) contracts being drafted concurrently for the same unit, which the partial index alone does not prevent. No new table, column, or index is introduced by this rule.
- **Renewal lifecycle — activation timing (business rule; no schema change).** A renewal contract must **not** become `active` immediately upon creation. It is created with `status = 'draft'` or `status = 'pending_signature'` and **remains in that status until its own `start_date`**. Only at the point the predecessor contract's term actually ends does the following occur, as a single application-orchestrated database transaction:
  1. The renewal (new) contract's `status` transitions to `active`.
  2. The predecessor contract's `status` transitions to `superseded` (and, where applicable, `renewed`, per the status-semantics note immediately below).
  3. `contract_status_history` (§5.4) records both transitions, in the same transaction.

  This transaction must preserve the one-active-lease-per-apartment invariant already enforced by `uq_lease_contracts_one_active_per_apartment` at every point — the predecessor's transition to `superseded` and the renewal's transition to `active` occur together, so the partial unique index is never violated mid-sequence. No new status value is introduced; this rule only clarifies the timing and transactional grouping of the existing `active`/`superseded`/`renewed` statuses already defined on `contract_status_enum`.
- **Status semantics note:** `status = 'renewed'` vs `status = 'superseded'` — both describe "this contract has been succeeded by a new one," but are kept as distinct enum values to serve two different audiences: `superseded` is the immutability trigger (the moment ANY row's `prior_contract_id` points at this row, the row becomes read-only for financial terms at the application layer, regardless of *why* it was succeeded) while a status of `renewed` is reserved specifically for the sub-case where the succession was a voluntary tenant renewal (as opposed to, say, a contract that was terminated early and the unit re-leased to a *different* tenant, which supersedes nothing — that's simply a new, unrelated contract with `prior_contract_id = NULL`). In practice, `renewed` is set immediately alongside `superseded` semantics for the common renewal path; the distinction matters for reporting ("renewal rate" as a business KPI) rather than for the immutability mechanism itself, which keys off "is this row referenced by another row's `prior_contract_id`," not off the specific status value.
- **Draft Lease Editability:** A lease contract in `draft` status is editable prior to signature workflow or activation. Once a contract transitions out of `draft` (to `pending_signature`, `active`, `terminated`, `expired`, `superseded`, or `cancelled`), its legal and commercial terms become strictly immutable. Editing a `draft` lease contract allows updating commercial terms (`apartment_id`, `tenant_id`, `start_date`, `end_date`, `monthly_rent_amount`, `security_deposit_amount`, `payment_frequency`, `payment_due_day`, `legal_regime`, `tenant_type`, `notes`) while keeping core identity (`id`, `company_id`, `prior_contract_id`, `contract_number`, `currency`, `status`) strictly locked. Any edit to `apartment_id`, `start_date`, or `end_date` must re-execute the non-terminal overlap check excluding the target contract. For a renewal draft (`prior_contract_id IS NOT NULL`), the predecessor relationship is immutable, and the `apartment_id` and `tenant_id` remain locked to match the predecessor contract. Editing a draft contract does not generate a `contract_status_history` row (since status remains `draft`), nor does it alter marketplace listing status or unit occupancy.
- `building_id` is derived from `apartment_id` at write/update time.

**Soft Delete Strategy:** Standard, but exceptionally rare in practice — a lease contract is a legal financial record; `deleted_at` here is reserved for genuine data-entry errors (a contract created entirely by mistake and never signed/active), rather than for correcting routine typos or field errors on an active draft, which should be updated directly via draft editing while in `draft` status. An `active` or `superseded` contract should, as a matter of operational policy, never be soft-deleted.

**Audit Requirements:** Every mutation logged to `audit_logs`; `status`, `monthly_rent_amount`, `security_deposit_amount`, `end_date`, and `prior_contract_id` changes are treated as high-severity events given their legal/financial weight. `contract_status_history` (§5.4) additionally captures every status transition specifically.

**Future Scalability Notes:** At 500,000 rows this table is comfortably single-table-sized — no partitioning need (leases don't have a natural "age out" query dimension the way logs/readings do; a 5-year-old superseded contract is still directly joined-to by its financial-history children indefinitely, and expiration/dashboard queries are always bounded by `company_id`/`status`, not by a time range needing partition pruning). The real future scale pressure from this module lands entirely on `rent_payments` (Module 6, dependent on this table's PK) via the 2,000,000+ payment-records target.

---

## 5.2 Table: `contract_terminations`

**Purpose:** Captures the discrete legal/financial event of a lease contract ending — whether by normal expiration, early termination, or legal eviction — and the financial settlement (deposit reconciliation) that results.

**Business Description:** One row per contract termination event, 1:1 with `lease_contracts`. This is the trigger point for deposit reconciliation, which touches `receipts` and `expenses` (Module 7), and the authoritative record of *why* and *how* a tenancy ended, distinct from the mere fact of the status change (which `contract_status_history` also records generically).

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| lease_contract_id | UUID | NO | — | The contract being terminated (1:1) |
| termination_type | termination_type_enum | NO | — | `normal_expiration` \| `early_termination` \| `mutual_agreement` \| `tenant_request` \| `owner_request` \| `legal_eviction` |
| termination_date | DATE | NO | — | The effective date the tenancy ends/ended |
| reason | TEXT | YES | NULL | Free-form elaboration beyond the categorical `termination_type` |
| notes | TEXT | YES | NULL | — |
| approved_by | UUID | YES | NULL | FK to `users` — the staff member who authorized the termination/settlement; nullable only for `normal_expiration` rows generated by the automated expiration job with no human approver |
| outstanding_balance | NUMERIC(12,3) | NO | 0 | Unpaid rent/fees owed by the tenant at time of termination |
| deposit_returned_amount | NUMERIC(12,3) | NO | 0 | Portion of `lease_contracts.security_deposit_amount` actually returned to the tenant |
| deposit_deduction_amount | NUMERIC(12,3) | NO | 0 | Portion withheld for damages/unpaid charges |
| deposit_deduction_reason | TEXT | YES | NULL | Required when `deposit_deduction_amount > 0` |
| final_utility_settlement_completed | BOOLEAN | NO | false | Whether the final utility re-billing/reconciliation (Phase 1 §1.17) has been closed out for this tenancy |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217, per Global Convention |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `lease_contract_id` → `lease_contracts(id)` ON DELETE RESTRICT — per Phase 2, termination is a discrete legal/financial event preserved permanently.
- `approved_by` → `users(id)` ON DELETE SET NULL.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_contract_terminations_lease_contract_id` on `(lease_contract_id)` WHERE `deleted_at IS NULL` — enforces the 1:1 cardinality (a contract is terminated exactly once; a corrected/re-entered termination record replaces via soft-delete-and-reinsert or an application-level update, never a second concurrent row).

**Check Constraints:**
- `chk_contract_terminations_balances_nonneg` — `outstanding_balance >= 0 AND deposit_returned_amount >= 0 AND deposit_deduction_amount >= 0`.
- `chk_contract_terminations_deduction_reason` — `deposit_deduction_amount = 0 OR deposit_deduction_reason IS NOT NULL` — a financial deduction against a tenant's deposit must be justified on record; this is a real dispute-prevention requirement, not a soft UX nicety, given deposit disputes are among the most common landlord-tenant conflicts in the Jordanian market (Phase 1 §1.22 makes the same point for maintenance photo evidence).
- `chk_contract_terminations_date_not_future` — `termination_date <= CURRENT_DATE` — mirrors `chk_meter_readings_date_not_future`'s reasoning: a termination is a record of something that has happened, not a future-scheduling field (a *planned* future termination is expressed via the contract's own `end_date` plus an upcoming `status` transition, not by pre-creating a termination row dated in the future).

**Indexes:**
- `uq_contract_terminations_lease_contract_id` (above) — doubles as the primary and only realistic lookup path for this table: "get the termination record for contract X" from the contract detail page. No additional index needed given the 1:1 cardinality — this table will never be queried company-wide independent of its parent contract as a primary access pattern (financial reporting on terminations goes through the Financial Module's own reporting tables, not a direct scan of this table).

**Deliberately omitted indexes:** no `company_id` index, no `termination_type` index. At 500,000 contracts with a fraction ever reaching termination (many will simply expire without an explicit termination record, or remain active), this table stays small; any company-wide or type-filtered termination reporting is a low-frequency analytical query better served by `report_snapshots` (Module 1 §1.33) than a bespoke hot-path index on a table this size and access frequency.

**Business Rules:** Creating a `contract_terminations` row is the same transaction that transitions `lease_contracts.status` to `terminated` (or leaves it to transition to `expired` via the scheduled job for `termination_type = 'normal_expiration'`) and writes the corresponding `contract_status_history` row — a single application-orchestrated transaction spanning three tables, consistent with how a lease renewal is also a single multi-table transaction (§5.0, §5.1).

**Soft Delete Strategy:** Standard, though in practice reserved for genuine data-entry correction (an incorrectly-recorded termination), not for reversing a real-world termination decision, which has already had downstream financial consequences (deposit already returned, unit already re-listed) that a soft-delete cannot cleanly undo.

**Audit Requirements:** All fields audit-logged; `deposit_deduction_amount`/`deposit_deduction_reason` and `outstanding_balance` changes are financially significant and always captured with before/after values.

**Future Scalability Notes:** Scales 1:1 (at most) with `lease_contracts` — bounded by the same 500,000-contract ceiling, comfortably sub-scale for any partitioning concern.

---

## 5.3 Table: `contract_status_history`

**Purpose:** Append-only log of every status transition a lease contract undergoes, purpose-built for the contract-lifecycle-timeline product feature (distinct from `audit_logs`, per §5.0's rationale).

**Business Description:** Every time `lease_contracts.status` changes — draft → pending_signature → active → (expired | renewed | terminated | cancelled) — a row is written here capturing the transition, its timestamp, its actor, and a business reason. This includes the renewal-boundary transaction described in §5.1 (a renewal's `pending_signature → active` transition and its predecessor's `active → superseded` transition), which writes two rows here in the same transaction.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| lease_contract_id | UUID | NO | — | The contract whose status changed |
| previous_status | contract_status_enum | YES | NULL | NULL only for the very first row of a contract's life (the initial `draft` creation has no "previous" state) |
| new_status | contract_status_enum | NO | — | — |
| changed_by | UUID | YES | NULL | FK to `users`; nullable for system-job-driven transitions (e.g., the automated expiration sweep) |
| changed_at | TIMESTAMPTZ | NO | now() | — |
| reason | TEXT | YES | NULL | Business justification for the transition, e.g. "tenant signed renewal," "non-payment after grace period," "mutual agreement to vacate early" |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `lease_contract_id` → `lease_contracts(id)` ON DELETE RESTRICT — preserved permanently; a contract's status timeline is core legal-traceability evidence.
- `changed_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** None — every transition is a distinct event by nature, mirroring `audit_logs`' "no dedup" philosophy (Module 3 §3.12).

**Check Constraints:**
- `chk_contract_status_history_no_noop` — `previous_status IS NULL OR previous_status != new_status` — rejects a logged "transition" that isn't actually a transition (previous and new status identical), which would only ever indicate an application bug, not a legitimate event worth recording.

**Indexes:**
- `idx_contract_status_history_contract_changed_at` composite on `(lease_contract_id, changed_at DESC)` — **the** query this table exists to serve: "full status timeline for contract X, most recent first" (the lease detail page's history tab). Equality-leading on `lease_contract_id`, `changed_at DESC` trailing so the common "most recent transition" access is a direct ordered index scan with no separate sort step, mirroring the identical pattern already established for `login_history`/`audit_logs` timeline queries in Module 3.
- `idx_contract_status_history_company_new_status` composite on `(company_id, new_status, changed_at DESC)` — supports a company-wide operational query: "show me every contract that transitioned to `terminated` (or any specific status) this month," used both for dashboard statistics (stated requirement) and for the automated-expiration-job's own audit trail ("confirm which contracts the job actually expired today").

**Deliberately omitted index:** no index on `changed_by` alone — "what status changes has this staff member made" is a low-frequency administrative-review query, not a hot path, and can be served adequately by a sequential scan within the (already small, per §5.0's "leases don't need partitioning") table at this module's actual row-count ceiling; adding a dedicated index for an infrequent query would be reflexive over-indexing.

**Business Rules:** Written in the same transaction as the `lease_contracts.status` UPDATE that it records — application-orchestrated (not a DB trigger), specifically because the `reason` field requires business context only the application layer has at the moment of the transition (a generic `AFTER UPDATE` trigger could capture `previous_status`/`new_status`/`changed_at` mechanically, but could never populate a meaningful `reason`, so the whole row is written at the application layer for consistency of approach rather than splitting the row's population between a trigger and application code). Per §5.1's renewal lifecycle rule, a renewal boundary writes two rows here in one transaction: the predecessor's `active → superseded` transition and the renewal's `pending_signature → active` (or `draft → active`) transition.

**Soft Delete Strategy:** Not applicable — pure append-only event log, identical philosophy to `login_history`/`audit_logs`/`meter_readings`: a status transition, once it happened, is a permanent historical fact that is never corrected in place (a data-entry mistake is handled by inserting a corrective row with an explanatory `reason`, never by editing or deleting the erroneous one).

**Audit Requirements:** This table is itself a specialized audit trail for one specific state machine; it is not additionally mirrored into `audit_logs` for every row (that would be pure duplication of an already-immutable, already-timestamped, already-actor-attributed record) — mirroring exactly how `login_history` is treated relative to `audit_logs` in Module 3 §3.7.

**Future Scalability Notes:** Grows with (contracts × average transitions-per-contract, typically 3–6 over a contract's life: draft → pending_signature → active → superseded/terminated/expired, sometimes more with a cancelled-and-redrafted path) — at 500,000 contracts, comfortably in the low millions of rows, well within single-table territory; unlike `meter_readings`/`audit_logs`/`login_history`, this table has no unbounded, ever-repeating-per-entity growth pattern (a single contract's status history is inherently short and finite), so no partitioning is warranted even at full target scale.

---

## 5.4 Table: `contract_documents`

**Purpose:** Supports multiple file attachments per lease contract (signed contract PDF, ID copies, income proof, miscellaneous attachments), integrating with the central `file_storage` governance table.

**Business Description:** A contract's complete legal packet is rarely a single file — Jordanian leasing practice typically involves the signed contract itself plus supporting identity/income documentation. This table is the ordered, categorized attachment set for one contract.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| lease_contract_id | UUID | NO | — | The contract this document belongs to |
| file_id | UUID | NO | — | FK to `file_storage` — the actual file metadata/blob pointer (forward reference — Documents module) |
| document_type | contract_document_type_enum | NO | 'other' | `signed_contract` \| `national_id_copy` \| `passport` \| `income_proof` \| `other` |
| description | VARCHAR(255) | YES | NULL | Free-form label, e.g. "Salary letter" |
| uploaded_by | UUID | YES | NULL | FK to `users`; nullable only for system-migrated/bulk-imported historical documents with no attributable uploader |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `lease_contract_id` → `lease_contracts(id)` ON DELETE RESTRICT — per the "orphan documents" integrity requirement, a document attachment can never exist without a valid owning contract.
- `file_id` → `file_storage(id)` ON DELETE RESTRICT — the file metadata record must outlive this join row (a document reference should never silently dangle if the file row itself is deleted elsewhere; RESTRICT forces an explicit, deliberate cleanup order rather than a silent orphan).
- `uploaded_by` → `users(id)` ON DELETE SET NULL.
- `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_contract_documents_contract_file` on `(lease_contract_id, file_id)` WHERE `deleted_at IS NULL` — the same physical file should not be attached twice to the same contract under two different rows (a genuine duplicate-upload data-entry error); does **not** prevent the same file being referenced by two *different* contracts' document rows (not a realistic scenario in practice, but not structurally harmful either, so not worth constraining against).

**Check Constraints:** None beyond enum typing and NOT NULL — `document_type` fully captures the domain's variability without needing conditional cross-column rules (unlike, say, `utility_meters`' scope/FK consistency, there is no scope-dependent nullable column here to validate against).

**Indexes:**
- `idx_contract_documents_lease_contract_id` on `(lease_contract_id)` WHERE `deleted_at IS NULL` — "list all documents for this contract," the lease detail page's documents tab — the only realistic access pattern for this table.
- `idx_contract_documents_type` composite on `(lease_contract_id, document_type)` WHERE `deleted_at IS NULL` — supports the documents tab's common categorized view ("show me just the signed contract" / "show me just ID documents") as a refinement of the base lookup above; justified as a genuinely distinct, frequently-used filter shape (the UI realistically renders documents grouped by type, not as a flat undifferentiated list) rather than reflexively added.

**Deliberately omitted index:** no standalone `file_id` index. "Which contract(s) reference this file" is not a stated or realistic product query (a `file_storage` row is created *for* a specific upload targeting a specific contract in the normal flow; reverse lookup from file to contract has no product feature behind it) — if ever needed for a data-integrity audit, a sequential scan against this table's modest row count (bounded by contracts × a handful of documents each) is entirely adequate without a dedicated index.

**Business Rules:** A `signed_contract`-typed document row is expected (application-enforced, not DB-enforced, since a `draft`-status contract legitimately has none yet) to exist before a contract transitions to `active` status — this is a workflow gate in application logic, not a database constraint, since it depends on the contract's current `status`, a cross-table condition better expressed at the application/service layer than a fragile CHECK-with-subquery (which PostgreSQL doesn't support natively for CHECK constraints in this form anyway).

**Soft Delete Strategy:** Standard — a superseded/withdrawn document (e.g., a corrected income-proof letter re-uploaded) is soft-deleted and replaced with a new row rather than the file being overwritten in place, preserving the historical documentation trail for dispute-resolution purposes.

**Audit Requirements:** Standard `audit_logs` coverage; upload and soft-delete events both logged, since removing a document from a contract's legal record is itself a traceable action.

**Future Scalability Notes:** Scales with (contracts × average documents-per-contract, typically 3–6) — comfortably in the low millions at 500,000 contracts, no partitioning need; actual file bytes live in object storage via `file_storage`, so this table itself stays narrow and cheap regardless of the underlying document sizes.

---

## 5.5 Database Integrity Summary

| Risk | Prevention Mechanism | Explanation |
|---|---|---|
| Overlapping active contracts on the same apartment | `uq_lease_contracts_one_active_per_apartment` partial unique on `(apartment_id) WHERE status='active'` | Atomically enforced by the database at INSERT/UPDATE time — no race-condition window between a check and an insert, unlike an application-level check-then-write |
| Overlapping draft/pending-signature contracts on the same apartment | Application-layer business rule (§5.1): validated before a contract transitions to `pending_signature`/`active`; renewals additionally require `new_contract.start_date >= prior_contract.end_date` | A cross-row invariant a single-row CHECK constraint cannot express; enforced at the transition point, the same layered posture already used for circular-renewal-chain prevention |
| Duplicate contract numbers within the same company | `uq_lease_contracts_company_contract_number` on `(company_id, contract_number)` | Company-scoped, not global — two companies may reuse the same number sequence independently |
| Invalid contract dates (end before start) | `chk_lease_contracts_dates` — `end_date > start_date` | Rejected at write time, not caught downstream in reporting |
| Invalid signed date (signed after term ended) | `chk_lease_contracts_signed_date` | Prevents a specific, real data-entry inconsistency without over-constraining legitimate pre/post-start-date signing timing |
| Orphan documents (dangling contract or file reference) | `contract_documents.lease_contract_id` and `.file_id` both `NOT NULL` with real FKs, `ON DELETE RESTRICT` | No conditional/polymorphic ambiguity, consistent with the receipts-table philosophy from Phase 1 §1.20 |
| Invalid renewals (renewal pointing at a nonexistent contract) | `prior_contract_id` real FK to `lease_contracts(id)`, `ON DELETE RESTRICT` | A renewal can never reference a contract that doesn't exist or is later hard-deleted out from under it |
| Circular renewal chains | `chk_lease_contracts_no_self_reference` (trivial 1-row case) + `uq_lease_contracts_prior_contract_id` (at-most-one-successor structural backstop) + application-layer chain-walk validation at renewal-creation time | Three layered defenses: the cheapest/DB-level catches the trivial case immediately; the uniqueness constraint eliminates "diamond" graphs; the application check handles the general n-cycle case, which is unreachable in practice anyway given renewals can only be created after their predecessor already exists |
| Renewal briefly overlapping-active with its predecessor | §5.1's renewal-lifecycle business rule: the renewal remains `pending_signature`/`draft` until its `start_date`; the renewal's transition to `active` and the predecessor's transition to `superseded` occur in the same transaction | Prevents any window where two contracts for the same apartment are both `active`, preserving `uq_lease_contracts_one_active_per_apartment` at every point |
| Deposit deduction without justification | `chk_contract_terminations_deduction_reason` | A financial deduction against a tenant's money must carry an on-record reason — real dispute-prevention requirement, not cosmetic |
| Future-dated termination | `chk_contract_terminations_date_not_future` | A termination row records something that already happened; planned future terminations belong on the contract's own `end_date`, not a pre-dated termination row |
| No-op status history entries | `chk_contract_status_history_no_noop` | Guards against an application bug logging a "transition" that changed nothing |

---

## 5.6 Security & Multi-Tenant Isolation

- **RBAC:** Contract creation/modification is gated by the existing `contracts.create`/`contracts.approve`/similar permission keys (Module 3, `permissions`) resolved via `role_permissions` — no new RBAC machinery needed in this module; it consumes the existing platform-wide permission catalog.
- **Audit Logs:** Every table in this module writes to `audit_logs` on mutation, per the Global Convention; `lease_contracts` financial-term changes and `contract_terminations` deposit fields are flagged high-severity.
- **Multi-Tenant Isolation:** Every table in this module carries a denormalized `company_id` and is RLS-ready under the standard `USING (company_id = current_setting('app.current_company_id')::uuid)` policy — no table in this module needs a non-standard RLS shape (unlike `refresh_tokens`/`login_history` in Module 3), since every row here belongs unambiguously to exactly one company with no cross-tenant legitimate-access case.
- **Soft Delete:** Standard across all four tables except the pure event log (`contract_status_history`, which is append-only by design like `login_history`/`meter_readings`). `lease_contracts` soft-delete is operationally rare by policy (§5.1).
- **Immutable Historical Contracts:** Enforced at the application layer today (checking `EXISTS (... WHERE prior_contract_id = :id)` before permitting financial-term UPDATEs), with a `BEFORE UPDATE` trigger as the recommended Phase 8 hardening so the guarantee survives even a direct SQL write bypassing the application — the identical belt-and-suspenders posture already applied to `audit_logs`' `REVOKE UPDATE, DELETE` recommendation in Module 3.

---

## 5.7 Scalability at Target Scale

| Scale point | Impact |
|---|---|
| 10,000 companies | Trivial for every table in this module — even at full contract volume, per-company row counts stay in the low hundreds to low thousands, well within any single index's efficient range. |
| 500,000 lease_contracts | Comfortably single-table. No time-range query pattern exists for this table (expiration/dashboard queries are `company_id`/`status`-bounded, not date-range-bounded in a way that would benefit from partition pruning), so — consistent with the identical reasoning already applied to `apartments` in Module 4 §4.4 — **partitioning is not recommended**. The composite indexes keyed on actual filter columns (`company_id`, `status`, `apartment_id`, `tenant_id`) are the correct scaling mechanism, not partitioning. `contract_status_history` grows to the low millions (3–6 rows per contract) but remains comfortably single-table for the same reason — no unbounded per-entity growth. |
| 2,000,000 rent_payments (Module 6, dependent on this module's PK) | Not a table in this module, but flagged here since it's the direct downstream consequence of `lease_contracts` volume: each contract generates dozens of payment rows over its life (monthly frequency × multi-year term). This is the clearest partitioning candidate in the *next* module, analogous to `meter_readings` in Module 4 — flagged now so Module 6's design starts from that assumption rather than discovering it mid-module. |
| Documents | Scales as a small multiple of `lease_contracts` (3–6 documents per contract) — low millions at most, no partitioning need at any point in the stated target scale. |

**Should any table in this module be partitioned?** No. Every table here either has a hard cardinality ceiling tied to `lease_contracts`' own (unpartitioned-by-design) row count, or — like `contract_status_history` — grows in a bounded, per-entity-finite way rather than the unbounded, ever-accumulating pattern that actually justifies partitioning (`meter_readings`, `audit_logs`, `login_history`). Applying partitioning here would be the same over-engineering mistake already explicitly avoided for `apartments` in Module 4 and `refresh_tokens` in Module 3 — a large-looking row count alone is not sufficient justification; the query-pattern test is what decides it, and this module's tables don't meet that test.

---

## Module 5 (Leasing) — Architecture Review

- **PostgreSQL Best Practices:** UUIDv7 PKs throughout, `TIMESTAMPTZ` for all temporal columns except genuine calendar dates (`start_date`, `end_date`, `termination_date`, etc., correctly typed `DATE` since a lease term boundary is a calendar-date concept, not a point-in-time-with-timezone concept — this distinction is applied consistently and correctly across the module), `NUMERIC(12,3)` for every JOD monetary column paired with an explicit `currency` column, matching the Global Convention exactly.
- **Security:** RBAC/audit/RLS/soft-delete all confirmed present and standard-shaped across every table in this module (§5.6); no gaps identified relative to the Module 3 security baseline this module inherits.
- **Performance:** The `uq_lease_contracts_one_active_per_apartment` and `idx_lease_contracts_expiration` indexes were specifically designed to double as both integrity constraints and the exact query shapes named in the brief ("Current Active Contract lookup," "Contract Expiration") — one structure serving both purposes wherever that overlap existed, avoiding parallel, potentially-drifting index+constraint pairs.
- **Referential Integrity:** Every FK in this module is `NOT NULL` and RESTRICT-on-delete except the deliberately nullable `prior_contract_id` (first-in-chain contracts) and the SET NULL actor-attribution columns (`created_by`/`updated_by`/`deleted_by`/`approved_by`/`changed_by`/`uploaded_by`), consistent with the platform-wide "preserve history, sever only the actor-attribution edge" pattern already established in every prior module.
- **Index Quality:** Every composite index in this module was checked for column-order correctness (equality-before-range/sort, `DESC` embedded where the query needs most-recent-first) — no index found with the wrong leading column. One index was deliberately **not** created and the omission explicitly justified: a standalone `building_id` index on `lease_contracts` (§5.1) — a real candidate a less careful pass might add reflexively, correctly identified as adding write cost without a corresponding read benefit given this module's actual stated query patterns, continuing the exact discipline established in Module 4 §4.3/§4.8.
- **Constraint Quality:** The circular-renewal-chain risk — flagged in the brief as a requirement but not reducible to a single CHECK constraint given PostgreSQL's inability to inspect other rows from a CHECK — is handled via the layered defense explicitly documented in §5.1 (trivial-case CHECK + uniqueness structural backstop + application-layer chain-walk), rather than either ignoring the general case or over-claiming that a CHECK constraint alone solves it. The overlapping-draft/pending-signature-contracts risk and the renewal-activation-timing rule are handled the same way — an explicit, documented application-layer business rule rather than either a silent gap or a false claim of full database enforcement. This kind of honest layered-defense documentation, rather than a false claim of complete DB-level enforcement, is the correct posture for a risk that genuinely spans the DB/application boundary.
- **Multi-Tenant Isolation:** Confirmed — every table denormalizes `company_id` directly (§5.6), no non-standard RLS shape required anywhere in this module, unlike Module 3's `refresh_tokens`/`login_history`.
- **Prisma / Entity Framework Compatibility:** The two partial unique indexes central to this module's integrity guarantees (`uq_lease_contracts_one_active_per_apartment`, `uq_lease_contracts_prior_contract_id`) are, per the Module 3 §3.0 compatibility note, **not expressible via Prisma's declarative `@@index`/`@@unique` DSL** (no `WHERE` clause support) and must be hand-written as raw SQL appended to the generated migration — flagged explicitly here, consistent with how every other partial index in this schema (Modules 1–4) has already been called out, so this doesn't get silently missed at implementation time. All GIN/trigram indexes (`idx_lease_contracts_contract_number_trgm`) require the same raw-SQL-migration treatment, plus a one-time `CREATE EXTENSION IF NOT EXISTS pg_trgm` already established as a prerequisite in Module 4.
- **Future Scalability:** `lease_contracts` and its direct children were evaluated against partitioning and deliberately found not to need it (§5.7), with the reasoning made explicit rather than defaulting to "large table therefore partition" — the correct, query-pattern-driven call, consistent with the identical `apartments`-table conclusion in Module 4. The one real forward-looking flag from this module is that `rent_payments` (Module 6) will need partitioning consideration from day one of that module's design, called out now so it isn't discovered as a surprise.
- **Maintainability:** Every deviation and every non-obvious modeling decision in this module (`contract_status_history` vs. `audit_logs` non-redundancy, `renewed` vs. `superseded` status semantics, the layered circular-chain defense, the overlapping-non-active-contracts business rule, and the renewal-activation-timing rule) is documented in place with its specific business and technical reasoning, continuing the "no undocumented deviation" discipline established and maintained without exception since Module 1.

---

*End of Module 5. Say "CONTINUE" for MODULE 6 — RENT PAYMENTS & CHEQUE HANDLING (rent_payments, cheque_details, payment schedules), which is the next table in the dependency chain flagged for partitioning consideration in §5.7.*

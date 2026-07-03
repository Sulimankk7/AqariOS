# PropertyOS — Principal Architecture Review Report

**Scope:** All 11 modules (48 tables) reviewed as one unified production database — Core, SaaS, Security, Properties, Leasing, Rent Payments & Cheque Handling, Financial Operations, Maintenance, Marketplace, Documents, Notifications.
**Reviewer role:** Principal Database Architect / Principal Software Architect / Senior PostgreSQL Performance Engineer
**Target stack:** PostgreSQL + Entity Framework Core

This report evaluates the design as submitted. It does not rewrite or regenerate any table. Where the documentation already anticipates and correctly resolves a risk (and says so), that is recorded as **Confirmed Correct**, not re-litigated as a new issue.

---

## 0. Headline Finding — Stated Review Scale vs. Designed Scale Mismatch

**Severity: Critical**
**Location:** Module 5 (Leasing) and Module 6 (Rent Payments) — schema-wide scalability strategy

**Description:** The review brief specifies target production scale as **20,000,000+ lease contracts** and **50,000,000+ payment records**. The actual physical design targets **500,000+ lease contracts** (Module 5 §5.8) and **2,000,000+ rent payments** (Module 6 §6.4) — roughly **40x** and **25x** below the stated production target, respectively. Every other module's scale target lines up with the brief (10,000 companies, 100,000 buildings, 2,000,000 apartments, millions of maintenance/marketplace/notification rows all match), which makes the Leasing/Rent Payments gap stand out as a real sizing miss rather than a rounding difference.

**Why it is a problem:** Every partitioning conclusion in Modules 5–6 was derived from the 500K/2M targets:
- `lease_contracts` was explicitly evaluated *against* partitioning because "500,000 rows... comfortably single-table-sized" (§5.1, §5.8). At 20,000,000 rows this conclusion must be re-run — the query-pattern argument (no time-range-bounded dominant query) still plausibly holds, but "comfortably single-table" no longer does, and the composite indexes (`idx_lease_contracts_company_status`, `idx_lease_contracts_expiration`, etc.) need to be re-verified for selectivity at 40x volume, particularly `idx_lease_contracts_contract_number_trgm` (GIN trigram indexes have real bloat/maintenance cost at that scale).
- `rent_payments`' quarterly (not monthly) partitioning grain was explicitly calibrated against a 2,000,000-row target with "~500,000 rows/monthly partition" math (§6.4). At 50,000,000 rows the same monthly partition would hold ~12.5M rows — the module's own stated reason for choosing quarterly over monthly ("500K/month is already a reasonable single-partition size") inverts at this scale; **monthly**, not quarterly, would likely be the correct grain, and the quarterly choice should be revisited.
- `payment_allocations` and `cheque_details` scale off `rent_payments`, so their un-partitioned conclusion also needs re-verification at 50M parent rows (cheque_details targeted 500K but would scale toward ~12M+ at this new baseline).

**Recommended fix:** Before production sign-off, either (a) confirm the smaller 500K/2M figures are the actual, correct business-realistic ceiling for the initial deployment horizon (in which case the review's own stated 20M/50M assumption should be corrected, not the schema), or (b) re-run the partitioning analysis for `lease_contracts` and `rent_payments` against the larger figures — most likely outcome: `rent_payments` should move from quarterly to monthly partitioning, and `lease_contracts` should get a documented (if still "no partition needed") re-confirmation at the larger scale rather than silently inheriting a conclusion computed at 1/40th the volume.

**Expected impact if unresolved:** No immediate correctness risk, but a real operational risk: partitions sized for 2M rows holding 12M+ rows each will have materially worse VACUUM times, larger per-partition indexes, and slower `DROP`/maintenance operations than intended — undermining the entire reason partitioning was chosen in the first place.

---

## 1. Normalization (1NF / 2NF / 3NF)

**Finding: Confirmed Correct**, with four explicitly-scoped, individually-justified departures, all correctly documented in place rather than silently introduced:

1. Small, stable, platform-governed domains as ENUMs (`governorate_enum`, `expense_category_enum`, `notification_type_enum`, every status/lifecycle enum). Appropriate — none of these are business-editable domains.
2. Company-scoped lookup tables with seeded defaults (`roles`, `document_categories`, `notification_templates`) where the domain is genuinely company-extensible. Appropriate.
3. Denormalized, always trigger-maintained cache columns (`apartments.occupancy_status`, `buildings.total_apartments_count`, `utility_meters.last_reading_value`, `rent_payments.amount_paid`/`due_date_status`). Each has a named maintenance trigger and a stated performance justification — this is the textbook-correct way to break 3NF for a cache, not a violation.
4. Narrowly-scoped JSONB escape hatches (`subscription_plans.feature_flags`, `audit_logs.metadata`, `efawateercom_transactions.raw_response`). Bounded and justified, not a dumping ground.

No unjustified violation was found anywhere in the 48 tables. One item flagged separately below (§4, `lease_contracts.contract_document_id`) is a *soft* duplication risk worth tracking even though it does not technically break 3NF (it's a nullable convenience pointer, not a repeating fact).

---

## 2. Referential Integrity

**Finding: Confirmed Correct** as the dominant pattern. FK discipline is applied with unusual consistency across all 11 modules:
- Structural parent-child FKs are `NOT NULL` + `ON DELETE RESTRICT` almost without exception, preserving financial/legal/operational history.
- Actor-attribution columns (`created_by`/`updated_by`/`deleted_by`/`uploaded_by`/`changed_by`/`approved_by`/`issued_by`/`reversed_by`) are uniformly `ON DELETE SET NULL`.
- Genuinely optional structural links (`apartments.parking_spots.default_apartment_id`, `maintenance_requests.apartment_id`/`tenant_id`) are `ON DELETE SET NULL`, correctly distinguished from mandatory structural links.
- No unintended circular FK chain was found. The one apparent candidate (`companies.created_by → users.id` alongside `users` having no `company_id`) is correctly not a cycle, and is explicitly called out as such (Module 3 §3.12).

### 2.1 Documentation Gap — `tenants` table never physically specified
**Severity: High**
**Location:** Referenced by `lease_contracts.tenant_id`, `rent_payments.tenant_id`, `cheque_details.tenant_id`, `maintenance_requests.tenant_id`, `viewing_requests` (conceptually), and Phase 1 §1.9–§1.12.

**Description:** `tenants`, `tenant_family_members`, `tenant_emergency_contacts`, and `tenant_vehicles` are used as FK targets throughout Modules 4–9 and are conceptually described in Phase 1, but no module in the uploaded set gives them a full column-level physical specification (PK, FKs, unique constraints, indexes, check constraints) the way every other table receives. The summary in Module 11 acknowledges this ("assumed physically specified alongside Module 5 per the original module-brief ordering") but the actual table DDL-equivalent was never produced.

**Why it is a problem:** This is the review's single largest completeness gap. It's impossible to verify several stated integrity requirements without it — most importantly Phase 1 §1.9's "national ID must be unique per company" rule (no constraint can be confirmed), the optional `user_id` 1:1 link's cardinality enforcement, and whether `tenants` denormalizes `company_id` consistently with every other table in the schema. Every downstream financial-integrity claim in Modules 5–9 ("tenant payment history," "tenant reliability check," national-ID-based guarantor fraud checks) implicitly assumes a `tenants` table shape that was never actually verified.

**Recommended fix:** Produce the Module 5 (or dedicated) physical spec for `tenants`/`tenant_family_members`/`tenant_emergency_contacts`/`tenant_vehicles` before final sign-off, at the same rigor as every other table in this design (indexes, constraints, RLS, soft-delete strategy, national-ID uniqueness constraint scoped per-company as `uq_tenants_company_national_id`).

**Expected impact:** Low implementation risk (the pattern is well-established elsewhere in this schema and would likely follow it cleanly), but non-trivial audit risk — this table carries PDPL-covered personal data (national ID) and is exactly the kind of table that needs explicit review, not an assumed shape.

### 2.2 Documentation Gap — `file_storage` never physically specified
**Severity: Medium**
**Location:** Referenced as a mandatory `RESTRICT` FK target from `contract_documents`, `expense_receipts`, `maintenance_request_attachments`, `building_documents`, `listing_images`, `meter_readings.photo_file_id`, and `lease_contracts.contract_document_id`.

**Description:** Same class of gap as §2.1 — `file_storage` is Phase 1 §1.32's central file-governance table, referenced by name and by its `ON DELETE RESTRICT` behavior in at least six modules, but its own columns (visibility scope, storage key, MIME type, size, uploaded-by) are never physically specified in the uploaded documents.

**Why it is a problem:** Every module correctly assumes this table exists and behaves a certain way (mandatory, RESTRICT, one row per uploaded file), but its own RLS policy, indexing, and — critically — its `visibility` scope column (mentioned conceptually in Phase 1 as part of the access-control story) can't be verified. This matters most for the confidential-document story in Module 10 §10.6: `building_documents.is_confidential` gates read access at the `building_documents` row level, but if `file_storage` itself doesn't independently gate the underlying blob's own access, the file could still be reachable via a stale direct-storage-key link.

**Recommended fix:** Physically specify `file_storage` with the same rigor as every other table, explicitly stating how (or whether) its own access control cooperates with row-level flags like `building_documents.is_confidential`.

### 2.3 `report_definitions` / `report_snapshots` never physically specified
**Severity: Low**
**Location:** Phase 1 §1.33, referenced in Phase 2's relationship catalog (§2.1.J) but not detailed in any of the 11 delivered modules.

**Description:** Same completeness gap, lower severity since these tables sit outside the core transactional/financial path and Phase 2 already flags a real risk on them (unbounded JSONB payload / TOAST bloat) that was never resolved with an actual column design.

**Recommended fix:** Specify before the Reports feature is built; not a blocker for the 11 modules already delivered.

---

## 3. Data Integrity (Constraints, Business Rules, Duplicate Prevention)

**Finding: Confirmed Correct**, and one of this design's genuine strengths. Highlights:
- Layered defense (application-primary + documented Phase-8 DB-trigger hardening) is used consistently and honestly for every invariant a CHECK constraint structurally cannot express: circular lease-renewal-chain prevention (Module 5 §5.1), payment over-allocation (Module 6 §6.3, and this one *is* already a real trigger, not deferred), marketplace vacancy-gated publishing (Module 9 §9.0), status-transition graphs (Modules 5, 6, 8, 9, 11), and confidential-document/notification-ownership RLS predicates (Modules 10, 11).
- Partial unique indexes are the correctly-chosen, atomic, race-condition-free mechanism for every "at most one active X per Y" rule (`uq_lease_contracts_one_active_per_apartment`, `uq_company_subscriptions_one_active`, `uq_parking_assignments_active_spot`, `uq_marketplace_listings_one_active_per_apartment`, `uq_listing_images_one_cover`, `uq_notification_deliveries_notification_channel`).
- Sign/positivity CHECK constraints are applied consistently and correctly across every monetary column, with the one deliberate, well-reasoned exception (`rent_payments.amount_due >= 0`, allowing zero for adjustment rows) explicitly called out.

### 3.1 `rent_payments.due_date_status` includes a `'bounced'` value with no documented derivation path
**Severity: Medium**
**Location:** Module 6 §6.1, `rent_payments.due_date_status` enum and its maintaining-trigger business rules.

**Description:** The enum domain is `pending | paid | partially_paid | late | overdue_unpaid | bounced | cancelled`. The trigger logic documented immediately below the column (the `amount_paid`-vs-`amount_due`-vs-`due_date` derivation rules) enumerates five derived states (`pending`, `overdue_unpaid`, `partially_paid`, `late`, `paid`) plus the explicitly-application-set `cancelled` state — but never states when or how `due_date_status = 'bounced'` is reached. The cheque-bounce reversal mechanism (§6.2) is described as causing the trigger to *recompute* the obligation "back down accordingly (e.g., from `paid` back to `pending`/`overdue_unpaid`)" — i.e., bounce reversal drives the row back to one of the five derived states, not to a distinct `'bounced'` value.

**Why it is a problem:** Either this is a dead enum value that should be removed (cheque-level bounce status already lives on `cheque_details.status`, so duplicating it on `rent_payments.due_date_status` may be unintentional scope creep), or it's a real, intended state that the trigger's documented logic is simply missing a branch for. As written, the schema permits a value the business rules never explain how to reach — a genuine ambiguity a future engineer would have to guess at.

**Recommended fix:** Either remove `'bounced'` from `due_date_status_enum` (recommended — `cheque_details.status = 'bounced'` combined with the recomputed `pending`/`overdue_unpaid`/`late` value on the obligation row already captures the full picture without duplicating cheque-specific state onto the payment-obligation enum), or explicitly document the derivation branch if it's intentionally retained.

### 3.2 `rent_payments.due_date` nullability conflicts with the partitioning key requirement
**Severity: High**
**Location:** Module 6 §6.1 (`chk_rent_payments_scheduled_period_purpose`) vs. §6.4 (partition key requirement).

**Description:** `due_date` is only guaranteed `NOT NULL` for `payment_purpose = 'scheduled_installment'` rows via `chk_rent_payments_scheduled_period_purpose`; it is legitimately nullable for `unallocated_receipt`/`adjustment` rows. §6.4 then recommends `RANGE PARTITION BY due_date`, and separately notes this requires "an application-layer convention of defaulting `due_date` to the row's `created_at::date` at insert time" to give partitioning a non-NULL routing key — but this convention is not itself enforced by any constraint.

**Why it is a problem:** PostgreSQL native partitioning requires a non-NULL partition key value for standard range routing. Relying on an *application-layer convention* with no DB-level backstop (e.g., a `NOT NULL` constraint on `due_date` with a computed default, or a `BEFORE INSERT` trigger) means a single missed code path in a future service (a data-migration script, an admin tool, a new microservice) can insert a NULL-`due_date` row that either fails at the partition-routing layer or silently lands in a default/catch-all partition, undermining the partitioning scheme's own correctness guarantee.

**Recommended fix:** Either make `due_date` genuinely `NOT NULL` schema-wide (populated at INSERT time from `created_at::date` for non-scheduled rows, same as the documented app convention, but enforced via a `DEFAULT`/trigger rather than convention alone), or add an explicit `DEFAULT partition` to the partitioned table so a stray NULL-key row is routed somewhere deterministic rather than erroring or being silently mis-clustered.

### 3.3 `lease_contracts.contract_document_id` — soft duplication risk against `contract_documents`
**Severity: Low**
**Location:** Module 5 §5.1 (`contract_document_id`) vs. §5.5 (`contract_documents`, `document_type = 'signed_contract'`).

**Description:** `lease_contracts` carries a direct, nullable `contract_document_id → file_storage` "convenience pointer to *the* canonical signed document," explicitly distinct from the fuller `contract_documents` child table which also supports a `signed_contract`-typed row. The module acknowledges this is a convenience denormalization, not a second source of truth, but no mechanism (trigger, application invariant, or constraint) is documented to keep the two in sync, and no rule states which one wins if they diverge (e.g., `contract_document_id` points at file A while the `contract_documents` row typed `signed_contract` points at file B).

**Why it is a problem:** Two independently-writable paths to "the signed contract" is a classic source of drift, especially since `contract_documents` supports soft-delete-and-replace (a corrected signed-contract upload creates a new `contract_documents` row) — nothing forces `lease_contracts.contract_document_id` to be updated in that same transaction.

**Recommended fix:** Either (a) document explicitly that `contract_document_id` is intentionally allowed to lag and is refreshed only opportunistically (acceptable if stated), or (b) enforce it's updated in the same application transaction that inserts/replaces the `signed_contract`-typed `contract_documents` row. Not urgent, but worth an explicit one-line business rule rather than leaving the sync obligation implicit.

---

## 4. Table Design (columns, naming, UUID usage)

**Finding: Confirmed Correct** on UUIDv7 usage (consistent, correctly justified over UUIDv4 for index locality), on avoiding unnecessary columns, and on the `snake_case`/`<referenced_singular>_id` naming convention being followed almost universally.

### 4.1 Status-column naming is inconsistent across modules
**Severity: Low**
**Location:** Schema-wide.

**Description:** Most lifecycle tables use a bare `status` column (`lease_contracts.status`, `maintenance_requests.status`, `marketplace_listings.status`, `notifications.status`, `cheque_details.status`, `company_subscriptions.status`), but two tables deviate: `rent_payments.due_date_status` and `viewing_requests.request_status`. Both deviations are individually reasonable in isolation (disambiguating from a generic "status" concept), but the review's own naming-consistency checklist calls this out as worth flagging: a future engineer writing a generic status-filter helper across tables will hit two exceptions to an otherwise uniform convention.

**Recommended fix:** Optional, cosmetic — no functional risk. If a future major-version migration touches these tables anyway, consider renaming to `status` for consistency, or explicitly document the naming exception once at the schema-conventions level rather than only per-table.

### 4.2 `payment_allocations.receiving_payment_id` / `obligation_payment_id` deviate from `<referenced_singular>_id` convention
**Severity: Low (Confirmed Intentional)**
**Location:** Module 6 §6.3.

**Description:** Both columns FK to `rent_payments(id)` but are not named `rent_payment_id` (which would be ambiguous given the table plays two roles in one row). This is a correct, necessary deviation from the naming convention — flagged here only for completeness per the review's naming-consistency checklist, not as a defect.

### 4.3 Redundant-looking `roles.is_system` alongside `company_id IS NULL`
**Finding: Confirmed Correct.** Module 3 §3.3 flags this explicitly and it is retained deliberately (belt-and-suspenders against a future NULL-handling bug in application code) — not a redundant column, a deliberate defensive one. No action needed.

---

## 5. Relationships (1:1, 1:Many, M:M)

**Finding: Confirmed Correct.** Every conceptual M:M relationship with business-meaningful attributes is correctly modeled as a first-class entity, never a bare join table — `user_company_roles`, `role_permissions`, `parking_assignments`, `payment_allocations`. Every 1:1 extension (`company_settings`, `building_addresses`, `company_receipt_sequences`, `contract_terminations`, `cheque_details`) correctly uses a unique constraint on the FK column to enforce cardinality rather than relying on convention. No mis-modeled relationship was found.

One relationship remains genuinely deferred rather than solved (correctly, per the design's own scope discipline): the dual-nullable `notifications.recipient_user_id`/(absent)`tenant_id` recipient shape from Phase 1 §1.27 was intentionally narrowed to a single mandatory `recipient_user_id` in Module 11, meaning a tenant without portal access cannot be a direct notification recipient in the current design. This is explicitly flagged by the module itself as a known, scoped limitation with a clean extension path — **Confirmed Correctly Documented**, not an oversight, but worth surfacing again here since it has a real product consequence (SMS/WhatsApp-only tenants cannot receive `notifications` rows today) that a stakeholder should consciously sign off on before launch.

---

## 6. Indexes

**Finding: Confirmed Correct** as the dominant pattern, and unusually disciplined for a document of this size. Every module explicitly maps every stated required query shape to a specific index (§X.6/§X.7 sections throughout), and — notably — every module also explicitly documents indexes that were *considered and rejected* (e.g., `idx_role_permissions_role_id` caught as redundant in Module 3 §3.5; `idx_listing_images_listing_display_order` caught as redundant with a unique constraint's backing index in Module 9 §9.2; a standalone `is_confidential` index correctly rejected in Module 10 §10.2). This is genuinely good practice and was verified consistent across all 11 modules — no missing index was found for any stated query requirement, and no clearly unnecessary index was found that the modules' own review sections didn't already catch and reject.

### 6.1 GIN trigram indexes — cumulative write-amplification not separately assessed
**Severity: Low**
**Location:** Schema-wide — `idx_buildings_name_trgm`, `idx_building_addresses_area_trgm`, `idx_lease_contracts_contract_number_trgm`, `idx_expenses_vendor_trgm`, `idx_rent_payments_payment_reference_trgm`, `idx_cheque_details_cheque_number_trgm`, `idx_maintenance_requests_title_trgm`, `idx_marketplace_listings_title_trgm`, `idx_building_documents_document_name_trgm`, `idx_notifications_subject_trgm`.

**Description:** Each individual GIN trigram index is well-justified per-table against a real search requirement, and each module correctly declines to add a *second* trigram index on a long-form description field. However, no module assesses the **cumulative** write-amplification cost of maintaining ten separate GIN indexes schema-wide, several on tables with very high write volume (`rent_payments`, `notifications`). GIN indexes have materially higher per-write maintenance cost than B-tree.

**Recommended fix:** Not a defect, but worth a one-time production load test specifically measuring INSERT throughput on `notifications` and `rent_payments` with their trigram indexes live, before assuming the per-table analysis generalizes safely in aggregate.

---

## 7. Query Performance

**Finding: Confirmed Correct.** The design consistently favors composite indexes with equality-before-range/sort column ordering and trailing `DESC` timestamp columns for keyset pagination, applied without exception from Module 3 onward. Dashboard, buildings, apartments, leases, payments, marketplace, maintenance, documents, and notifications query shapes were each explicitly named and each explicitly mapped to a covering index. No expensive unindexed query pattern was identified in any module's stated requirements.

One area worth flagging:

### 7.1 `marketplace_listings` public browse index evaluated only for read cost, not for anonymous-traffic abuse
**Severity: Low**
**Location:** Module 9 §9.6, `idx_marketplace_listings_public_published_featured`.

**Description:** The module correctly identifies this as the first genuinely public, unauthenticated-traffic hot path in the schema and optimizes it well for legitimate load. It does not address rate-limiting/query-cost abuse from unauthenticated scraping (e.g., deep `OFFSET`-independent but still expensive pagination sweeps, or adversarial `ILIKE`-pattern trigram-search abuse via `idx_marketplace_listings_title_trgm`, which is also reachable without authentication).

**Recommended fix:** Application/API-gateway-layer concern, not a schema defect — flagged for completeness since the module explicitly calls out `viewing_requests` INSERT abuse (§9.7, rate-limiting recommended) but doesn't apply the same lens to the public *read* path.

---

## 8. Scalability

Covered in depth in §0 (headline finding). Summary by table family against the review's stated target scale:

| Table family | Review's stated target | Module's designed target | Verdict |
|---|---|---|---|
| companies | 10,000 | 10,000+ | Matches |
| buildings | 100,000 | 100,000+ | Matches |
| apartments | 2,000,000 | 2,000,000+ | Matches |
| **lease_contracts** | **20,000,000** | **500,000+** | **Mismatch — 40x under target; re-verify partitioning conclusion** |
| **rent_payments** | **50,000,000** | **2,000,000+** | **Mismatch — 25x under target; re-verify quarterly partition grain** |
| maintenance_requests | millions | 5,000,000+ | Matches |
| marketplace_listings | millions | 2,000,000+ | Matches |
| notifications | millions | 50,000,000+ | Matches (exceeds) |

Every partitioning decision that *was* made at the correctly-scaled tables (`meter_readings`, `audit_logs`, `login_history`, `expenses`, `notifications`, `notification_deliveries`) is well-reasoned and query-pattern-driven, not row-count-driven, and is **Confirmed Correct**. The only scalability risk in this design is the two tables sized against a stale (much smaller) target, per §0.

---

## 9. Security

**Finding: Confirmed Correct** as the dominant pattern:
- RBAC (`roles`/`permissions`/`role_permissions`/`user_company_roles`) is the single, consistently-reused authorization mechanism — no module invents parallel authorization machinery.
- Tenant isolation via `company_id` + RLS is applied to every table, with exactly three deliberate, individually-justified deviations, each documented: `users`/`permissions` (platform-wide roots), `refresh_tokens`/`login_history` (join-based RLS through `users`), and `marketplace_listings`/`listing_images` (public-read carve-out).
- `audit_logs` coverage is comprehensive and severity-calibrated by actual consequence, not applied uniformly.
- Soft-delete deviations fall cleanly into three explicitly-documented classes (pure event logs, no-independent-lifecycle 1:1 extensions, and the one "must never be deleted but is mutated in place" case — `notification_deliveries`) — no undocumented deviation was found.
- Confidential-data handling (`building_documents.is_confidential`, `notifications` recipient-ownership) is addressed with an honest layered posture rather than an overclaim.

### 9.1 Deferred (Phase 8) trigger-level hardening — production-readiness gap if not implemented before launch
**Severity: High**
**Location:** Schema-wide — every instance of "application-layer today, DB-trigger hardening recommended for Phase 8."

**Description:** Several genuinely important invariants are enforced **only at the application layer today**, with a documented (but not yet built) DB-trigger backstop deferred to "Phase 8":
- `lease_contracts` financial-term immutability once superseded (Module 5 §5.1, §5.7)
- Full lease-renewal circular-chain prevention beyond the trivial 1-row case (Module 5 §5.1 — partially backstopped by a uniqueness constraint, but the general n-cycle case is application-only)
- `maintenance_requests`/`cheque_details`/`marketplace_listings`/`notifications` full status-transition **graphs** (only the stateless, single-row-checkable sub-rules are DB-enforced)
- Marketplace publish-time vacancy gate (`apartments.occupancy_status = 'vacant'` check before `draft → published`)
- `building_documents.is_confidential` DB-level RLS predicate (currently application-layer only)
- `notifications` recipient-ownership RLS predicate (currently application-layer only)
- `audit_logs` `REVOKE UPDATE, DELETE` database-role-level protection (currently convention-only)

**Why it is a problem:** Every one of these is individually well-reasoned and honestly labeled — this is not sloppy design, it's transparent, deliberate scoping. But "Phase 8" is not defined anywhere in the reviewed materials as a committed, scheduled deliverable — it reads as an indefinitely-deferred aspiration. Application-layer-only enforcement means a compromised application credential, a buggy admin tool, a data-migration script, or a future microservice bypassing the main application's business-logic layer can violate every one of these invariants with **no database-level backstop**. For a system holding legal/financial records (lease terms, receipt numbering, confidential ownership documents) this is a materially different risk posture than "defense in depth already built."

**Recommended fix:** Before production launch, explicitly triage this list: which of these seven items are must-fix-before-launch (recommend: `audit_logs` REVOKE, and the `lease_contracts` immutability trigger, given their outsized legal/financial consequence) versus genuinely acceptable to ship application-layer-only for v1 with a committed near-term follow-up date. Do not let "Phase 8" remain an undated placeholder in the production go-live decision.

**Expected impact:** This is the review's second-most-important finding after §0. It does not indicate the schema is wrong — it indicates the schema's *documented* security posture is more defense-in-depth than what will actually be *running* on day one unless this list is explicitly worked through.

---

## 10. Financial Integrity

**Finding: Confirmed Correct**, and the strongest section of the entire design. Specific strengths verified:
- The `rent_payments`/`payment_allocations` split correctly solves partial payment, overpayment, and multi-period settlement without ever mutating historical facts in place (Module 6 §6.0, §6.3) — this is genuinely necessary complexity, not over-engineering, and the module's own justification for *not* further splitting into a fourth "payment event" table is sound (it would force every simple 1:1 payment through an unnecessary extra join).
- Over-allocation and negative-balance prevention is enforced by an actual `BEFORE INSERT OR UPDATE` trigger (§6.3) — not deferred to Phase 8 — correctly recognized as too important to leave application-layer-only, in direct and welcome contrast to the deferred items in §9.1 above.
- The receipt-numbering mechanism (`company_receipt_sequences`, Module 7 §7.4) correctly uses an atomic row-lock `UPDATE ... RETURNING` pattern, has documented gap-tolerance as an accepted tradeoff (not a bug), and is backstopped by a genuine unique constraint — this is production-grade design for a legally-significant sequential numbering requirement.
- Cheque lifecycle state (Module 6 §6.2) correctly models the real-world bounce-then-redeposit-then-clear scenario rather than over-constraining it away, and correctly ties reversal of a bounced cheque's allocations back through the same allocation/trigger machinery rather than a special-cased path.
- `expense_receipts`' explicit non-enforcement of aggregate consistency against parent `expenses.amount` (Module 7 §7.2) versus `payment_allocations`' strict enforcement is a deliberately different integrity posture for a documentary-evidence relationship versus a financial-settlement relationship — correctly reasoned, not an inconsistency.

No finding beyond §3.1/§3.2 above (both already logged against `rent_payments`).

---

## 11. Marketplace

**Finding: Confirmed Correct.** The INTERNAL-only scope narrowing from Phase 1's dual-mode design is explicitly flagged with a clean future-extension path. The vacancy-gate/publish-decoupling from `apartments.occupancy_status` is correctly modeled as a one-time gate, not an ongoing sync. The public-read/public-write-only RLS carve-out (published listings readable publicly; viewing-request submission is public-write but not public-read) is the correct, minimal cross-tenant surface — no broader relaxation of isolation was found. `archived`-as-status-not-soft-delete is correctly distinguished from genuine data-entry-error soft-delete. No issues beyond §7.1 above (anonymous-traffic abuse surface, application-layer concern).

---

## 12. Maintenance

**Finding: Confirmed Correct.** The `internal_notes` (scratchpad) vs. `maintenance_request_comments` (threaded, individually-authored) distinction is correctly non-redundant. `maintenance_status_history` is correctly append-only and correctly not partitioned (bounded per-entity growth, unlike `audit_logs`/`login_history`). The `closed_date`/status correlation constraint correctly excludes `resolved` from the terminal branch to model the real "fixed but not yet formally closed" gap. The decision to evaluate `maintenance_requests` *against* partitioning despite its 5,000,000-row target, on the grounds that its dominant query ("Open Requests") is status-bounded rather than date-bounded, is well-reasoned and internally consistent with how `apartments`/`lease_contracts`/`marketplace_listings` were treated. No issues found.

---

## 13. Documents

**Finding: Confirmed Correct.** `document_categories` as a company-scoped table (not an enum, not a system/custom split like `roles`) is well-justified against the stated "categories are managed per company" requirement, with the divergence from the `roles` pattern explicitly explained (no platform-level security invariant comparable to the immutable Owner role). The `is_confidential` intra-tenant access model (§10.6) is the schema's first genuinely novel security shape and is handled with appropriate honesty about what's enforced today (application layer) versus recommended (RLS hardening) — see §9.1 above for the production-readiness consequence of that deferral. `uploaded_by`/`created_by` as two distinct columns is a narrow, explicitly-justified exception to the schema's usual single-actor-column consolidation, consistent with the identical exception already used for `maintenance_request_attachments`.

---

## 14. Notifications

**Finding: Confirmed Correct**, with the scope-narrowing caveat already noted in §5 (single mandatory `recipient_user_id`, no `tenant_id` path for portal-less tenants — explicitly flagged by the module itself, and worth a conscious stakeholder sign-off before launch given its product impact). The `notifications`/`notification_deliveries` split correctly mirrors the `rent_payments`/`payment_allocations` obligation-vs-settlement pattern. The decision to partition `notifications`/`notification_deliveries` monthly (in contrast to `marketplace_listings`/`maintenance_requests` being excluded from partitioning) is correctly and explicitly reasoned as a genuine difference in dominant query shape (time-ordered historical record vs. status-bounded active worklist), not a mechanical reapplication of either precedent. The recipient-ownership RLS predicate is the schema's second genuinely novel intra-tenant security shape — same production-readiness note as §9.1/§13 applies.

---

## 15. Naming Consistency

**Finding: Largely Confirmed Correct**, with the two Low-severity deviations already logged in §4.1 (`due_date_status`, `request_status` vs. the otherwise-universal `status`). `company_id`, `created_at`/`updated_at`/`created_by`/`updated_by`/`deleted_at`/`deleted_by`, `building_id`, `apartment_id`, `user_id`-family columns, and enum naming (`<domain>_enum`) are all applied with genuine, verified consistency across all 11 modules — this was checked table-by-table and no inconsistency beyond the two status-column names was found.

---

## 16. Production Readiness

The schema is **structurally production-grade** — the modeling discipline, the honesty about deferred hardening, and the consistent requirement-to-index mapping are well above what's typical at this stage of a project. It is not yet a "ship as-is" green light because of two concrete, resolvable gaps:

1. **§0** — the Leasing/Rent Payments scale mismatch must be reconciled (either the target is smaller than assumed, or the partitioning grain for `rent_payments` needs to move to monthly and `lease_contracts` needs a re-confirmed no-partition decision at the real target).
2. **§9.1** — the "Phase 8" deferred-hardening list must be explicitly triaged into launch-blocking vs. acceptable-for-v1, rather than left as an undated aspiration, given that several items (receipt-numbering-adjacent `lease_contracts` immutability, `audit_logs` write protection) carry real legal/financial consequence if bypassed.

Neither gap requires a redesign — both are "finish what's already correctly planned" items, not architectural defects.

---

## Scoring

| Category | Score |
|---|---|
| **Overall Score** | **86 / 100** |
| Production Readiness | 74 / 100 |
| Performance | 92 / 100 |
| Security | 82 / 100 |
| Scalability | 78 / 100 |
| Maintainability | 95 / 100 |
| Data Integrity | 90 / 100 |

**Scoring rationale (brief):** Maintainability and Performance are the strongest dimensions — the module-by-module self-review discipline (caught redundancies, explicitly-rejected reflexive indexes, documented deviations) is genuinely rare and high quality. Data Integrity is very strong, pulled down slightly only by §3.1/§3.2's `rent_payments` findings. Security is pulled down by the volume of items deferred to an undated "Phase 8" (§9.1) rather than by any actual design flaw. Scalability is pulled down specifically and only by the §0 mismatch. Production Readiness is the lowest score because it's the composite of the two open items above — both fixable in days, not a redesign.

---

## Final Determination

**Would you deploy this database in production? — NO, not yet — but close.**

This is not a rejection of the architecture. It is a "finish the last mile" verdict: the design itself is sound, and every open item below is a completion task against a plan that's already correctly laid out in the documentation, not a rework of a flawed plan.

### Must be fixed before deployment:
1. **Reconcile the Leasing/Rent Payments scale target (§0).** Confirm the real production ceiling for `lease_contracts`/`rent_payments`; if it's genuinely 20M/50M, revise `rent_payments`' partition grain to monthly and re-run the `lease_contracts` partitioning evaluation at the correct scale.
2. **Fix the partition-key nullability gap on `rent_payments.due_date` (§3.2)** before the partitioned table is created — this is a correctness issue for the partitioning migration itself, not just a performance nicety.
3. **Triage the "Phase 8" deferred-hardening list (§9.1)** into launch-blocking vs. deferred-with-a-date. At minimum, implement `audit_logs`' `REVOKE UPDATE, DELETE` and the `lease_contracts` financial-term-immutability trigger before go-live — both are cheap to build and protect the two highest-consequence invariants in the schema.
4. **Physically specify `tenants` and `file_storage` (§2.1, §2.2)** — these are referenced everywhere as if already fully designed; they need the same rigor as every other table before implementation starts, particularly `tenants` given its PDPL-covered personal data.
5. **Resolve the `due_date_status = 'bounced'` ambiguity (§3.1)** — either remove the value or document its derivation.

### If the above are addressed (optional improvements only, not blockers):
- Rename `due_date_status`/`request_status` to `status` for full naming consistency (cosmetic).
- Add an explicit sync rule (or remove) `lease_contracts.contract_document_id` relative to `contract_documents` (§4.3).
- Load-test cumulative GIN trigram write cost on `rent_payments`/`notifications` before assuming per-table analysis generalizes (§6.1).
- Add rate-limiting consideration for the public marketplace read/search path, mirroring what's already planned for `viewing_requests` writes (§7.1).
- Physically specify `report_definitions`/`report_snapshots` ahead of building the Reports feature (§2.3).

---

*End of Architecture Review Report.*

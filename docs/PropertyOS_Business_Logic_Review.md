# PropertyOS — Business Logic Review Report

**Reviewer role:** Principal Business Analyst / Product Architect / Senior ERP Consultant / Property Management Domain Expert (Jordan)
**Scope:** Business logic only — lifecycle rules, workflow correctness, financial consistency, Jordanian market fit. No SQL, indexing, or normalization commentary (already reviewed separately).
**Source material reviewed:** Phase 1 (Business Entity Analysis), Phase 2 (ERD & Relationship Catalog), Phase 3 Global Conventions, and Modules 1–11 (Core, SaaS, Security, Properties, Leasing, Rent Payments & Cheques, Financial Operations, Maintenance, Marketplace, Documents, Notifications).

---

## 1. Company Management

Company lifecycle (soft-delete-only, no cascading loss of financial/legal history), the immutable Owner role, and subscription-gated access are all sound and match how a Jordanian PM company or individual owner actually operates (many owners start unregistered, incorporate later — `commercial_registration_no` being optional correctly reflects this). RBAC via `roles`/`permissions`/`role_permissions` is a correct, proportionate design for companies ranging from a 3-building family operation to a 200-building enterprise.

**No issues found in this area — business logic is correct as designed.**

---

## 2. Buildings, Apartments, Occupancy, Vacancies

`occupancy_status` as a trigger-derived cache off active `lease_contracts` is the correct source of truth, and the Marketplace's "gate at publish-time, don't sync continuously" design (§9.0) is a defensible, explicitly-documented MVP decision.

### Finding 2.1 — No reconciliation workflow when a published listing's unit becomes occupied through another channel
**Severity:** Medium
**Location:** Module 9 §9.0, `marketplace_listings.status`
**Description:** The design explicitly acknowledges that if a unit tied to a `published` listing gets leased through a walk-in/offline channel, the listing does not auto-expire or flag for review — this is called "a business-policy decision, not modeled here."
**Why it's a problem:** In practice this is exactly the scenario that most damages tenant trust in a marketplace product — a prospective tenant calls about a unit that's already gone. Leaving this entirely undefined means it will not get built unless someone remembers to specify it later.
**Recommended Business Rule:** At minimum, the lease-creation transaction (Module 5) should check for any `published` listing on the apartment being leased and transition it to `rented` in the same transaction — this is a natural, low-cost addition to the existing "lease creation is a multi-table transaction" pattern already used elsewhere (renewal, termination).
**Expected Impact:** Eliminates stale/ghost listings without adding new tables or columns — pure workflow wiring.

---

## 3. Leasing

The renewal chain, immutability-after-supersession, and "one active lease per apartment" enforcement are all strong, well-reasoned business rules that correctly reflect Jordanian leasing practice (including `legal_regime` for Old Rent Law tenancies).

### Finding 3.1 — No prevention of overlapping future/draft leases for the same apartment
**Severity:** High
**Location:** Module 5 §5.1, `uq_lease_contracts_one_active_per_apartment`
**Description:** The uniqueness constraint only restricts rows with `status = 'active'`. Nothing prevents two `draft` or `pending_signature` contracts from being created for the same apartment with overlapping `start_date`/`end_date` ranges, and nothing validates that a renewal's `start_date` is not earlier than its predecessor's `end_date`.
**Why it's a problem:** A leasing agent could accidentally (or a second agent, unaware of the first, could independently) draft two competing future leases for the same unit. Both could reach `pending_signature` and be sent for tenant signature before anyone notices the double-booking. This is a real, common operational failure mode in a multi-staff leasing office.
**Recommended Business Rule:** At the point a contract transitions to `active` (or at `pending_signature`, to catch it earlier), validate that no other non-terminal (`draft`/`pending_signature`/`active`) contract exists for the same `apartment_id` whose date range overlaps. For renewals specifically, require the new contract's `start_date >= prior_contract's end_date`.
**Expected Impact:** Prevents double-leasing a unit and the resulting tenant-facing embarrassment/legal exposure.

### Finding 3.2 — Renewal timing relative to occupancy cache is unspecified
**Severity:** Low
**Location:** Module 4 §4.4 (occupancy trigger) × Module 5 (renewal)
**Description:** When a renewal contract is created and the predecessor moves to `superseded`, the occupancy-status trigger fires on `lease_contracts.status` changes — but a renewal is commonly signed weeks before the old term ends. If the new contract is created with `status='active'` immediately (rather than staying `pending_signature`/`draft` until the old term actually ends), the apartment's cached occupancy is unaffected in practice (still occupied) but the *active-lease* semantics briefly represent two contracts' worth of ambiguity if not sequenced carefully.
**Recommended Business Rule:** Document explicitly (as a business rule, not just an implicit consequence) that a renewal's new contract should remain `pending_signature` until its `start_date`, and only flip to `active` — in the same transaction that supersedes the old contract — at or after the predecessor's `end_date`.
**Expected Impact:** Removes ambiguity for implementers; closes the same class of gap as 3.1 at the renewal boundary specifically.

---

## 4. Rent Collection

The `rent_payments`/`payment_allocations` split correctly and elegantly solves partial payment, overpayment, and multi-period settlement — this is genuinely strong design work and matches real Jordanian collection patterns (partial payments, batched back-rent settlement).

### Finding 4.1 — Grace period is never applied to late-payment determination
**Severity:** High
**Location:** Module 1 §1.2 (`company_settings.rent_grace_period_days`) vs. Module 6 §6.1 (`due_date_status` derivation)
**Description:** `company_settings` defines a configurable grace period ("days after due date before a payment is flagged late"), but the `due_date_status` trigger logic in Module 6 compares `amount_paid`/`due_date` directly against `CURRENT_DATE` with no grace-period offset anywhere in the derivation.
**Why it's a problem:** A company-level setting that is captured but never consulted is not just an omission — it's actively misleading to a company admin who configures a 5-day grace period expecting it to change when a payment is flagged late/overdue, and it doesn't. Every company would show identical late-payment behavior regardless of their configured grace period.
**Recommended Business Rule:** The `due_date_status` derivation must offset its comparison date by the owning company's `company_settings.rent_grace_period_days` (i.e., a payment is `late`/`overdue_unpaid` only once `CURRENT_DATE > due_date + grace_period_days`).
**Expected Impact:** Makes an already-designed, already-exposed configuration setting actually function; directly affects collections dashboards, late-fee eligibility, and tenant-facing status.

### Finding 4.2 — Late fees are never actually applied
**Severity:** High
**Location:** Module 1 §1.2 (`company_settings.late_fee_type`/`late_fee_value`) vs. Modules 6–7
**Description:** `company_settings` captures a full late-fee policy (`none`/`fixed`/`percentage` + value), but no workflow anywhere in Modules 6 or 7 actually charges it — there is no automatic `rent_payments`/`adjustment` row, no `expenses` row, no notification, nothing that reflects a late fee actually being assessed against a late-paying tenant.
**Why it's a problem:** Late fees are a real, commonly-used lever for Jordanian landlords, and the schema was clearly designed with the intent to support them (the config exists), but the business process that turns configuration into a charged, collectible obligation is entirely missing.
**Recommended Business Rule:** When a `rent_payments` row's `due_date_status` transitions into `late`/`overdue_unpaid` past the grace period, and the company's `late_fee_type != 'none'`, generate a `rent_payments` row with `payment_purpose = 'adjustment'` representing the late fee, linked to the same contract, so it flows through the existing allocation/collections machinery unmodified.
**Expected Impact:** Turns an already-modeled policy into an enforceable one without new tables — pure workflow completion.

### Finding 4.3 — Bounced-cheque fee is recorded but never billed to the tenant
**Severity:** Medium
**Location:** Module 6 §6.2, `cheque_details.bounce_fee_charged`
**Description:** The column exists to record that a fee was charged, but no business rule connects it to an actual collectible ledger entry (a new obligation on `rent_payments`/`payment_allocations`).
**Why it's a problem:** Same class of gap as 4.2 — data is captured descriptively but doesn't participate in the actual receivables ledger, so "how much does this tenant currently owe including bounce fees" cannot be answered from the ledger alone.
**Recommended Business Rule:** Bounce-fee assessment should create a `payment_purpose = 'adjustment'` obligation row on the affected contract, mirroring the late-fee mechanism recommended in 4.2.
**Expected Impact:** Ensures the receivables ledger (not just the cheque sub-record) reflects the true amount owed.

**Otherwise correct and worth stating explicitly:** partial payments, overpayments, multi-period settlement, and receipt sequencing (gapless, cross-type continuous numbering) are all well-designed and production-appropriate.

---

## 5. Cheque Handling

**No financial inconsistency was found in the cheque lifecycle itself.** The bounce → reversal → recompute chain is correctly designed: a bounce reverses the specific `payment_allocations` row(s), which correctly cascades back through the trigger to `rent_payments.amount_paid`/`due_date_status`, and re-presentment (bounce → re-deposit → clear) is explicitly and correctly supported rather than blocked by an over-eager constraint. This is genuinely solid work — see Finding 4.3 above for the one adjacent gap (the fee itself isn't billed), but the core lifecycle is correct.

---

## 6. Financial Operations

### Finding 6.1 — `efawateercom_billers` (biller registration) was never built, but transactions assume it exists
**Severity:** High
**Location:** Module 7 §7.0 vs. Phase 1 §1.18
**Description:** Phase 1 conceptually required `efawateercom_billers` (a company's registered gateway credentials — "a company *without* a biller record simply cannot generate eFAWATEERcom-payable bills"). Module 7 explicitly drops this table from scope, leaving `efawateercom_transactions` with no way to verify a company is actually eligible to use the channel before a transaction is attempted.
**Why it's a problem:** Without a biller-registration record, there is no business gate preventing a company that never registered with eFAWATEERcom from having transactions recorded against it, and no way to expose "is this company eligible" to the tenant-facing payment flow.
**Recommended Business Rule:** Before the platform can be released with eFAWATEERcom as a live payment channel, either build the minimal `efawateercom_billers` table (even a narrow one) or explicitly gate `payment_method = 'efawateercom'` behind a company-level feature flag until biller registration exists.
**Expected Impact:** Prevents a broken/impossible payment flow from being technically representable in the data.

### Finding 6.2 — Maintenance-to-expense linkage described in Phase 2 was dropped in the physical build
**Severity:** Medium
**Location:** Phase 2 §2.1.E (`maintenance_requests |o--o| expenses`, 1:1) vs. Module 7 (`expenses`) and Module 8 (`maintenance_requests`)
**Description:** Phase 2's relationship catalog explicitly models an optional link from a maintenance request to the expense it generated ("not every maintenance request incurs a separately tracked expense row"). Neither `expenses` (Module 7) nor `maintenance_requests` (Module 8) carries this FK in the final physical design — the relationship is silently absent from both tables.
**Why it's a problem:** Cost-per-repair tracking ("how much did fixing the elevator actually cost, in total, across parts + labor") is a genuinely common Jordanian PM company need (building-level P&L, vendor cost review) and was explicitly designed for at the conceptual layer, then quietly dropped. Without it, a company cannot answer "what did this specific maintenance issue cost us" from the data.
**Recommended Business Rule:** Add a nullable `maintenance_request_id` FK on `expenses` (or vice versa), consistent with the original Phase 2 relationship, so a maintenance-driven cost can be traced back to its originating request.
**Expected Impact:** Restores a previously-designed, business-relevant capability that appears to have been lost in translation between the conceptual and physical design passes — not a new feature, a regression fix.

**Otherwise correct:** the receipts subsystem (gapless per-company numbering, non-polymorphic table-per-type, defense-in-depth against duplicate numbers) is fully auditable and production-appropriate. Expense categorization and building/company-wide cost attribution are sound.

---

## 7. Maintenance

Request → attachment → comment → status-history modeling is sound, and the `internal_notes` vs. `maintenance_request_comments` distinction is a sensible, real-world-matched design.

### Finding 7.1 — No staff/vendor assignment field, despite being referenced conceptually
**Severity:** Low (explicitly scoped out, but worth flagging for market fit)
**Location:** Phase 2 §2.1.F (`users |o--o{ maintenance_requests: assigned_to`) vs. Module 8 (explicitly excludes technicians/assignment)
**Description:** Phase 2's ERD includes an assignee relationship; Module 8's brief explicitly excludes "technicians, work orders... vendors" from scope, so no assignee column exists in the final table.
**Why it's a problem:** This is a deliberate, documented scope decision, not an oversight — but it does mean a request currently has no way to reflect "who is actually working this ticket" beyond an unstructured comment or the internal-notes field. For a company with more than a handful of maintenance staff, "whose queue is this in" is a basic operational question the current design cannot answer structurally.
**Recommended Business Rule:** Not a defect to fix before release given the explicit scope boundary — flagged only as a near-term roadmap item, since Jordanian PM companies of any real size (the stated scale target implies many do have multiple maintenance staff) will ask for this quickly after go-live.
**Expected Impact:** None for this release; informs the next module's priority.

**Otherwise correct:** status transitions, closed-date consistency, and the append-only status-history table are all well-designed and match how a Jordanian company actually works a repair ticket.

---

## 8. Marketplace

See Finding 2.1 (listing/occupancy reconciliation) above — this is the one real gap in this module. Everything else — the vacancy gate at publish-time, one-active-listing-per-apartment, the archived-is-a-status-not-a-delete distinction, and the public/private RLS split — is correctly designed and does **not** create inconsistent occupancy states in the schema itself; the risk is purely in the missing reconciliation workflow already flagged.

---

## 9. Documents

The `is_confidential` intra-tenant access model is well-designed for staff-vs-staff visibility. Expiring-document tracking (feeding the notification system via the `document_expiring` template type) correctly closes the loop Phase 1 flagged as a genuine compliance risk (civil-defense certificate lapses).

### Finding 9.1 — No tenant-facing document visibility model
**Severity:** Medium
**Location:** Module 10 (entire), cross-referenced against Requirement Area 11 ("View building documents (when permitted)")
**Description:** `building_documents` has exactly two visibility states relative to *company staff* (`is_confidential` true/false), both scoped entirely within the company's own authenticated staff. There is no column, flag, or RLS carve-out anywhere that allows a tenant to see any building document at all, "when permitted" or otherwise.
**Why it's a problem:** The stated Tenant Portal requirement explicitly expects tenants to be able to view building documents under some permission condition (e.g., a building's occupancy permit, or their own lease-adjacent compliance documents). As designed, there is no mechanism for this — a tenant has zero read access to `building_documents`, full stop.
**Recommended Business Rule:** Introduce a company-controlled visibility flag (e.g., `is_tenant_visible`) on `building_documents`, defaulting to false, that a company can opt into per document — separate from and in addition to the existing `is_confidential` staff-facing flag.
**Expected Impact:** Closes a stated Tenant Portal requirement that is currently unimplementable against the documented schema.

---

## 10. Notifications

The `notifications`/`notification_deliveries` split, content-immutability-after-send, and the recipient-ownership RLS layer are all strong, correctly-reasoned designs.

### Finding 10.1 — Tenants without portal access cannot receive any notification
**Severity:** High
**Location:** Module 11 §11.0, `notifications.recipient_user_id` (mandatory, single-FK)
**Description:** This is explicitly flagged in the module itself: since `recipient_user_id` is mandatory and a `tenants` row's `user_id` link is optional, a tenant who has never activated portal access — which Phase 1 §1.9 explicitly describes as "very common for older or less tech-comfortable tenants who pay via bank transfer and never touch the app" — cannot be the recipient of any notification at all.
**Why it's a problem:** This directly contradicts a business reality the platform itself already documented as common in this exact market. Rent-due reminders, late-payment notices, and document-expiry alerts — arguably the platform's single highest-value recurring communication — cannot reach a meaningful fraction of the actual tenant base (SMS/WhatsApp-only tenants) under the current design.
**Recommended Business Rule:** Before release, either (a) require portal-account creation at lease signing (a policy change, not a technical one, and one that cuts against the market reality the platform itself documented), or (b) add the previously-flagged nullable `tenant_id` alternative recipient path with a CHECK constraint enforcing exactly one of `recipient_user_id`/`tenant_id`, per the extension path the module itself already describes.
**Expected Impact:** This is likely the single most consequential business-logic gap in the entire platform for actual Jordanian market usability — rent reminders are a core value proposition, and a large share of the target user base cannot receive them as designed.

---

## 11. Tenant Portal

Cross-checking each stated capability against the documented schema:

| Capability | Status |
|---|---|
| View lease information | ✅ Supported (`lease_contracts`, tenant-scoped read) |
| View rent payments | ✅ Supported (`rent_payments`, `idx_rent_payments_tenant_due_date`) |
| View receipts | ✅ Supported (`rent_payment_receipts`, 1:1 off `rent_payments`) |
| Submit maintenance requests | ✅ Supported (`maintenance_requests.tenant_id`) |
| Track maintenance requests | ✅ Supported (`maintenance_status_history`, `maintenance_request_comments`) |
| View building documents (when permitted) | ❌ **Not implemented** — see Finding 9.1 |
| Pay rent | ⚠️ Partially supported — payment *recording* exists, but no tenant-initiated payment-submission workflow is described (staff/gateway-initiated only); acceptable if payment is always gateway/staff-mediated, but worth explicit confirmation |
| Pay utility bills via eFAWATEERcom | ❌ **Not implementable as scoped** — see Finding 11.1 below |

### Finding 11.1 — `utility_bills` was never physically built, despite being a stated Tenant Portal capability
**Severity:** Critical
**Location:** Phase 1 §1.17 (`utility_bills` fully specified conceptually) vs. the final 11-module physical build (table absent from every module, including the summary's full 48-table list)
**Description:** Phase 1 describes `utility_bills` in detail (dual calculation modes, mandatory single-lease-period billing, no-split-billing MVP rule) and Phase 2's relationship catalog fully wires it into `apartments`, `lease_contracts`, `utility_meters`, and `efawateercom_transactions`. It is never physically specified in any of Modules 1–11, and does not appear in the final 48-table summary.
**Why it's a problem:** The requirement set explicitly asks the tenant portal to support "pay utility bills through eFAWATEERcom integration" — but there is no table to represent a utility bill at all. `efawateercom_transactions` (Module 7) is scoped to `rent_payments` settlement *only*, by explicit brief instruction, with utility-bill settlement flagged as a future `efawateercom_utility_bill_transactions` sibling table not yet built. There is currently no way to generate, display, or pay a utility bill anywhere in the platform.
**Recommended Business Rule:** This is not a "missing business rule" so much as a missing module — `utility_bills` must be designed and built (it was already fully specified conceptually in Phase 1, so this is a completion task, not new scope) before "pay utility bills" can be considered a real, releasable capability.
**Expected Impact:** Without this, one of the explicitly-stated Tenant Portal requirements is entirely non-functional. Given how central utility re-billing is to Jordanian property management practice (Phase 1 itself calls this "the actual official utility bill often arrives to the building/company, not the individual tenant"), this is a market-critical gap, not a nice-to-have.

---

## 12. Jordanian Market Fit

The platform's core design choices are strongly matched to the Jordanian market: JOD 3-decimal currency handling, governorate/district/area addressing over Western postal codes, post-dated cheque lifecycle modeling (a genuine, correctly-prioritized market necessity), Old Rent Law tenancy flagging, Arabic/English bilingual fields throughout, and eFAWATEERcom as a first-class payment channel are all appropriate and non-speculative — nothing here is over-engineered or imported from a market this platform doesn't target.

**Missing processes that would normally exist in this market and are not yet covered:**
- **Utility re-billing to tenants** (Finding 11.1) — this is arguably the most Jordan-specific gap of all, since Phase 1 itself identifies manual authority-bill re-allocation as the *dominant* real-world pattern, not an edge case.
- **SMS/WhatsApp-only tenant communication** (Finding 10.1) — explicitly acknowledged elsewhere in the same documentation set as common in this market, yet unsupported by the notification design as built.
- **Late fee assessment** (Finding 4.2) — a real, commonly negotiated lease term in Jordan that is configured but not enforced.

No unnecessary or market-inappropriate features were identified — the review found no instance of over-engineering to flag.

---

## 13–14. Missing Business Rules & Edge Cases (Consolidated)

| Scenario | Currently Handled? | Reference |
|---|---|---|
| Tenant leaves early | ✅ `contract_terminations` with deposit reconciliation | Module 5 §5.3 |
| Lease renewal before expiration | ⚠️ Structurally possible but date-overlap unvalidated | Finding 3.1/3.2 |
| Returned (bounced) cheque | ✅ Correctly reversed through allocations | Module 6 §6.2 |
| Multiple partial payments | ✅ Correctly modeled via `payment_allocations` | Module 6 §6.3 |
| Expired marketplace listing | ✅ `published → expired` status transition | Module 9 §9.1 |
| Deleted (offboarded) employee | ✅ Membership soft-deleted, history preserved via `SET NULL` | Module 3 §3.2 |
| Deleted building | ✅ `RESTRICT` while any history exists | Module 4 §4.1 |
| Deleted apartment | ✅ Soft-delete only, history intact | Module 4 §4.4 |
| Archived/churned company | ✅ Subscription lapses; data never cascade-deleted | Module 1, Module 2 |
| Two competing draft leases for one unit | ❌ Not prevented | Finding 3.1 |
| Late fee assessment | ❌ Configured but never applied | Finding 4.2 |
| Bounce-fee billing to tenant | ❌ Recorded but not billed | Finding 4.3 |
| Utility bill generation/payment | ❌ Table never built | Finding 11.1 |
| Notification to portal-less tenant | ❌ Structurally impossible | Finding 10.1 |
| Listing vs. actual occupancy drift | ❌ No reconciliation | Finding 2.1 |
| Tenant document visibility | ❌ No mechanism | Finding 9.1 |
| Maintenance cost traceability | ❌ Link dropped from physical design | Finding 6.2 |
| eFAWATEERcom biller eligibility | ❌ No registration gate | Finding 6.1 |

---

## 15. Production Readiness

The **transactional core** — leasing, rent collection, cheque handling, receipts, and financial audit trail — is genuinely production-grade: internally consistent, well-reasoned, and free of the kind of silent data-corruption risk this review was specifically looking for (no way to double-allocate funds, no way to lose a financial record, no way to represent a logically impossible cheque state). This is strong work.

However, the platform as documented is **not yet complete against its own stated requirements**: one entire stated Tenant Portal capability (utility bill payment) has no supporting table at all, and one entire user segment (portal-less tenants) cannot receive notifications by design. These are not polish items — they are stated requirements with no implementation path in the current documentation.

---

## Scores

| Dimension | Score |
|---|---|
| Business Logic Score | 78 / 100 |
| Jordanian Market Fit Score | 82 / 100 |
| ERP Readiness Score | 68 / 100 |
| Maintainability Score | 90 / 100 |
| Business Consistency Score | 85 / 100 |

*(Maintainability scores highest because every deviation and decision across all 11 modules is exceptionally well self-documented — this made the gaps above easy to find precisely because so little else was hidden or unstated.)*

---

## Would you release this ERP to real property owners in Jordan?

## **NO**

The transactional core is release-quality, but the platform cannot yet deliver on requirements it explicitly commits to. The following business rules **must** be added before release:

1. **Build `utility_bills`** (and the minimal supporting eFAWATEERcom-settlement path) — without it, "pay utility bills" is not a real capability. *(Finding 11.1 — Critical)*
2. **Add a notification recipient path for tenants without portal access** (nullable `tenant_id` alongside `recipient_user_id`, per the extension already flagged in the module itself) — without it, a meaningful share of real tenants never receive a rent reminder. *(Finding 10.1 — High)*
3. **Apply the configured grace period to late-payment determination** — the setting already exists and is already exposed; it simply needs to be consulted. *(Finding 4.1 — High)*
4. **Prevent overlapping draft/future leases on the same apartment** — closes a real double-booking risk. *(Finding 3.1 — High)*
5. **Gate or complete eFAWATEERcom biller eligibility** before allowing the channel to go live for a company that never registered. *(Finding 6.1 — High)*

Once these five are addressed, the remaining findings (4.2, 4.3, 6.2, 9.1, 2.1, 3.2) are reasonable **optional improvements** for a fast-follow release rather than blockers — none of them create financial inconsistency or data loss risk on their own, they simply leave configured functionality inert or leave a stated capability partially unimplemented.

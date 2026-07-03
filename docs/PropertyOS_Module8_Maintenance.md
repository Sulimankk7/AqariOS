# MODULE 8 — MAINTENANCE

Continuation of `PropertyOS_Phase3_Physical_Design.md`, `PropertyOS_Module4_Properties.md`, `PropertyOS_Module5_Leasing.md`, `PropertyOS_Module6_RentPayments_Updated.md`, and `PropertyOS_Module7_FinancialOperations_Updated.md`. Applies the same Global Conventions (UUIDv7 PKs, `created_at`/`updated_at`/`created_by`/`updated_by`, soft delete, `company_id` + RLS tenant isolation, `snake_case` naming) unless a deviation is explicitly justified.

Scope for this module is deliberately narrow, per the module brief: **four tables only** — `maintenance_requests`, `maintenance_request_attachments`, `maintenance_request_comments`, `maintenance_status_history`. No technicians, work orders, inventory, scheduling, vendors, or quotations — those are explicitly out of scope and are not introduced anywhere below, even implicitly via a stray column.

Scale target for this module: **5,000,000+ maintenance requests, 20,000,000+ comments, 20,000,000+ attachments, 10,000+ companies.**

---

## 8.0 Module-Wide Design Decisions (read before the tables)

**"Created By" and "Author" are satisfied by the Global Convention's own `created_by` column, not a duplicate business column.** The brief lists "Created By" as a required field on `maintenance_requests` and "Author" as a required field on `maintenance_request_comments`. Rather than adding a second, parallel `reported_by_user_id`/`author_user_id` column that would sit alongside the Global Convention's own `created_by UUID NULL REFERENCES users(id)` and duplicate its exact meaning (who created this row), this module treats `created_by` itself as the authoritative "who reported this" / "who authored this comment" field. This is a deliberate consolidation, not an oversight: the two concepts — "the audit-trail actor who inserted the row" and "the business-meaningful person who filed the request / wrote the comment" — are the *same fact* for both of these tables (unlike, say, `rent_payments`, where the row is billing-job-generated and the actual money-received event is a separate business fact from row-insertion). Documented explicitly here so it isn't mistaken for a missed requirement.

**`tenant_id` is optional; `building_id` is mandatory.** Per the Phase 2 relationship catalog (§2.1.F) and the Phase 2 weak-relationship review item #3 (already flagged there and resolved by documentation, not a structural change), a maintenance request is always attributable to at least a building — even a common-area issue (lobby lighting, shared generator, elevator) has no apartment and no tenant, but must always have a building. `apartment_id` is nullable (populated for unit-specific issues, NULL for common-area/building-wide issues) and `tenant_id` is nullable (populated when a tenant reports the issue via the portal or is otherwise the affected party; NULL for staff-initiated or common-area requests). This module also carries the module-wide `company_id` denormalization pattern already established since Module 4 §4.0 — every table here has it directly, avoiding a multi-hop join through `apartments → floors → buildings` purely to enforce RLS or scope a dashboard query.

**A single free-form `internal_notes` field on `maintenance_requests` is a distinct concept from the `maintenance_request_comments` table, not a redundant one.** `internal_notes` is a single mutable scratchpad column on the request row itself — a quick, low-ceremony staff note with no independent identity, no author-per-entry attribution, and no permanent append-only history (it is simply overwritten as staff update it, exactly like `notes` columns elsewhere in this schema, e.g. `lease_contracts.notes`, `expenses.notes`). `maintenance_request_comments` is a genuine threaded discussion log — multiple entries over time, each independently authored, timestamped, and (per the brief) individually correctable via the standard soft-delete convention rather than silently overwritten. The two serve different real workflows (a one-line internal flag vs. an ongoing back-and-forth about a specific repair) and are not collapsed into one structure.

**Current status lives on `maintenance_requests.status`; it is application-written, not a trigger-derived cache.** This is a deliberate contrast with `rent_payments.due_date_status` (Module 6 §6.1), which *is* a trigger-maintained cache derived from child-table aggregates. There is no equivalent aggregate to derive maintenance status from — the current status of a maintenance request is not a computed function of its comments or attachments, it is a first-class fact the application sets directly as staff work the ticket. `maintenance_status_history` is written in the **same transaction** as every status-changing `UPDATE` to `maintenance_requests.status` (mirroring `contract_status_history`'s exact relationship to `lease_contracts.status`, Module 5 §5.4) — the history table is the permanent audit trail of transitions, while `maintenance_requests.status` remains the fast, single-row "what is this ticket's state right now" read path every list/dashboard/detail screen actually needs.

**Why `maintenance_requests` is evaluated against, and excluded from, partitioning — despite a 5,000,000-row target larger than several already-partitioned tables in this schema.** Full treatment in §8.6, but flagged here up front because it's a genuinely different conclusion from this schema's two closest precedents (`rent_payments`, Module 6 §6.4; `expenses`, Module 7 §7.6), both of which *are* partitioned at comparable or smaller row counts. The distinction: this module's single highest-frequency query — "open/active requests" (the stated "Open Requests" requirement, and the dashboard's default landing view) — filters on `status`, not on a date range, and an open request may have been filed at any point in the table's history, not just recently. A `request_date`-based RANGE partition would therefore not prune anything for that query (an old, still-open ticket lives in an old partition), unlike `rent_payments`/`expenses`, whose dashboards are genuinely bounded to "this month"/"this quarter" in addition to a status filter. This is the same "query-pattern-driven, not row-count-driven" test this schema has applied consistently (`apartments`, Module 4 §4.4; `lease_contracts`, Module 5 §5.8) — it simply produces a different, and for the first time counter-to-precedent-row-count, answer here.

---

## 8.1 Table: `maintenance_requests`

**Purpose:** The central record of a reported issue or service need against a building, an apartment within it, or a company-wide/common-area concern — the anchor entity for this module and the parent of every attachment, comment, and status transition below.

**Business Description:** One row per reported issue, from initial report through resolution and closure. Captures where the issue is (building, optionally apartment), who is affected (optionally a tenant), what kind of issue it is, how urgent it is, and its current position in the resolution workflow — without embedding any technician/vendor/work-order concept, per this module's explicit scope boundary.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company (tenant-scope root) |
| building_id | UUID | NO | — | Every request is at minimum building-scoped — the stated requirement's baseline; common-area/building-wide issues carry no `apartment_id` but always carry a `building_id` |
| apartment_id | UUID | YES | NULL | The specific unit the issue concerns; NULL for building-common-area/shared-infrastructure issues (lobby, roof, shared generator, elevator machine room) |
| tenant_id | UUID | YES | NULL | The affected/reporting tenant; NULL for staff-initiated requests and for common-area issues with no single affected tenant |
| title | VARCHAR(255) | NO | — | Short summary, e.g. "Kitchen sink leaking", displayed on every list/worklist screen |
| description | TEXT | NO | — | Full description of the issue — required; a categorized-but-undescribed ticket is not actionable for whoever picks it up |
| category | maintenance_category_enum | NO | 'other' | `electrical` \| `plumbing` \| `air_conditioning` \| `elevator` \| `cleaning` \| `water` \| `structural` \| `doors_windows` \| `internet` \| `other` — the stated closed category list |
| priority | maintenance_priority_enum | NO | 'medium' | `low` \| `medium` \| `high` \| `emergency` — the stated priority list |
| status | maintenance_status_enum | NO | 'open' | `open` \| `in_progress` \| `waiting` \| `resolved` \| `closed` \| `cancelled` — current-state snapshot, application-written; see §8.0 for its relationship to `maintenance_status_history` |
| request_date | DATE | NO | CURRENT_DATE | The date the issue was reported/logged — a historical fact, not a scheduling field (see Check Constraints) |
| closed_date | DATE | YES | NULL | The date the request reached a terminal state (`closed` or `cancelled`); NULL until then — see Check Constraints for the full consistency rule |
| internal_notes | TEXT | YES | NULL | Free-form, single-field staff scratchpad — distinct from `maintenance_request_comments`, see §8.0 |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | Doubles as the stated "Created By" business field — see §8.0; nullable only for the rare system/migration-imported row with no attributable human reporter |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `building_id` → `buildings(id)` ON DELETE RESTRICT — per Phase 2, every request is at minimum building-scoped and preserved permanently as operational/dispute-resolution history.
- `apartment_id` → `apartments(id)` ON DELETE SET NULL — per Phase 2, unlinking preserves the request as a building-scoped record rather than orphaning or blocking it; a request's historical value does not depend on the specific unit FK surviving indefinitely the way a financial ledger row depends on its contract.
- `tenant_id` → `tenants(id)` ON DELETE SET NULL — per Phase 2, staff can raise/retain requests without a tenant initiator; the request must survive even if the tenant record itself is later reassigned or corrected.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** None. A maintenance request has no natural business key — two genuinely distinct issues (even identical in category/title, e.g. two separate "AC not cooling" reports from the same apartment months apart) are not duplicates, they are two real, distinct events, the same reasoning already applied to `expenses` (Module 7 §7.1) having no uniqueness constraint.

**Check Constraints:**
- `chk_maintenance_requests_request_date_not_future` — `request_date <= CURRENT_DATE` — mirrors `chk_expenses_expense_date_not_future` (Module 7) and `chk_meter_readings_date_not_future` (Module 4): a request documents something already reported, not a future-scheduling field.
- `chk_maintenance_requests_closed_date_status_consistency` — `(status NOT IN ('closed', 'cancelled') AND closed_date IS NULL) OR (status IN ('closed', 'cancelled') AND closed_date IS NOT NULL)` — the direct implementation of the stated "Invalid closed dates" integrity requirement's core case: a request cannot be in a terminal state without a recorded closure date, and cannot carry a closure date while still open/in-progress/waiting/resolved. `resolved` is deliberately **not** included in the terminal-status branch — per this module's workflow model, `resolved` means the underlying issue has been fixed but the ticket has not yet been formally closed out (e.g., pending tenant confirmation or a final staff sign-off), so it legitimately carries no `closed_date` yet, mirroring the real-world gap between "fixed" and "filed away" that a stricter two-state model would incorrectly collapse.
- `chk_maintenance_requests_closed_date_not_before_request_date` — `closed_date IS NULL OR closed_date >= request_date` — a request cannot be closed before it was ever reported.
- `chk_maintenance_requests_closed_date_not_future` — `closed_date IS NULL OR closed_date <= CURRENT_DATE` — same "historical fact, not a scheduling field" reasoning as `request_date`.
- `chk_maintenance_requests_title_not_blank` — `length(btrim(title)) > 0` — a title consisting only of whitespace is not a meaningful summary; cheap, structural rejection of a real data-entry failure mode rather than leaving it to application-layer trimming alone.

**Indexes:**
- `idx_maintenance_requests_company_status_request_date` composite on `(company_id, status, request_date DESC)` WHERE `deleted_at IS NULL` — the primary **Dashboard Statistics** and general status-filtered worklist query shape: "all requests for company X in status Y, most recent first." `company_id` leads as the mandatory tenant-scoping equality filter present in every query against this table; `status` second as the near-universal secondary equality filter (worklists and dashboard tiles ask for one specific status bucket at a time); `request_date DESC` trailing so "most recent first" is a direct ordered index scan.
  - *Queries served:* `SELECT * FROM maintenance_requests WHERE company_id = $1 AND status = $2 AND deleted_at IS NULL ORDER BY request_date DESC LIMIT 50`; dashboard tiles counting requests per status.
  - *Expected improvement:* turns a filter-then-sort over a table headed toward 5,000,000 rows into a single ordered index range-scan.
  - *Why preferred over separate single-column indexes:* identical reasoning already established for `idx_rent_payments_company_status_due_date` (Module 6) — one composite serves both the equality filters and the sort order.
- `idx_maintenance_requests_company_open_priority` composite on `(company_id, priority, request_date DESC)` WHERE `status IN ('open', 'in_progress', 'waiting') AND deleted_at IS NULL` — a deliberately narrower, partial-on-status companion index purpose-built for the single highest-frequency screen named in the brief: **Open Requests**. Priority-second (not `request_date`) because the operational worklist this serves is a triage queue — staff need "what's still active, worst-priority-first," not simply "what's still active, oldest-first" — mirroring the `idx_apartments_company_occupancy_bedrooms` (Module 4) / `idx_rent_payments_late_overdue` (Module 6) pattern of a narrow, single-purpose partial index reserved for the one query that matters most operationally.
  - *Queries served:* the default landing-page worklist ("show me everything still open, emergency/high priority first"); the triage dashboard.
  - *Expected improvement:* because this index only ever contains currently-active rows (a small, bounded fraction of total historical request volume at any given time — most requests eventually reach `resolved`/`closed`/`cancelled` and drop out), it stays small and fast indefinitely regardless of how large the table's full historical row count grows, exactly the same durability argument already made for `idx_rent_payments_late_overdue`.
- `idx_maintenance_requests_building_request_date` composite on `(building_id, request_date DESC)` WHERE `deleted_at IS NULL` — the stated **Building Requests** requirement: full request history for one building, most-recent-first, the building detail page's maintenance tab.
- `idx_maintenance_requests_apartment_request_date` composite on `(apartment_id, request_date DESC)` WHERE `apartment_id IS NOT NULL AND deleted_at IS NULL` — the stated **Apartment Requests** requirement: a unit's maintenance history across its life, spanning multiple tenancies — a real "has this unit historically had recurring issues" owner-facing question, mirroring `idx_rent_payments_apartment_due_date`'s identical rationale (Module 6). Partial on `apartment_id IS NOT NULL` since common-area requests (a real fraction of the table) have no apartment to report against.
- `idx_maintenance_requests_tenant_request_date` composite on `(tenant_id, request_date DESC)` WHERE `tenant_id IS NOT NULL AND deleted_at IS NULL` — the stated **Tenant Requests** requirement: a tenant's full maintenance history, both for the tenant portal's own "my requests" view and for staff reviewing a tenant's history. Partial for the same reason as the apartment index — a meaningful fraction of rows have no tenant.
- `idx_maintenance_requests_company_category` composite on `(company_id, category)` WHERE `deleted_at IS NULL` — supports the stated **Filtering** requirement's category dimension (e.g., "show me all plumbing issues across the portfolio") and category-bucketed reporting, mirroring `idx_expenses_company_category_date`'s rationale (Module 7) though without a trailing date column here, since category-filtered maintenance views are typically consumed as a full worklist (further refined by status via the index above) rather than a strictly date-ordered report the way financial category spend is.
- `idx_maintenance_requests_title_trgm` — GIN trigram index on `title` — supports the stated **Search** requirement for staff locating a request by partial/misremembered title text, the same rationale class as `idx_buildings_name_trgm` (Module 4), `idx_lease_contracts_contract_number_trgm` (Module 5), and `idx_expenses_vendor_trgm` (Module 7).

**Deliberately omitted indexes:**
- No standalone `priority` index outside the `company_open_priority` partial composite above — priority-only filtering across *all* historical statuses (not just active ones) is a low-frequency analytical question, not a worklist hot path, adequately served by the broader `company_status_request_date` composite's result set for any specific status.
- No standalone `created_by` index — "which requests did staff member X file" is an infrequent administrative-review query, not a hot path, the same judgment already applied to `idx_login_history`'s omission of a `changed_by`-only index in `contract_status_history` (Module 5 §5.4).
- No index on `description` beyond the `title` trigram — a full-text/fuzzy search requirement was stated generically ("Search"), and `title` is this table's short, purpose-built summary field exactly analogous to `buildings.name`/`expenses.vendor_name`; indexing the long-form `description` column as well would be a second GIN index on a table already carrying several composites, for a marginal search-quality gain over title-search, violating the "avoid unnecessary indexes" discipline applied without exception since Module 3.

**Business Rules:**
- `status` is set directly by application code as staff (or an automated SLA-timeout process, if one is ever added — out of this module's scope) move a ticket through its lifecycle; every `UPDATE` to `status` must, in the same transaction, insert the corresponding `maintenance_status_history` row (§8.4) — application-orchestrated, not DB-trigger-driven, for the identical reason `contract_status_history`'s population is application-orchestrated (Module 5 §5.4): the optional `reason` field on the history row requires business context only the application layer has at the moment of transition.
- The **valid transition graph** between statuses (e.g., disallowing a jump directly from `open` to `closed` without an intermediate state, or preventing a `cancelled` ticket from being silently reopened) is an application-layer workflow guard, not a DB constraint — expressing a full state-transition graph in SQL would require a `BEFORE UPDATE` trigger inspecting `OLD.status → NEW.status` against an allowed-transitions table, which PostgreSQL CHECK constraints cannot do (no access to the row's previous value). This is flagged here as the recommended Phase 8 hardening, the identical "application-layer today, DB-trigger hardening recommended for Phase 8" posture already used for `lease_contracts` immutability (Module 5 §5.1) and `cheque_details` status transitions (Module 6 §6.2) — what *is* enforced today at the DB level is the narrower, stateless, single-row-checkable invariant (`closed_date` presence/absence matching terminal-vs-non-terminal status), which is exactly the class of rule a CHECK constraint *can* express.
- `apartment_id`/`tenant_id`, once set, may be corrected via an explicit administrative update (a misattributed unit on a common-area ticket, for instance) — this mirrors `expenses.building_id`'s identical "realistic data-entry correction scenario, no downstream ledger-integrity implication" reasoning (Module 7 §7.1), in contrast to the "denormalized-at-write, immutable thereafter" treatment given to genuinely immutable financial-ledger FKs elsewhere in this schema (e.g. `rent_payments.lease_contract_id`).

**Soft Delete Strategy:** Standard — reserved for genuine data-entry errors (a duplicate ticket accidentally filed twice for the same report); a real, resolved-or-cancelled request is never soft-deleted, since it remains operationally and legally relevant history (recurring-issue analysis, dispute resolution, "was this pre-existing damage" questions at move-out per Phase 1 §1.22's identical rationale for maintenance photo evidence).

**Audit Requirements:** Every mutation logged to `audit_logs`; `status`, `apartment_id`/`tenant_id` reassignment, and `deleted_at` soft-deletes are treated as elevated-severity events given their operational/dispute-resolution weight. As with `rent_payments`/`amount_paid` (Module 6 §6.1), the responsibility for explaining *why* status changed is carried by `maintenance_status_history`'s own `reason` field, not duplicated as free-form `audit_logs.metadata` commentary for the same event.

**Future Scalability Notes:** Explicitly named in the scale target (5,000,000+ rows). Evaluated against, and excluded from, partitioning — full reasoning in §8.0 and §8.6.

---

## 8.2 Table: `maintenance_request_attachments`

**Purpose:** Supports multiple file attachments (photos, videos, documents) per maintenance request, integrating with the central `file_storage` governance table — the before/after photographic evidence Phase 1 §1.22 identifies as a real dispute-prevention necessity.

**Business Description:** A maintenance request's supporting evidence is rarely a single file — a tenant's initial photo of the leak, staff's photo of the completed repair, and occasionally a vendor-provided document. This table is the ordered attachment set for one request.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| maintenance_request_id | UUID | NO | — | The request this attachment belongs to — every attachment belongs to exactly one request (stated requirement) |
| file_id | UUID | NO | — | FK to `file_storage` — the actual uploaded file metadata/blob pointer |
| description | VARCHAR(255) | YES | NULL | Free-form label, e.g. "Before photo — kitchen sink", "Vendor invoice scan" |
| uploaded_by | UUID | YES | NULL | FK to `users`; nullable for system-migrated/bulk-imported historical attachments and for tenant-portal uploads where the uploader is recorded via `created_by` instead — see Business Rules |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `maintenance_request_id` → `maintenance_requests(id)` ON DELETE RESTRICT — per the stated "orphan attachments" integrity requirement; an attachment can never exist without a valid owning request.
- `file_id` → `file_storage(id)` ON DELETE RESTRICT — the file metadata record must outlive this join row, identical reasoning to `contract_documents.file_id` (Module 5 §5.5) and `expense_receipts.file_id` (Module 7 §7.2).
- `uploaded_by`, `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_maintenance_request_attachments_request_file` on `(maintenance_request_id, file_id)` WHERE `deleted_at IS NULL` — the direct implementation of the stated "Duplicate attachment references" integrity requirement: the same physical file should not be attached twice to the same request under two different rows, mirroring `uq_contract_documents_contract_file` (Module 5 §5.5) and `uq_expense_receipts_expense_file` (Module 7 §7.2) exactly. Does **not** prevent the same file being referenced by two *different* requests' attachment rows — not a realistic scenario, and not structurally harmful either.

**Check Constraints:** None beyond NOT NULL/FK typing — there is no scope-dependent nullable column here to validate against (no analogue to `utility_meters`' scope/FK consistency check), the identical conclusion already reached for `contract_documents` (Module 5 §5.5).

**Indexes:**
- `idx_maintenance_request_attachments_request_id` on `(maintenance_request_id)` WHERE `deleted_at IS NULL` — "list all attachments for this request," the request detail page's attachments tab — the only realistic access pattern for this table, mirroring `idx_contract_documents_lease_contract_id`'s identical rationale (Module 5 §5.5).

**Deliberately omitted index:** no standalone `file_id` index — "which request does this file belong to" has no realistic reverse-lookup product feature, identical reasoning already applied to `contract_documents` (Module 5 §5.5) and `expense_receipts` (Module 7 §7.2); a data-integrity audit, if ever needed, tolerates a sequential scan against this table's modest per-request row count.

**Business Rules:** `uploaded_by` and `created_by` are, in the overwhelming majority of cases, the same actor and could be consolidated per the §8.0 pattern used elsewhere in this module — they are deliberately kept **as two distinct columns here**, unlike `maintenance_requests.created_by`/`maintenance_request_comments.created_by`, because attachment upload is the one place in this module where the *acting* user and the *audit-row-creating* user can genuinely diverge: a tenant may upload a photo through the tenant portal (captured in `uploaded_by`, since the portal action is tenant-attributable to a `users` row via the tenant's optional portal link) while the row itself may be inserted by a background ingestion/sync process on the tenant's behalf, or vice versa for a staff-side bulk-import tool. This is a narrow, deliberate exception to the §8.0 consolidation principle, called out explicitly so it doesn't read as an inconsistency.

**Soft Delete Strategy:** Standard — a superseded/incorrect attachment (wrong file uploaded) is soft-deleted and replaced with a new row rather than the file being overwritten in place, preserving the historical evidentiary trail, identical philosophy to `contract_documents` (Module 5 §5.5).

**Audit Requirements:** Standard `audit_logs` coverage; upload and soft-delete events both logged, since removing evidence from a maintenance request's record is itself a traceable action with real dispute-resolution weight.

**Future Scalability Notes:** Explicitly named in the scale target (20,000,000+ rows). Evaluated against, and excluded from, partitioning in §8.6 — its dominant query pattern is the FK-equality lookup above, not a time-range scan, the identical shape and conclusion already reached for `payment_allocations` (Module 6 §6.4) and `expense_receipts`/`rent_payment_receipts` (Module 7 §7.6).

---

## 8.3 Table: `maintenance_request_comments`

**Purpose:** Supports internal, threaded communication regarding a maintenance request — the ongoing back-and-forth between staff (and, where relevant, tenant-portal-originated updates) as a ticket is worked.

**Business Description:** Every substantive update or discussion point on a request is recorded here as an individually authored, timestamped entry, distinct from the single-field `internal_notes` scratchpad on `maintenance_requests` itself (§8.0).

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| maintenance_request_id | UUID | NO | — | The request this comment belongs to — every comment belongs to exactly one request (stated requirement) |
| comment_text | TEXT | NO | — | The comment body — required; an empty comment carries no communicative value |
| created_at | TIMESTAMPTZ | NO | now() | Doubles as the stated "Created At" business field |
| updated_at | TIMESTAMPTZ | NO | now() | Supports the standard correction-of-a-typo edit case |
| created_by | UUID | YES | NULL | Doubles as the stated "Author" business field — see §8.0; nullable per the Global Convention's actor-attribution pattern, though in practice a comment is essentially always authored by a real, identifiable person at creation time |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete — see Business Rules |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `maintenance_request_id` → `maintenance_requests(id)` ON DELETE RESTRICT — per the stated "orphan comments" integrity requirement; a comment can never exist without a valid owning request.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** None — every comment is a distinct communicative event by nature; no meaningful uniqueness beyond the PK, the same "no dedup" philosophy already applied to `audit_logs` (Module 3 §3.12) and `contract_status_history` (Module 5 §5.4).

**Check Constraints:** `chk_maintenance_request_comments_text_not_blank` — `length(btrim(comment_text)) > 0` — a whitespace-only comment carries no communicative value and is almost certainly a client-side submission bug rather than a legitimate entry; mirrors `chk_maintenance_requests_title_not_blank` above.

**Indexes:**
- `idx_maintenance_request_comments_request_created_at` composite on `(maintenance_request_id, created_at DESC)` WHERE `deleted_at IS NULL` — **the** query this table exists to serve: "full comment thread for request X, most-recent-first (or, at the application layer, reversed for chronological display)" — the request detail page's discussion tab. `maintenance_request_id` leads as the mandatory equality filter; `created_at DESC` trails so the common "recent activity" access is a direct ordered index scan with no separate sort step, mirroring `idx_contract_status_history_contract_changed_at`'s identical pattern (Module 5 §5.4).

**Deliberately omitted indexes:** no standalone `company_id` index and no `created_by` index — comments are never queried at the company-wide or author-wide level as a stated primary access pattern (no "all comments across my portfolio" or "everything staff member X has written" screen exists in the brief); the single composite above already fully serves this table's one realistic access pattern, and RLS policy evaluation on this per-request-bounded, low-row-count table doesn't need a dedicated index the same way `maintenance_requests` itself does — the identical reasoning already applied to `lease_guarantors`' omission of a standalone `company_id` index (Module 5 §5.2).

**Business Rules:** A comment, once created, may be corrected (typo fix) via a standard `UPDATE` to `comment_text`, tracked via `updated_at`/`updated_by` — unlike the strictly append-only `maintenance_status_history` (§8.4), comments follow the ordinary Global Convention mutable-row pattern, since a comment is an editable communication artifact, not an immutable historical fact the way a status transition or a financial ledger entry is.

**Soft Delete Strategy:** Standard — a comment posted in error (wrong request, sensitive information posted by mistake) is soft-deleted rather than hard-deleted, preserving the discussion thread's integrity for any later audit or dispute-resolution need while removing it from normal display.

**Audit Requirements:** Standard `audit_logs` coverage; edits and soft-deletes of comments are logged, since altering or removing a record of internal communication about an active issue is itself a traceable action.

**Future Scalability Notes:** Explicitly named in the scale target (20,000,000+ rows). Evaluated against, and excluded from, partitioning in §8.6 — dominant query pattern is the single FK-equality-lookup-and-sort composite above, bounded per-request, the identical shape already established for `maintenance_request_attachments`.

---

## 8.4 Table: `maintenance_status_history`

**Purpose:** Append-only log of every status transition a maintenance request undergoes, purpose-built for the request's lifecycle-timeline product feature — the direct structural analogue of `contract_status_history` (Module 5 §5.4) applied to this module's own state machine.

**Business Description:** Every time `maintenance_requests.status` changes — `open` → `in_progress` → `waiting`/`resolved` → `closed`, or any point diverted to `cancelled` — a row is written here capturing the transition, its timestamp, its actor, and an optional business reason.

**Columns:**

| Column Name | PostgreSQL Data Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized tenant scope |
| maintenance_request_id | UUID | NO | — | The request whose status changed |
| previous_status | maintenance_status_enum | YES | NULL | NULL only for the very first row of a request's life (the initial `open` creation has no "previous" state) |
| new_status | maintenance_status_enum | NO | — | — |
| changed_by | UUID | YES | NULL | FK to `users`; nullable for any future system/automated-process-driven transition (e.g., an SLA-timeout auto-escalation, should one ever be added outside this module's current scope) |
| changed_at | TIMESTAMPTZ | NO | now() | — |
| reason | TEXT | YES | NULL | Optional business justification for the transition, e.g. "vendor confirmed repair complete," "tenant unresponsive, closing after 3 attempts," "duplicate of another open ticket" |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `maintenance_request_id` → `maintenance_requests(id)` ON DELETE RESTRICT — per the stated "orphan status history" integrity requirement; preserved permanently as the request's core lifecycle-traceability evidence.
- `changed_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** None — every transition is a distinct event by nature, mirroring `contract_status_history`'s identical "no dedup" philosophy (Module 5 §5.4).

**Check Constraints:** `chk_maintenance_status_history_no_noop` — `previous_status IS NULL OR previous_status != new_status` — rejects a logged "transition" that isn't actually a transition, the identical rule and rationale already established for `chk_contract_status_history_no_noop` (Module 5 §5.4): a same-value "transition" would only ever indicate an application bug, never a legitimate event worth recording.

**Indexes:**
- `idx_maintenance_status_history_request_changed_at` composite on `(maintenance_request_id, changed_at DESC)` — **the** query this table exists to serve: "full status timeline for request X, most recent first" (the request detail page's history tab), mirroring `idx_contract_status_history_contract_changed_at`'s identical pattern (Module 5 §5.4).
- `idx_maintenance_status_history_company_new_status` composite on `(company_id, new_status, changed_at DESC)` — supports a company-wide operational query: "show every request that transitioned to `closed` (or any specific status) this month" — used for dashboard statistics (stated requirement) and for reviewing how quickly tickets are moving through the pipeline, mirroring `idx_contract_status_history_company_new_status`'s identical pattern (Module 5 §5.4).

**Deliberately omitted index:** no index on `changed_by` alone — "what status changes has this staff member made" is a low-frequency administrative-review query, not a hot path, the identical judgment already applied to the same column in `contract_status_history` (Module 5 §5.4).

**Business Rules:** Written in the same transaction as the `maintenance_requests.status` UPDATE it records — application-orchestrated, not a DB trigger, for the identical reason already given for `contract_status_history` (Module 5 §5.4): the `reason` field requires business context only the application layer has at the moment of transition, so the whole row is populated at the application layer for consistency of approach rather than splitting population between a trigger and application code.

**Soft Delete Strategy:** Not applicable — pure append-only event log, identical philosophy to `contract_status_history`/`login_history`/`meter_readings`: a status transition, once it happened, is a permanent historical fact, never corrected in place. A data-entry mistake is handled by inserting a corrective row with an explanatory `reason`, never by editing or deleting the erroneous one.

**Audit Requirements:** This table is itself a specialized audit trail for one specific state machine; it is not additionally mirrored into `audit_logs` for every row (pure duplication of an already-immutable, already-timestamped, already-actor-attributed record) — mirroring exactly how `contract_status_history` is treated relative to `audit_logs` (Module 5 §5.4).

**Future Scalability Notes:** Grows with (requests × average transitions-per-request, typically 3–5 over a request's life: `open` → `in_progress` → `waiting`/`resolved` → `closed`, sometimes more with a reopened/escalated path) — at 5,000,000 requests, comfortably in the low tens-of-millions of rows. Evaluated against, and excluded from, partitioning in §8.6, the identical conclusion and reasoning already reached for `contract_status_history` (Module 5 §5.4): this table has no unbounded, ever-repeating-per-entity growth pattern the way `audit_logs`/`login_history`/`meter_readings` do — a single request's status history is inherently short and finite.

---

## 8.5 Database Integrity Summary

| Risk | Prevention Mechanism | Explanation |
|---|---|---|
| Invalid status transitions (structural cases) | `chk_maintenance_requests_closed_date_status_consistency` (DB-level) + application-layer transition-graph validation (Phase 8 trigger hardening recommended) | The stateless, single-row-checkable half of "invalid transitions" (closed-date/status correlation) is enforced today at the DB level; the full transition-graph rule requires inspecting `OLD.status`, which a CHECK constraint cannot do — the same layered posture already applied to `cheque_details` (Module 6 §6.2) |
| Orphan attachments | `maintenance_request_attachments.maintenance_request_id` and `.file_id` both `NOT NULL` with real FKs, `ON DELETE RESTRICT` | No attachment can exist without valid, permanent references to both its owning request and its underlying file, mirroring `contract_documents` (Module 5 §5.5) |
| Orphan comments | `maintenance_request_comments.maintenance_request_id` `NOT NULL` with a real FK, `ON DELETE RESTRICT` | No comment can exist without a valid owning request |
| Orphan status history | `maintenance_status_history.maintenance_request_id` `NOT NULL` with a real FK, `ON DELETE RESTRICT` | A request's lifecycle timeline can never be detached from the request it describes |
| Invalid closed dates | `chk_maintenance_requests_closed_date_status_consistency`, `chk_maintenance_requests_closed_date_not_before_request_date`, `chk_maintenance_requests_closed_date_not_future` | A comprehensive set of CHECK constraints enforcing both status/date-field correlation and date ordering — no lifecycle-inconsistent request row is representable |
| Duplicate attachment references | `uq_maintenance_request_attachments_request_file` on `(maintenance_request_id, file_id)` | The same file cannot be attached twice to the same request under two different rows |
| No-op status history entries | `chk_maintenance_status_history_no_noop` | Guards against an application bug logging a "transition" that changed nothing |
| Blank/meaningless title or comment text | `chk_maintenance_requests_title_not_blank`, `chk_maintenance_request_comments_text_not_blank` | Whitespace-only text carries no informational value and almost always indicates a client-side submission bug |
| Future-dated maintenance records | `chk_maintenance_requests_request_date_not_future`, `chk_maintenance_requests_closed_date_not_future` | A request/closure date documents something that has already happened, consistent with the identical reasoning applied to `expenses.expense_date` (Module 7) and `meter_readings.reading_date` (Module 4) |

---

## 8.6 Scalability at Target Scale

| Scale point | Impact |
|---|---|
| 10,000 companies | Trivial for every table in this module — per-company row counts stay in the low hundreds to low thousands even at full portfolio size, well within any single index's efficient range. |
| 5,000,000 maintenance_requests | **This module's central scalability decision, and a deliberate departure from the two closest precedents in this schema.** Unlike `rent_payments` (Module 6 §6.4) and `expenses` (Module 7 §7.6) — both partitioned because their dominant dashboard/reporting queries are genuinely bounded to a recent or fiscal-period date range in addition to a status/category filter — `maintenance_requests`' single highest-frequency query, "Open Requests," filters on `status` alone and is **not** date-bounded: an open ticket may have been filed at any point in the table's multi-year history and remains equally relevant to that query today. A `request_date`-RANGE partition would not prune anything for this dominant access pattern, since active tickets are not clustered in recent partitions the way settled financial rows are. **Recommendation: do not partition `maintenance_requests`.** The composite indexes keyed on the table's actual filter columns (`company_id`, `status`, `priority`, `building_id`, `apartment_id`, `tenant_id`, `category`) are the correct scaling mechanism — the same conclusion, for a related but distinctly-reasoned cause, already reached for `apartments` (Module 4 §4.4) and `lease_contracts` (Module 5 §5.8). |
| 20,000,000 maintenance_request_comments | Scales as a small multiple of requests (typically a handful of comments per active ticket, fewer or none on simple/quickly-resolved ones). Dominant query pattern is a single FK-equality-lookup-and-sort (`idx_maintenance_request_comments_request_created_at`), bounded per-request to a small result set regardless of the table's total row count — the identical shape already excluded from partitioning for `payment_allocations` (Module 6 §6.4) and the receipt tables (Module 7 §7.6). **Partitioning is not recommended.** |
| 20,000,000 maintenance_request_attachments | Same reasoning and same conclusion as comments above — dominant access is `idx_maintenance_request_attachments_request_id`'s FK-equality lookup, bounded per-request, not a platform-wide time-range scan. **Partitioning is not recommended.** |
| maintenance_status_history | Grows to the low tens of millions (5,000,000 requests × 3–5 transitions each) but, like `contract_status_history` (Module 5 §5.4), has no unbounded per-entity growth pattern — a single request's status timeline is inherently short and finite, unlike `audit_logs`/`login_history`/`meter_readings`'s genuinely ever-repeating-per-entity time series. **Partitioning is not recommended.** |

**Should any table in this module be partitioned?** No. `maintenance_requests` is the only table here with a row count in the same order of magnitude as tables partitioned elsewhere in this schema, and it is the one explicitly evaluated *against* partitioning on query-pattern grounds — its dominant access pattern is a status filter spanning the table's entire history, not a bounded recent-date-range scan, so a date-based RANGE partition would not serve its real workload. The three child tables all scale in lockstep with `maintenance_requests` at small per-parent multiples with FK-equality-lookup-dominant access patterns, the same "row count alone doesn't justify it" test already applied consistently to `payment_allocations`, `cheque_details`, and both Module 7 receipt tables.

---

## 8.7 Performance Optimization Explanation

- **Open Requests:** `idx_maintenance_requests_company_open_priority` — the narrowest, most surgical partial index in this module, scoped to exactly the active-status subset and ordered for priority-first triage, reserved for the single highest-value, highest-frequency screen in this module.
- **Building Requests:** `idx_maintenance_requests_building_request_date` — equality-leading composite with `DESC` trailing sort, the standard pattern applied throughout this schema since Module 3.
- **Apartment Requests:** `idx_maintenance_requests_apartment_request_date` — identical pattern, rooted at the apartment rather than the building, partial on `apartment_id IS NOT NULL` since common-area requests have none.
- **Tenant Requests:** `idx_maintenance_requests_tenant_request_date` — identical pattern, rooted at the tenant, partial on `tenant_id IS NOT NULL`.
- **Dashboard Statistics:** `idx_maintenance_requests_company_status_request_date` (overall status-bucketed counts/lists) and `idx_maintenance_status_history_company_new_status` (transition-volume tiles, e.g. "closed this month") together cover this module's dashboard-tile shapes.
- **Search:** `idx_maintenance_requests_title_trgm` — fuzzy substring search on the short summary field, the same trigram pattern already applied to `buildings.name`, `lease_contracts.contract_number`, and `expenses.vendor_name`.
- **Filtering:** every filtering dimension named in the brief (status, priority, building, apartment, tenant, category) is covered by one of the composite indexes above — no filtering requirement in this module lacks a matching leading-column-correct index.
- **Pagination:** every composite index with a trailing `DESC` date column directly supports keyset pagination (`WHERE (building_id, request_date) < ($1, $2) ORDER BY request_date DESC LIMIT 50`), the same preferred-over-`OFFSET` strategy established since Module 3 §3.10 — essential here given `maintenance_request_comments`/`maintenance_request_attachments` are among the fastest-growing tables in the schema by row count after `meter_readings`.
- **Comment/Attachment Drill-Down:** `idx_maintenance_request_comments_request_created_at` and `idx_maintenance_request_attachments_request_id` are each the single, sufficient index for their table's one realistic access pattern — no additional index was added to either table beyond what its actual (narrow, FK-rooted) query shape requires, consistent with the "avoid unnecessary indexes" instruction repeated throughout this module's brief.

---

## 8.8 Security & Multi-Tenant Isolation

- **RBAC:** Maintenance request creation, status transitions, comment authoring, and attachment upload are gated by existing `maintenance.create`/`maintenance.update_status`/`maintenance.comment`-style permission keys (Module 3, `permissions`) resolved via `role_permissions` — no new RBAC machinery introduced in this module; it consumes the existing platform-wide permission catalog exactly as every prior module has.
- **Audit Logs:** Every table in this module writes to `audit_logs` on mutation per the Global Convention; `status` transitions, `apartment_id`/`tenant_id` reassignment on `maintenance_requests`, and any soft-delete across all four tables are flagged elevated-severity given their operational/dispute-resolution weight.
- **Multi-Tenant Isolation:** Every table in this module carries a denormalized `company_id` and is RLS-ready under the standard `USING (company_id = current_setting('app.current_company_id')::uuid)` policy. No table in this module needs a non-standard RLS shape (unlike Module 3's `refresh_tokens`/`login_history`) — every row in every table here belongs unambiguously to exactly one company, with no cross-tenant legitimate-access case.
- **Soft Delete:** Standard across `maintenance_requests`, `maintenance_request_attachments`, and `maintenance_request_comments`; explicitly **not applicable** to `maintenance_status_history`, which is a pure append-only event log, the identical treatment already given to `contract_status_history` (Module 5 §5.4).
- **Evidentiary Integrity:** `maintenance_request_attachments` — carrying before/after photographic evidence — is treated with the same dispute-prevention seriousness Phase 1 §1.22 originally flagged for this concept: attachments are never overwritten in place, only soft-deleted-and-replaced, preserving a complete evidentiary trail for any later move-out or liability dispute.

---

## Module 8 (Maintenance) — Architecture Review

- **PostgreSQL Best Practices:** UUIDv7 PKs throughout; `TIMESTAMPTZ` for all point-in-time columns (`changed_at`, audit columns) versus correctly-typed `DATE` for calendar-date concepts (`request_date`, `closed_date`) — the identical DATE-vs-TIMESTAMPTZ discipline applied correctly and consistently since Module 5. No monetary columns exist in this module, so the `NUMERIC(12,3)`/`currency` convention is not applicable here — correctly absent rather than reflexively included.
- **Referential Integrity:** Every FK in this module is `NOT NULL` and `RESTRICT`-on-delete for the structural parent-child relationships (`maintenance_request_id`, `file_id`), except `apartment_id`/`tenant_id` on `maintenance_requests` (correctly `SET NULL`, per Phase 2's relationship catalog, since a request's continued existence does not depend on the specific unit/tenant FK surviving) and the standard `SET NULL` actor-attribution columns (`created_by`/`updated_by`/`deleted_by`/`uploaded_by`/`changed_by`) — consistent with the platform-wide "preserve history, sever only the actor-attribution or non-structural edge" pattern established in every prior module.
- **Constraint Quality:** The `closed_date`/`status` correlation constraint was deliberately reviewed against the real-world "resolved but not yet formally closed" workflow gap and `resolved` was consciously **excluded** from the terminal-status branch — called out explicitly here as a constraint shape that was considered and deliberately narrowed, not merely applied mechanically, continuing the "show the caught near-miss / deliberate scoping decision" transparency discipline established in Module 3 §3.5 and continued in Module 6 §6.2.
- **Index Quality:** Every composite index in this module was checked for column-order correctness (equality-before-range/sort, `DESC` embedded where a query needs most-recent-first, and — for the Open Requests case — a triage-priority column deliberately placed ahead of the date column because the query's real ordering need is severity, not recency) — no index found with the wrong leading column. Two indexes were deliberately **not** created and the omission explicitly justified rather than silently skipped: a standalone `priority` index outside the open-requests partial composite, and a `description`-column trigram index alongside the `title` trigram index (§8.1) — both real candidates a less careful pass might add reflexively, both correctly identified as adding write cost without a corresponding read benefit given this module's actual stated query patterns.
- **Query Performance:** Every stated required query shape (Open Requests, Building Requests, Apartment Requests, Tenant Requests, Dashboard Statistics, Search, Filtering, Pagination) is explicitly mapped to a specific index in §8.7, with no requirement left unaddressed and no index added without a corresponding named requirement — the same discipline as Modules 4, 6, and 7's requirement-to-index mapping.
- **Security:** Reviewed against every requirement in the module brief (§8.8) — RBAC, audit logging, multi-tenant isolation, and soft delete all confirmed present and standard-shaped across the module, with the one deliberate, explicitly-justified exception (`maintenance_status_history`'s append-only, no-soft-delete design) documented in place rather than silently inconsistent.
- **Multi-Tenant Isolation:** Confirmed — every table denormalizes `company_id` directly (§8.0, §8.8); no table in this module needs a non-standard RLS shape.
- **Future Scalability:** `maintenance_requests` is the first large table in this schema evaluated against partitioning and excluded for a reason distinct from (though related to) the `apartments`/`lease_contracts` precedent — not merely "no time-range dimension exists," but specifically "the dominant query's date-boundedness assumption, which held for `rent_payments`/`expenses`, does not hold here" — a genuinely new, non-mechanical application of this schema's established partitioning test, reasoned explicitly in §8.0 and §8.6 rather than defaulting to either "large row count therefore partition" or "prior module partitioned a similarly-sized table therefore this one should too." `maintenance_request_comments`, `maintenance_request_attachments`, and `maintenance_status_history` were each evaluated and excluded on the same FK-equality-lookup-dominant / bounded-per-entity-growth grounds already established for their closest structural analogues elsewhere in the schema.
- **Maintainability:** Every non-obvious modeling decision in this module — the `created_by`-doubles-as-"Created By"/"Author" consolidation, the `internal_notes`-vs-`comments` distinction, the deliberate exclusion of `resolved` from the terminal-`closed_date` branch, and the partitioning-exclusion reasoning that departs from this schema's two closest precedents — is documented in place with its specific business and technical reasoning, continuing the "no undocumented deviation" discipline maintained without exception since Module 1.

---

*End of Module 8. Say "CONTINUE" for MODULE 9 — MARKETPLACE (marketplace_listings, listing_images, viewing_requests).*

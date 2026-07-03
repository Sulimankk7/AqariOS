# PropertyOS — PHASE 3: Physical Database Design
## Production-Ready PostgreSQL Table Specifications

This document is the physical companion to `PropertyOS_Database_Design.md` (Phases 1–2). Every table below implements the conceptual entities and FINAL architecture decisions established there. Generated module-by-module per the required order: Core → SaaS → Security → Properties → Leasing → Tenants → Financial → Maintenance → Marketplace → Documents → Notifications.

## Global Conventions (apply to every table unless explicitly overridden)

- **Primary keys:** `id UUID NOT NULL DEFAULT uuid_generate_v7()` — UUIDv7 chosen over UUIDv4 specifically because it is time-ordered, which keeps B-tree PK indexes append-mostly (avoiding the random-insert page-split penalty UUIDv4 causes at high write volume) while still giving the collision-resistance and non-guessability of a UUID — the correct choice for a multi-tenant system generating IDs client-side or across distributed workers without coordination.
- **Audit columns**, present on every table: `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, `updated_at TIMESTAMPTZ NOT NULL DEFAULT now()` (maintained via a shared `set_updated_at()` trigger, not application code, so it's impossible to forget), `created_by UUID NULL REFERENCES users(id)`, `updated_by UUID NULL REFERENCES users(id)` (nullable because system/job-generated rows have no human actor).
- **Soft delete**, present on every table representing durable business data (explicitly noted per-table where it does NOT apply, e.g., ephemeral security artifacts): `deleted_at TIMESTAMPTZ NULL`, `deleted_by UUID NULL REFERENCES users(id)`. A row is "active" when `deleted_at IS NULL`. All application queries default to this filter; enforced consistently via Prisma middleware, not left to per-query discipline.
- **Multi-tenant isolation:** every tenant-scoped table carries `company_id UUID NOT NULL REFERENCES companies(id)` and has Row-Level Security enabled with a policy of the shape `USING (company_id = current_setting('app.current_company_id')::uuid)`, detailed fully in Phase 8. This is noted per-table as "RLS Policy: standard tenant-isolation" and only elaborated where a table deviates from the standard pattern (e.g., `permissions`, `marketplace_listings` public read).
- **Naming conventions:** tables `snake_case`, plural (`buildings`, not `Building`); columns `snake_case`; FKs named `<referenced_singular>_id`; indexes named `idx_<table>_<columns>`; unique constraints `uq_<table>_<columns>`; check constraints `chk_<table>_<rule>`.
- **Timestamps:** always `TIMESTAMPTZ`, never bare `TIMESTAMP` — a Jordan-only v1 with a stated GCC-expansion roadmap must never store ambiguous local time.
- **Money:** always `NUMERIC(12,3)` for JOD-denominated amounts (3 decimal places — JOD's minor unit is the fils, 1/1000 JOD, not a 2-decimal currency; getting this wrong is a classic Jordan-market implementation bug), paired with an explicit `currency CHAR(3) NOT NULL DEFAULT 'JOD'` column on every monetary table, never a hardcoded assumption, per the GCC-expansion future-proofing already established in Phase 1.

---

# MODULE 1 — CORE

## 1.1 Table: `companies`

**Purpose:** The tenant root entity. Every other tenant-scoped table traces back to a row here, directly or transitively.

**Business Description:** Represents the paying customer — a property owner, investment company, or property management company using PropertyOS. Anchors multi-tenant isolation for the entire platform.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key, tenant root identifier |
| legal_name | VARCHAR(255) | NO | — | Registered legal name (Arabic or English) |
| display_name | VARCHAR(255) | NO | — | Trading/display name shown in UI |
| commercial_registration_no | VARCHAR(50) | YES | NULL | رقم السجل التجاري — nullable to support unregistered individual owners operating without a formal legal entity |
| tax_number | VARCHAR(50) | YES | NULL | الرقم الضريبي, nullable for the same reason |
| company_type | company_type_enum | NO | 'individual_owner' | `individual_owner` \| `property_management_company` \| `investment_company` |
| primary_phone | VARCHAR(20) | NO | — | +962-format validated Jordanian mobile/landline |
| primary_email | VARCHAR(255) | YES | NULL | Nullable — phone-first onboarding is common in this market |
| country_code | CHAR(2) | NO | 'JO' | ISO 3166-1 alpha-2; future-proofs GCC expansion |
| is_active | BOOLEAN | NO | true | Platform-level kill switch independent of subscription status (fraud/ToS enforcement) |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:** `created_by`, `updated_by`, `deleted_by` → `users(id)` (nullable, deferred FK — `users` itself has `company_id`, so this is a legitimate circular-but-nullable reference resolved at the application layer during first-user bootstrap; detailed in Phase 8's onboarding-transaction note).

**Unique Constraints:** `uq_companies_commercial_registration_no` on `(commercial_registration_no)` WHERE `commercial_registration_no IS NOT NULL` (partial unique — multiple NULLs must be allowed for unregistered owners).

**Check Constraints:** `chk_companies_primary_phone_format` — validates `+962` E.164 format via regex.

**Indexes:**
- `idx_companies_deleted_at` on `(deleted_at)` WHERE `deleted_at IS NULL` — every "list active companies" query (admin dashboards, platform ops) filters on this; partial index keeps it tiny even at scale since most rows are active.
- `idx_companies_commercial_registration_no` — supports the partial unique constraint above and admin lookup-by-registration-number.

**Business Rules:** A company must have either `commercial_registration_no` populated or be explicitly flagged `individual_owner` — enforced at the application layer (not a CHECK constraint, since the business rule involves a value-presence-conditional-on-enum which is expressible in SQL but adds fragility for a rule likely to evolve; documented here as an app-layer invariant instead).

**Soft Delete Strategy:** Standard — `deleted_at` populated on company offboarding/churn. Financial and legal child records (buildings, leases, payments) are never cascade-hard-deleted; a soft-deleted company's data remains fully intact and exportable for the statutory Jordanian financial-record retention period.

**Audit Requirements:** Every `UPDATE`/soft-`DELETE` on this table is captured in `audit_logs` (Module 3) — this is the highest-sensitivity table in the schema from a tenant-integrity standpoint, since it's the isolation root.

**Future Scalability Notes:** `country_code` and the `company_type` enum are the two columns explicitly placed here to support GCC expansion without a future migration — new country-specific validation and workflow logic branches on these existing columns rather than requiring schema changes.

## 1.2 Table: `company_settings`

**Purpose:** One-to-one configuration extension of `companies`, isolating rarely-hot-path config from the tenant-lookup-critical `companies` row.

**Business Description:** Configurable business parameters governing how the platform behaves for a given company: currency, grace periods, fiscal year, language, timezone.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company (1:1) |
| default_currency | CHAR(3) | NO | 'JOD' | ISO 4217 |
| rent_grace_period_days | SMALLINT | NO | 5 | Days after due date before a payment is flagged late |
| late_fee_type | late_fee_type_enum | NO | 'none' | `none` \| `fixed` \| `percentage` |
| late_fee_value | NUMERIC(12,3) | YES | NULL | Amount or percentage depending on `late_fee_type` |
| fiscal_year_start_month | SMALLINT | NO | 1 | 1–12 |
| default_language | CHAR(2) | NO | 'ar' | `ar` \| `en` |
| timezone | VARCHAR(50) | NO | 'Asia/Amman' | IANA timezone identifier |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:** `company_id` → `companies(id)` ON DELETE CASCADE.

**Unique Constraints:** `uq_company_settings_company_id` on `(company_id)` — enforces the 1:1 cardinality.

**Check Constraints:**
- `chk_company_settings_fiscal_month` — `fiscal_year_start_month BETWEEN 1 AND 12`.
- `chk_company_settings_late_fee_value` — `late_fee_value IS NOT NULL WHEN late_fee_type != 'none'`.

**Indexes:** None beyond the unique constraint's implicit index — this table is looked up exclusively by `company_id`, already covered.

**Business Rules:** Auto-created (via application transaction, not a DB trigger, to keep default-value business logic in one place) at company signup with sensible Jordan-market defaults, so the settings row always exists — no nullable "settings not yet configured" state to handle downstream.

**Soft Delete Strategy:** None — this table has no independent lifecycle; it is hard-deleted only as a CASCADE consequence of a (rare, admin-only) hard-delete of its parent company, never soft-deleted on its own.

**Audit Requirements:** Changes logged to `audit_logs` — `late_fee_type`/`late_fee_value` changes in particular are financially consequential and must be traceable.

**Future Scalability Notes:** None required — this is a narrow, low-cardinality, low-write table by nature; no partitioning or indexing concerns at any realistic scale.

---

## Module 1 (Core) — Architecture Review

- **Redundant columns:** None found. `default_currency` here vs. `currency_at_subscription` on `company_subscriptions` (Module 2) are not redundant — the former is the company's *ongoing operational* currency for new leases/bills, the latter is a historical *snapshot* of what was billed at signup time; they can legitimately diverge if a company's operational currency changes after subscribing (a genuine, if rare, future GCC-expansion scenario), so keeping them independent is correct, not duplicative.
- **Missing indexes:** None identified for this module's actual query patterns (tenant lookup by `id` is PK-covered; active-company listing is covered by the partial index).
- **Missing constraints:** Added `chk_companies_primary_phone_format` and the two `company_settings` CHECK constraints during this pass — not present in the Phase 1/2 conceptual description, added now because physical design is where format/range validation belongs, not the conceptual layer.
- **Naming inconsistencies:** None — both tables conform to the Global Conventions.
- **Scalability concerns:** None at this module's scale — `companies` and `company_settings` are both low-row-count-relative-to-the-platform tables (thousands, not millions, of rows), no partitioning need. Flagging forward: `audit_logs` referencing `companies.id` at high volume is where the *real* scale concern for this module's blast radius lives, addressed in Module 3.

---

# MODULE 2 — SAAS (SUBSCRIPTION MANAGEMENT)

## 2.1 Table: `subscription_plans`

**Purpose:** Read-mostly commercial catalog of plans PropertyOS sells.

**Business Description:** Platform-operator-managed product catalog (Starter/Growth/Enterprise-style tiers), priced natively in JOD with GCC-currency extensibility.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| code | VARCHAR(50) | NO | — | Stable machine-readable identifier, e.g. `starter_v1` — used in application code/feature-gating logic, immutable once referenced by any subscription |
| name_en | VARCHAR(100) | NO | — | Display name (English) |
| name_ar | VARCHAR(100) | NO | — | Display name (Arabic) |
| description_en | TEXT | YES | NULL | — |
| description_ar | TEXT | YES | NULL | — |
| monthly_price | NUMERIC(12,3) | NO | — | — |
| yearly_price | NUMERIC(12,3) | NO | — | — |
| currency | CHAR(3) | NO | 'JOD' | ISO 4217 |
| max_buildings | INTEGER | YES | NULL | NULL = unlimited (explicit sentinel semantics, never a magic number like -1) |
| max_users | INTEGER | YES | NULL | NULL = unlimited |
| max_storage_mb | INTEGER | YES | NULL | NULL = unlimited |
| feature_flags | JSONB | NO | '{}' | Boolean/enum feature toggles, e.g. `{"efawateercom_integration": true, "ai_chatbot": false}` |
| supports_trial | BOOLEAN | NO | false | — |
| trial_duration_days | SMALLINT | YES | NULL | Required when `supports_trial = true` |
| is_active | BOOLEAN | NO | true | Retired plans set false, never hard-deleted |
| sort_order | SMALLINT | NO | 0 | Pricing-page display order |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:** None — this is a root reference/catalog table.

**Unique Constraints:** `uq_subscription_plans_code` on `(code)`.

**Check Constraints:**
- `chk_subscription_plans_trial_duration` — `trial_duration_days IS NOT NULL WHEN supports_trial = true`.
- `chk_subscription_plans_prices_positive` — `monthly_price > 0 AND yearly_price > 0`.
- `chk_subscription_plans_quotas_positive` — `(max_buildings IS NULL OR max_buildings > 0) AND (max_users IS NULL OR max_users > 0) AND (max_storage_mb IS NULL OR max_storage_mb > 0)`.

**Indexes:**
- `idx_subscription_plans_active_sort` on `(is_active, sort_order)` WHERE `is_active = true` — the exact shape of the pricing-page query (active plans, in display order); partial+composite because the query never needs inactive plans and always needs the sort.

**Business Rules:** Never hard-deleted — `is_active = false` is the only retirement mechanism, since existing `company_subscriptions` rows must permanently retain a valid FK target. `code` is immutable once any subscription references it (application-enforced).

**Soft Delete Strategy:** Not applicable in the standard sense — `is_active` serves the same functional purpose as `deleted_at` for this table's specific "retire without breaking history" need, deliberately not reusing the generic `deleted_at` pattern because "inactive for new sales" and "deleted" are different concepts here (an inactive plan is still fully valid and displayed for its existing subscribers' billing-history views).

**Audit Requirements:** Price and feature-flag changes logged to `audit_logs` — pricing changes are commercially sensitive and must be traceable to a specific admin action and timestamp.

**Future Scalability Notes:** Row count will remain in the dozens for the life of the product — no indexing/partitioning concern ever. `feature_flags` JSONB is the intentional, narrowly-scoped extensibility point for new gated features without schema migration (per Phase 1 §Subscription Management).

## 2.2 Table: `company_subscriptions`

**Purpose:** The stateful, time-bound assignment of a company to a plan.

**Business Description:** Tracks each company's subscription lifecycle — trial, active, past-due, suspended, cancelled, expired — preserving full historical record per the FINAL requirement that subscription history must never be lost.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company |
| plan_id | UUID | NO | — | Current plan classification |
| status | subscription_status_enum | NO | 'trialing' | `trialing` \| `active` \| `past_due` \| `suspended` \| `cancelled` \| `expired` |
| start_date | DATE | NO | — | — |
| end_date | DATE | NO | — | — |
| trial_end_date | DATE | YES | NULL | Populated only when this row began as a trial |
| price_at_subscription | NUMERIC(12,3) | NO | — | Snapshot of the price actually billed — protects existing subscribers from retroactive plan price changes |
| currency_at_subscription | CHAR(3) | NO | 'JOD' | Snapshot, independent of the plan's current `currency` |
| billing_cycle | billing_cycle_enum | NO | 'monthly' | `monthly` \| `yearly` |
| auto_renew | BOOLEAN | NO | true | — |
| suspended_at | TIMESTAMPTZ | YES | NULL | — |
| suspension_reason | VARCHAR(255) | YES | NULL | Required when `suspended_at` is set |
| cancelled_at | TIMESTAMPTZ | YES | NULL | — |
| cancellation_reason | VARCHAR(255) | YES | NULL | — |
| expired_at | TIMESTAMPTZ | YES | NULL | Set by scheduled job when `end_date` lapses with no renewal |
| external_billing_ref | VARCHAR(255) | YES | NULL | Neutral hook for a future billing/payment-gateway module — deliberately no gateway-specific columns here (FINAL: future billing systems must integrate without a migration on this table) |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT (never hard-delete a company with subscription history).
- `plan_id` → `subscription_plans(id)` ON DELETE RESTRICT.

**Unique Constraints:**
- `uq_company_subscriptions_one_active` — **partial unique index** on `(company_id)` WHERE `status IN ('trialing','active','past_due')`. This is the physical enforcement of "every company must have exactly one active subscription" — the single most important constraint in this module.

**Check Constraints:**
- `chk_company_subscriptions_dates` — `end_date > start_date`.
- `chk_company_subscriptions_trial_end` — `trial_end_date IS NOT NULL WHEN status = 'trialing'`.
- `chk_company_subscriptions_suspension` — `suspension_reason IS NOT NULL WHEN suspended_at IS NOT NULL`.

**Indexes:**
- `idx_company_subscriptions_company_status` composite on `(company_id, status)` — every authorization-middleware check ("does this company currently have valid access") filters exactly this shape, on the hottest read path in the entire billing subsystem (evaluated on a meaningful fraction of authenticated requests).
- `idx_company_subscriptions_end_date` on `(end_date)` WHERE `status IN ('trialing','active','past_due')` — supports the scheduled expiration job's daily sweep ("find subscriptions whose end_date has passed") without scanning cancelled/expired history.

**Business Rules:** Renewal creates a **new row**, never mutates `end_date` forward in place — append-only in practice, mirroring the `lease_contracts.prior_contract_id` philosophy from Phase 1/2, though here linkage between successive periods for the same company is achieved implicitly via `(company_id, start_date)` ordering rather than an explicit self-referencing FK, since subscription periods (unlike lease renewals) don't carry negotiated-terms lineage worth chain-walking — a simple chronological company-scoped query answers "subscription history" completely.

**Soft Delete Strategy:** None — rows are never deleted, soft or hard. The `status` state machine itself is the lifecycle mechanism; a row transitions to `cancelled`/`expired` and remains permanently queryable, which is the literal implementation of "subscription history must be preserved" and "companies cannot lose historical data after subscription expiration."

**Audit Requirements:** Every status transition logged to `audit_logs`, including the actor (system job vs. specific admin) — critical for billing dispute resolution and for the "why was this company suspended" support workflow.

**Future Scalability Notes:** Row count grows linearly with (companies × renewal frequency) — modest even at thousands of tenants (a few rows per company per year), no partitioning need foreseeable. `external_billing_ref` is the explicit, already-built extension point for a future dedicated billing-provider integration table to attach to.

---

## Module 2 (SaaS) — Architecture Review

- **Redundant columns:** None. `price_at_subscription`/`currency_at_subscription` intentionally duplicate `subscription_plans.monthly_price`/`currency` in value at the moment of creation — this is the deliberate historical-snapshot pattern (Phase 1), not accidental redundancy; flagged and confirmed correct, not fixed.
- **Missing indexes:** Initially the module draft had only the partial unique index; added `idx_company_subscriptions_company_status` and `idx_company_subscriptions_end_date` during this review pass, since both are hit on hot, high-frequency paths (auth middleware and the daily expiration sweep respectively) that would otherwise force sequential scans as the table grows.
- **Missing constraints:** Added `chk_company_subscriptions_dates` and `chk_company_subscriptions_trial_end` during this pass — date-ordering and trial-consistency invariants belong at the database level, not solely in application code, since a direct SQL write (migration script, admin tool, future service) must not be able to produce an inverted date range or an inconsistent trial state.
- **Naming inconsistencies:** None.
- **Scalability concerns:** None material for this module in isolation. Cross-module concern noted for Phase 7: the `idx_company_subscriptions_company_status` index is evaluated on a large share of authenticated requests platform-wide, making it a strong candidate for aggressive in-memory caching (e.g., a short-TTL cache of "is company X currently active") at the application layer, so that the vast majority of authorization checks never touch this index at all under normal load — noted here for Phase 7 rather than solved now, since caching strategy is out of scope for physical table design.

---

*End of Module 2.*

---

# MODULE 3 — SECURITY

This is the highest-scrutiny module in the schema. Designed for hundreds of thousands of users and thousands of companies platform-wide.

## 3.0 PostgreSQL 17 / Prisma Compatibility Notes (apply throughout this module)

- **UUIDv7 generation:** PostgreSQL 17 does **not** ship a native `uuidv7()` function (that lands in PG18). Rather than depending on the `pg_uuidv7` C extension — which many managed Postgres providers (RDS, Cloud SQL, Supabase-restricted tiers) don't whitelist — the recommended approach is a **pure PL/pgSQL `uuid_generate_v7()` function**, defined once in an early migration, used as the `DEFAULT` on every table's `id` column. This has zero extension dependency, works identically on any PG17 host, and is trivially portable to native `uuidv7()` when the platform eventually upgrades to PG18+ (a one-line `DEFAULT` change, no data migration, since the *value format* is identical). Prisma models this with `@default(dbgenerated("uuid_generate_v7()"))` on the `id` field — Prisma has no built-in UUIDv7 generator, so relying on the database default (not `@default(uuid())`, which is v4) is the correct integration point.
- **INET columns** (`ip_address` throughout this module): Prisma supports native PostgreSQL types via `@db.Inet` on a `String` field. Storage and simple equality/read queries work natively through Prisma Client; CIDR-range containment queries (e.g., "all logins from this subnet") are not expressible through Prisma's query builder and require `$queryRaw` — acceptable, since that's an infrequent security/forensics operation, not a hot path.
- **Partial, composite-with-INCLUDE, and expression indexes:** Prisma's declarative `@@index` in `schema.prisma` does not support `WHERE` clauses or `INCLUDE` columns as of current Prisma versions. Every partial/covering index specified in this module must be **hand-written as raw SQL appended to the generated Prisma migration file** (`prisma migrate dev --create-only`, then edit the SQL before applying) rather than expressed in the schema DSL. This is called out explicitly per-index below where it applies, since it's a real, recurring friction point between "correct PostgreSQL design" and "what Prisma's schema language can express" — the correct resolution is always to let the migration SQL be the source of truth for these indexes while Prisma's schema stays aware of them only via `@@index` comments/documentation, not by fighting the DSL.
- **Native enums:** Prisma supports PostgreSQL enums natively and cleanly (`enum` blocks map 1:1) — no compatibility concern for any enum column in this module.

## 3.1 Table: `users`

**Purpose:** Platform-wide authentication identity root. Not tenant-scoped by itself — a user's relationship to any given company is expressed through `user_company_roles`.

**Business Description:** A human who can authenticate — company staff, owners, or tenants using the Tenant Portal. Single identity table avoids duplicating auth machinery per user type.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| email | CITEXT | YES | NULL | Case-insensitive email; nullable — phone-only accounts are valid |
| phone | VARCHAR(20) | YES | NULL | +962 E.164 format; nullable — email-only accounts are valid |
| password_hash | TEXT | YES | NULL | Argon2id hash; nullable — an OTP-only account may never set a password |
| password_algorithm | VARCHAR(20) | NO | 'argon2id' | Explicit, not assumed — allows future algorithm migration without ambiguity |
| full_name | VARCHAR(255) | NO | — | — |
| preferred_language | CHAR(2) | NO | 'ar' | `ar` \| `en` |
| email_verified_at | TIMESTAMPTZ | YES | NULL | NULL = unverified |
| phone_verified_at | TIMESTAMPTZ | YES | NULL | NULL = unverified |
| password_reset_token_hash | TEXT | YES | NULL | SHA-256 hash of a one-time reset token; never store the raw token |
| password_reset_expires_at | TIMESTAMPTZ | YES | NULL | Short-lived (15–30 min); reset flow rejects if past |
| failed_login_attempts | SMALLINT | NO | 0 | Incremented on failed auth, reset to 0 on success |
| locked_until | TIMESTAMPTZ | YES | NULL | Set when `failed_login_attempts` crosses threshold; account lockout mechanism |
| last_login_at | TIMESTAMPTZ | YES | NULL | Denormalized for fast profile display, avoiding a `login_history` aggregate query on every profile view |
| last_login_ip | INET | YES | NULL | Same denormalization rationale |
| mfa_enabled | BOOLEAN | NO | false | MFA-ready flag |
| mfa_secret_encrypted | TEXT | YES | NULL | Encrypted at the application layer (envelope encryption, not plaintext TOTP secret) before storage; required when `mfa_enabled = true` |
| mfa_type | mfa_type_enum | YES | NULL | `totp` \| `sms`; NULL when MFA disabled |
| is_active | BOOLEAN | NO | true | Platform-wide kill switch (fraud/ToS), independent of any per-company suspension in `user_company_roles` |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | Self-referencing FK to `users(id)` |

**Primary Key:** `id`

**Foreign Keys:** `deleted_by` → `users(id)` ON DELETE SET NULL (nullable self-reference; an admin deleting a user account).

**Unique Constraints:**
- `uq_users_email` — UNIQUE INDEX on `(email)` WHERE `email IS NOT NULL AND deleted_at IS NULL` (partial: allows the same email to be reused by a new account after a prior account with that email was soft-deleted, and allows unlimited NULLs for phone-only accounts).
- `uq_users_phone` — same pattern on `(phone)` WHERE `phone IS NOT NULL AND deleted_at IS NULL`.

**Check Constraints:**
- `chk_users_email_or_phone` — `(email IS NOT NULL OR phone IS NOT NULL)` — every account needs at least one auth identifier.
- `chk_users_mfa_secret_required` — `(mfa_enabled = false) OR (mfa_secret_encrypted IS NOT NULL AND mfa_type IS NOT NULL)`.

**Indexes:**
- `uq_users_email`, `uq_users_phone` (above) — these double as the primary login-lookup indexes; every authentication request does `WHERE email = $1 AND deleted_at IS NULL`, so the partial unique index *is* the performance-critical index, not a separate one. Better than a plain (non-partial) unique index because it keeps the index smaller (excludes soft-deleted rows, which accumulate over time) and allows email reuse post-deletion, which a naive full-table unique constraint would block forever.
- `idx_users_locked_until` on `(locked_until)` WHERE `locked_until IS NOT NULL` — supports a scheduled job or lazy-check clearing expired lockouts; partial because the overwhelming majority of rows have `locked_until = NULL` at any time, so a full index would be almost entirely dead weight.

**Business Rules:** `failed_login_attempts` increments atomically on failed auth; on reaching a threshold (application-configured, e.g., 5), `locked_until = now() + interval` is set — the lockout duration itself is application policy, not a DB constraint, since it may need to change without a migration.

**Soft Delete Strategy:** Standard `deleted_at`. A soft-deleted user immediately fails authentication (login queries always filter `deleted_at IS NULL`) while their historical `audit_logs`/`login_history`/financial-actor references remain intact and attributable.

**Audit Requirements:** Password changes, email/phone changes, MFA enable/disable, and account lock/unlock events are all written to `audit_logs` with `entity_name = 'users'` — these are the canonical account-security-relevant mutations.

**Future Scalability Notes:** At 500,000 users, this table is still comfortably sized for a single unpartitioned table (a few hundred MB even with TOAST'd text columns) — no partitioning needed. The two partial unique indexes remain small and fast indefinitely since they scale with *active* accounts, not cumulative history.

## 3.2 Table: `user_company_roles`

**Purpose:** Tenant membership — the actual multi-tenant join between `users` and `companies`, carrying the assigned role.

**Business Description:** One row per user-per-company relationship. A user with memberships at multiple companies (e.g., an employee at Company A who also rents from Company B) has multiple rows here.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| user_id | UUID | NO | — | — |
| company_id | UUID | NO | — | — |
| role_id | UUID | NO | — | Current assigned role |
| status | membership_status_enum | NO | 'invited_pending' | `invited_pending` \| `active` \| `suspended` |
| invited_at | TIMESTAMPTZ | NO | now() | — |
| joined_at | TIMESTAMPTZ | YES | NULL | Set when invite accepted / status → active |
| suspended_at | TIMESTAMPTZ | YES | NULL | — |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete (offboarding) |

**Primary Key:** `id`

**Foreign Keys:**
- `user_id` → `users(id)` ON DELETE CASCADE (soft-delete cascades in practice; hard-delete only in rare admin GDPR/PDPL-erasure flows).
- `company_id` → `companies(id)` ON DELETE CASCADE.
- `role_id` → `roles(id)` ON DELETE RESTRICT — cannot delete a role while any membership references it (prevents users silently losing all permissions, per Phase 1 §1.3 business rule).

**Unique Constraints:** `uq_user_company_roles_user_company` on `(user_id, company_id)` WHERE `deleted_at IS NULL` — one active membership row per user per company; a role change is an UPDATE to `role_id` on the existing row, not a new row (single-role-per-tenant-per-user model; extensible to multi-role via a future join table without touching this table's structure, should that ever be required).

**Check Constraints:** `chk_user_company_roles_joined_at` — `joined_at IS NULL OR joined_at >= invited_at`.

**Indexes:**
- `idx_user_company_roles_user_id` on `(user_id)` WHERE `deleted_at IS NULL` — supports "which companies can this user access" — evaluated on **every authenticated request** during company-context resolution (e.g., multi-tenant users switching between companies in the UI), making this one of the hottest-path indexes in the whole schema.
- `idx_user_company_roles_company_status` composite on `(company_id, status)` WHERE `deleted_at IS NULL` — supports "list active staff for company X" (employee management screens); `company_id` first because every query in this access pattern is always scoped to one company (RLS-aligned), `status` second because the query virtually always filters to `active` — column order follows the equality-then-equality selectivity/usage convention (most-queried-together-first), not just cardinality.

**Business Rules:** A company always retains an immutable system-seeded Owner-role membership (Phase 1 §1.3) — enforced at the application layer (a DB constraint preventing "last owner deletion" is expressible via a trigger but deliberately left to application logic here, since the rule involves a company-wide aggregate check that's cheap enough at write time and clearer to reason about outside a trigger).

**Soft Delete Strategy:** Standard — offboarding a staff member soft-deletes the membership row (not the `users` row, which may still be valid for a different company).

**Audit Requirements:** Role assignment changes and status transitions (especially `suspended`) are audit-logged — a permission escalation or account suspension is a security-relevant event by definition.

**Future Scalability Notes:** Row count scales as (avg. memberships per user × 500,000 users) — comfortably in the low millions at full platform scale, no partitioning need; both indexes remain efficient at that scale since they're equality-first composites over a bounded-cardinality status enum.

## 3.3 Table: `roles`

**Purpose:** RBAC role definitions — system-seeded defaults plus tenant-defined custom roles.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | YES | NULL | NULL = system role (platform-owned); populated = tenant-custom role |
| code | VARCHAR(50) | NO | — | Machine key, e.g. `owner`, `accountant` |
| name_en | VARCHAR(100) | NO | — | — |
| name_ar | VARCHAR(100) | NO | — | — |
| is_system | BOOLEAN | NO | false | Redundant-looking but deliberately explicit alongside `company_id IS NULL` — see Architecture Review |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete — custom roles only; system roles are never deletable (app-enforced) |

**Primary Key:** `id`

**Foreign Keys:** `company_id` → `companies(id)` ON DELETE CASCADE (custom roles only; NULL rows unaffected).

**Unique Constraints:**
- `uq_roles_system_code` on `(code)` WHERE `company_id IS NULL AND deleted_at IS NULL` — system role codes are globally unique.
- `uq_roles_company_code` on `(company_id, code)` WHERE `company_id IS NOT NULL AND deleted_at IS NULL` — custom role codes unique per company.

**Check Constraints:** `chk_roles_is_system_consistency` — `(is_system = true AND company_id IS NULL) OR (is_system = false AND company_id IS NOT NULL)`.

**Indexes:** `idx_roles_company_id` on `(company_id)` WHERE `company_id IS NOT NULL AND deleted_at IS NULL` — supports "list custom roles for company X" (role-management settings screen); partial since the majority of platform-wide role rows are the handful of system roles, not worth indexing generically.

**Business Rules:** System Owner role's permission set cannot be edited below `company.manage` (application-enforced, per Phase 1 §1.3).

**Soft Delete Strategy:** Custom roles only; a role in active use (referenced by `user_company_roles`) cannot be soft-deleted without first reassigning affected memberships (RESTRICT FK above already guarantees this at the DB level for any delete attempt).

**Audit Requirements:** Custom role creation/edit logged.

**Future Scalability Notes:** Tiny table (dozens of system roles + a handful of custom roles per company) — no scale concern ever.

## 3.4 Table: `permissions`

**Purpose:** Platform-level, code-governed permission key catalog.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| key | VARCHAR(100) | NO | — | e.g. `contracts.create`, `payments.approve` |
| module | VARCHAR(50) | NO | — | Grouping for admin UI (`contracts`, `payments`, `reports`) |
| description_en | VARCHAR(255) | NO | — | — |
| description_ar | VARCHAR(255) | NO | — | — |
| is_deprecated | BOOLEAN | NO | false | Permission keys are never hard-deleted once shipped (may be referenced by historical `role_permissions`/`audit_logs`); deprecated keys are excluded from new-role UI |
| created_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:** None — root reference table, seeded via application-code migration, not user-editable.

**Unique Constraints:** `uq_permissions_key` on `(key)`.

**Check Constraints:** None required — `key` format is validated at the application/seed-script layer, not worth a regex CHECK for a table only the platform team writes to.

**Indexes:** `idx_permissions_module` on `(module)` WHERE `is_deprecated = false` — supports the role-editor UI's grouped permission picker.

**Business Rules:** Never hard-deleted; `is_deprecated` is the retirement mechanism, mirroring `subscription_plans.is_active`'s pattern for the same "never break historical references" reason.

**Soft Delete Strategy:** Not applicable — see `is_deprecated` above.

**Audit Requirements:** Changes here are platform-release events, not tenant actions — logged separately in deployment/release history, not `audit_logs` (which is tenant-action-scoped).

**Future Scalability Notes:** Dozens to low hundreds of rows ever — no concern.

## 3.5 Table: `role_permissions`

**Purpose:** RBAC junction — which permissions a role grants.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| role_id | UUID | NO | — | — |
| permission_id | UUID | NO | — | — |
| granted_at | TIMESTAMPTZ | NO | now() | — |
| granted_by | UUID | YES | NULL | FK to `users`; nullable for system-role seed grants with no human actor |

**Primary Key:** `id`

**Foreign Keys:**
- `role_id` → `roles(id)` ON DELETE CASCADE — junction row has no meaning without its role.
- `permission_id` → `permissions(id)` ON DELETE RESTRICT.
- `granted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_role_permissions_role_permission` on `(role_id, permission_id)` — prevents duplicate grants of the same permission to the same role.

**Indexes:**
- `idx_role_permissions_role_id` on `(role_id)` — this is the single most performance-critical index in the entire Security module: **every authorization check on every API request** resolves "what permissions does this user's role grant" via this exact lookup. Estimated benefit: without it, permission resolution on a table that could reach low millions of rows (thousands of companies × custom roles × permissions-per-role) degrades from an index seek (sub-millisecond) to a sequential scan (potentially tens of milliseconds+) on the hottest possible path — unacceptable, since this check happens on effectively every authorized request. The `uq_role_permissions_role_permission` unique constraint's implicit composite index already covers `(role_id, permission_id)` lookups, but a **dedicated single-column index on `role_id` alone** is still justified and non-redundant: the composite unique index is only useful when both columns are known (checking one specific permission), while permission-resolution needs *all* permissions for a role (an index range-scan on `role_id` alone, which the composite index also technically supports as a leftmost-prefix — meaning in practice PostgreSQL can use the unique index for both purposes and a separate single-column index would be redundant. **Correction applied during this pass:** the standalone `idx_role_permissions_role_id` is dropped as genuinely redundant with the leftmost prefix of `uq_role_permissions_role_permission(role_id, permission_id)` — PostgreSQL will use that unique index for `WHERE role_id = $1` just as efficiently. This redundancy is called out explicitly here rather than silently fixed, because "avoid unnecessary indexes" is a stated requirement and this is exactly the kind of near-miss a less careful review would ship.

**Business Rules:** Granting/revoking permissions on a role takes effect immediately for all members via the shared `role_id` lookup — no per-user permission caching at the DB layer (caching, if needed, is an application/Redis concern, out of scope here).

**Soft Delete Strategy:** Hard-deleted on revoke (junction row, no independent historical value beyond what `audit_logs` already captures via `granted_at`/`granted_by` history at grant time — a revoke is logged to `audit_logs` separately).

**Audit Requirements:** Every grant and revoke logged to `audit_logs` — permission changes are the definition of a security-relevant event.

**Future Scalability Notes:** Bounded by (roles × permissions-per-role) — even at thousands of companies with custom roles, this stays in the tens-of-thousands to low-hundreds-of-thousands range, not a partitioning candidate.

## 3.6 Table: `refresh_tokens`

**Purpose:** Long-lived credential backing JWT access-token refresh, with rotation and theft detection.

**Business Description:** Implements refresh token rotation: each use of a refresh token issues a new one and marks the old one `rotated`. If a `rotated` (already-used) token is ever presented again, that's a replay/theft signal, and the entire rotation family is revoked.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| user_id | UUID | NO | — | — |
| token_hash | TEXT | NO | — | SHA-256 hash of the token — the raw token is never persisted, only ever held client-side |
| family_id | UUID | NO | — | Shared across an entire rotation chain originating from one login; the theft-detection unit |
| replaced_by_token_id | UUID | YES | NULL | Self-referencing; set when this token is rotated, pointing at its successor |
| device_fingerprint | TEXT | YES | NULL | Hashed client fingerprint (installed app instance / browser) for session-list display and anomaly detection |
| device_name | VARCHAR(255) | YES | NULL | User-friendly label ("iPhone 15, Amman") shown in "manage sessions" UI |
| ip_address | INET | NO | — | — |
| user_agent | TEXT | YES | NULL | — |
| issued_at | TIMESTAMPTZ | NO | now() | — |
| expires_at | TIMESTAMPTZ | NO | — | — |
| revoked_at | TIMESTAMPTZ | YES | NULL | — |
| revoked_reason | revoke_reason_enum | YES | NULL | `rotated` \| `logout` \| `theft_detected` \| `admin_revoked` \| `expired` |

**Primary Key:** `id`

**Foreign Keys:**
- `user_id` → `users(id)` ON DELETE CASCADE.
- `replaced_by_token_id` → `refresh_tokens(id)` ON DELETE SET NULL — self-referencing rotation chain.

**Unique Constraints:** `uq_refresh_tokens_token_hash` on `(token_hash)` — prevents any possibility of duplicate-hash collision being silently accepted as a valid distinct session (astronomically unlikely with SHA-256, but the constraint is free and closes the theoretical gap).

**Check Constraints:** `chk_refresh_tokens_revoked_reason` — `revoked_reason IS NOT NULL WHEN revoked_at IS NOT NULL`.

**Indexes:**
- `uq_refresh_tokens_token_hash` (above) — **the** lookup path for every token-refresh API call (`WHERE token_hash = $1`); this is on the hottest possible path (evaluated on every access-token renewal, which happens far more often than login itself). Alternative considered and rejected: looking up by `id` first (JWT could carry the token's UUID as a claim) — rejected because it would require trusting a client-supplied ID before verifying the token's actual secret, whereas hashing the full presented token and looking up by hash means an attacker who doesn't possess the exact token cannot even reach a row, which is a stronger security property, not just a performance one.
- `idx_refresh_tokens_user_id` on `(user_id)` WHERE `revoked_at IS NULL` — supports "list this user's active sessions" (session management UI) and "revoke all sessions for user X" (logout-everywhere / forced-logout-on-password-change flows); partial because revoked/expired tokens vastly outnumber active ones over time and are irrelevant to this query.
- `idx_refresh_tokens_family_id` on `(family_id)` — supports theft-detection's core operation: "revoke every token descended from this family" the instant a replay is detected. Not made partial (unlike the user_id index) because theft detection must also find already-revoked/rotated tokens within a family (to confirm the presented token really was previously rotated-out, not just currently active), so filtering out revoked rows here would break the exact check this index exists for.
- `idx_refresh_tokens_expires_at` on `(expires_at)` WHERE `revoked_at IS NULL` — supports a scheduled cleanup job purging/archiving expired tokens; without this, the cleanup sweep would sequential-scan the whole table as it grows.

**Business Rules:** On theft detection (a `revoked_reason = 'rotated'` token presented again), the application revokes **every row sharing that `family_id`** in one transaction and should force a step-up re-authentication for the affected user — this is an application-orchestrated multi-row UPDATE, not a DB trigger, since it also needs to trigger a user-facing security notification (outside the DB's concern).

**Soft Delete Strategy:** **Not soft-deleted.** This is a deliberate deviation from the Global Convention, called out explicitly: refresh tokens are ephemeral security artifacts, not durable business records — `revoked_at`/`revoked_reason` already fully capture their lifecycle state, and rows are hard-deleted by a retention-policy cleanup job (e.g., 90 days past `expires_at` or `revoked_at`) purely to bound table size, not for business-data-integrity reasons. Applying the standard `deleted_at` soft-delete pattern here would be redundant with `revoked_at` and would leave a permanently growing table with no purge path — the wrong tradeoff for this specific table.

**Audit Requirements:** Theft-detection events (`revoked_reason = 'theft_detected'`) are always also written to `audit_logs` with `severity = 'critical'` — this is the single most security-critical event type the whole schema can produce.

**Future Scalability Notes:** Highest-write-volume table in this module relative to its per-row lifespan (every login + every silent refresh = a write). At 500,000 users with realistic session counts, active-row count stays bounded (partial indexes keep it fast), but *cumulative* row count without the cleanup job would grow unbounded — the retention-purge job is not optional, it's load-bearing for this table's long-term performance, called out explicitly here so it isn't missed at implementation time.

## 3.7 Table: `login_history`

**Purpose:** Immutable authentication-attempt audit trail — every login attempt, successful or not.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| user_id | UUID | YES | NULL | NULL when the attempted identifier didn't match any account — a failed lookup must still be logged for brute-force detection, and cannot FK to a nonexistent user |
| attempted_identifier | VARCHAR(255) | YES | NULL | The raw email/phone attempted, stored even on lookup failure, specifically to support pattern detection across failed attempts against the same identifier |
| company_id | UUID | YES | NULL | Populated when the login was company-context-scoped (e.g., tenant portal login flow tied to a specific company's branding/subdomain) |
| status | login_status_enum | NO | — | `success` \| `failed_password` \| `failed_locked` \| `failed_mfa` \| `failed_not_found` |
| ip_address | INET | NO | — | — |
| user_agent | TEXT | YES | NULL | — |
| device_fingerprint | TEXT | YES | NULL | — |
| geolocation_country | CHAR(2) | YES | NULL | Coarse geo (country-level only, minimizing PII footprint) for impossible-travel/anomaly detection |
| created_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:**
- `user_id` → `users(id)` ON DELETE SET NULL — a deleted user's login history is retained (security retention requirement) but the FK link is severed on hard-delete (soft-delete, the normal case, doesn't touch this at all).
- `company_id` → `companies(id)` ON DELETE SET NULL.

**Unique Constraints:** None — every attempt is a distinct event by nature; no meaningful uniqueness beyond the PK.

**Check Constraints:** `chk_login_history_success_has_user` — `(status != 'success') OR (user_id IS NOT NULL)` — a successful login must always be attributable to a real user.

**Indexes:**
- `idx_login_history_user_id_created_at` composite on `(user_id, created_at DESC)` WHERE `user_id IS NOT NULL` — supports "show this user's recent login activity" (account security page); `user_id` first because that's the equality filter, `created_at DESC` second because the query always wants most-recent-first, making this a genuine covering-order composite (avoids a separate sort step entirely, not just an equality filter). Estimated benefit: turns an O(n log n) sort-after-scan into a direct index-ordered scan.
- `idx_login_history_ip_created_at` composite on `(ip_address, created_at DESC)` — supports brute-force/rate-limiting queries ("how many failed attempts from this IP in the last N minutes") — evaluated on every login attempt as a pre-check, making it a hot path despite the table's huge overall size.
- `idx_login_history_status_created_at` composite on `(status, created_at DESC)` WHERE `status != 'success'` — supports the security-monitoring dashboard's "recent failed attempts platform-wide" view; partial because successful logins (the large majority) are irrelevant to this specific monitoring query, keeping the index a fraction of the table's size despite the table itself being enormous.

**Business Rules:** Append-only — no `UPDATE`s ever occur on existing rows (not even soft-delete).

**Soft Delete Strategy:** **Not applicable.** Like `refresh_tokens`, this is a deliberate deviation from the Global Convention: `login_history` is a pure event log, not a mutable business record — there is nothing to "soft delete," a row either exists as a permanent historical fact or is purged entirely by a retention job once past the statutory/security retention window (addressed in scalability section below via partitioning, which makes bulk retention-driven deletion cheap).

**Audit Requirements:** This table *is* itself a category of audit trail (authentication-specific); it is not additionally mirrored into the generic `audit_logs` table for every row (that would be pure duplication) — only *security-significant derived events* (e.g., "account locked after N failures," "impossible travel detected") get a corresponding `audit_logs` entry, cross-referencing this table via `metadata`.

**Future Scalability Notes:** Explicitly named in the scale target (20 million rows) — this table is a first-class **partitioning candidate**, detailed fully in §3.11.

## 3.8 Table: `audit_logs`

**Purpose:** Immutable, append-only record of every significant state change platform-wide — the PDPL-driven accountability backbone.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| actor_user_id | UUID | YES | NULL | Who performed the action; nullable for system/scheduled-job-initiated changes |
| company_id | UUID | YES | NULL | Tenant scope; nullable for platform-level actions not tied to any single tenant (e.g., a platform-admin editing `subscription_plans`) |
| entity_name | VARCHAR(100) | NO | — | The table/domain entity affected, e.g. `lease_contracts` |
| entity_id | UUID | YES | NULL | The specific row affected; nullable for actions not tied to one row (e.g., a bulk export, a report generation) |
| action | audit_action_enum | NO | — | `create` \| `update` \| `delete` \| `soft_delete` \| `restore` \| `login` \| `logout` \| `permission_change` \| `export` \| `status_change` |
| previous_values | JSONB | YES | NULL | Pre-change snapshot of affected fields; NULL for `create` |
| new_values | JSONB | YES | NULL | Post-change snapshot; NULL for `delete` |
| occurred_at | TIMESTAMPTZ | NO | now() | — |
| ip_address | INET | YES | NULL | — |
| user_agent | TEXT | YES | NULL | — |
| request_id | UUID | YES | NULL | Correlates this entry to the single HTTP request that produced it |
| correlation_id | UUID | YES | NULL | Correlates this entry to a broader multi-step business transaction spanning several requests |
| metadata | JSONB | NO | '{}' | Free-form action-specific context |
| severity | audit_severity_enum | NO | 'info' | `info` \| `warning` \| `critical` |
| source | audit_source_enum | NO | 'api' | `api` \| `web` \| `mobile` \| `system_job` \| `admin_console` |
| created_at | TIMESTAMPTZ | NO | now() | — |

**Why every field exists (explicitly required):**
- **`actor_user_id`** — accountability: "who did this." Nullable because not every state change has a human actor (scheduled billing jobs, automated lease-status refreshes).
- **`company_id`** — tenant-scoping so a company admin's audit-trail view can be correctly filtered/RLS-isolated; nullable for the narrow class of platform-level (cross-tenant) actions.
- **`entity_name`** — identifies *what kind* of record changed, enabling both entity-specific history views and platform-wide "all changes to contracts this week" style monitoring.
- **`entity_id`** — identifies *which specific* record, enabling the single most common support/dispute-resolution query: "show me everything that happened to this exact row."
- **`action`** — the operation type; distinguishes creation from modification from deletion from more semantically specific events (`permission_change`, `status_change`) that deserve their own category rather than being buried as a generic `update`.
- **`previous_values` / `new_values`** — the actual diff, in JSONB rather than two separate full-row snapshots or a generic text diff, because JSONB allows storing only the *changed* fields (not the whole row) which keeps rows small while still being precisely queryable (`previous_values->>'rent_amount'`) — this is the PDPL-driven "demonstrable accountability for who accessed/modified personal data" requirement made concrete.
- **`occurred_at`** — chronological ordering and retention-window calculation; separate from `created_at` deliberately (see Architecture Review — flagged and resolved).
- **`ip_address` / `user_agent`** — forensic context matching the same pattern as `login_history`, letting a security investigation correlate an audit event with the network/device context it originated from.
- **`request_id`** — ties this specific database change to the exact API request that caused it, which is invaluable for debugging ("this contract got corrupted — which request did it?") independent of any broader business-workflow context.
- **`correlation_id`** — ties together *multiple* audit rows that resulted from one logical business operation spanning several requests or several row changes in one transaction (e.g., a lease renewal simultaneously creating a new `lease_contracts` row, marking the old one `superseded`, and generating a notification) — without this, reconstructing "what actually happened when this tenant renewed" requires manually correlating timestamps across unrelated rows; with it, one query (`WHERE correlation_id = $1`) returns the complete transaction.
- **`metadata`** — a bounded, deliberately narrow escape hatch for action-specific context not worth a dedicated column (e.g., a `reason` string for a manual override, an `export_format` for a data export) — avoiding a proliferation of nullable action-type-specific columns on this one shared table.
- **`severity`** — triage signal letting a security-monitoring dashboard surface `critical` events (e.g., permission escalation, refresh-token-theft-triggered entries) without scanning/filtering through the much larger volume of routine `info`-level CRUD noise.
- **`source`** — distinguishes a user-driven change (web/mobile/api) from a system-initiated one (`system_job`) or a platform-operator action (`admin_console`) — relevant both for security review (an `admin_console` action touching tenant data is inherently higher-scrutiny) and for debugging.

**Primary Key:** `id`

**Foreign Keys:**
- `actor_user_id` → `users(id)` ON DELETE SET NULL.
- `company_id` → `companies(id)` ON DELETE SET NULL — deliberately SET NULL, not CASCADE or RESTRICT: a company's audit history must survive even a (rare, admin-driven) hard-delete of the company itself, since audit records are the accountability trail *of* that deletion event too.

**Unique Constraints:** None — every logged event is a distinct fact by nature.

**Check Constraints:** `chk_audit_logs_entity_id_consistency` — none imposed requiring `entity_id` presence tied to `action`, deliberately, since some legitimate actions (`export`, bulk operations) are entity-scoped without a single-row `entity_id` — over-constraining here would reject valid real-world log entries.

**Indexes:**
- `idx_audit_logs_company_occurred_at` composite on `(company_id, occurred_at DESC)` WHERE `company_id IS NOT NULL` — covers the #1 UI query: a company admin's own audit trail, most-recent-first. Partial because platform-level (`company_id IS NULL`) rows are a small minority and irrelevant to every tenant-scoped view.
- `idx_audit_logs_entity` composite on `(entity_name, entity_id, occurred_at DESC)` WHERE `entity_id IS NOT NULL` — covers "full change history of this specific record," the standard support/dispute-resolution query; three-column composite ordered entity_name-then-entity_id because `entity_name` alone is low-cardinality (a few dozen distinct values) and not independently useful, so it only earns its place as the leading column of a composite that's actually filtered on both together.
- `idx_audit_logs_actor_user_id` composite on `(actor_user_id, occurred_at DESC)` WHERE `actor_user_id IS NOT NULL` — supports "what has this user done" for security investigation/offboarding review.
- **No GIN index on `metadata`** — deliberately omitted. Free-text/structured search *inside* the metadata blob is not a stated query requirement, and a GIN index carries real write-amplification cost on a table already sized in the millions of append-only rows; this is exactly the "avoid unnecessary indexes" principle in practice — add it later, specifically, if and when a concrete query pattern demands it, rather than speculatively now.

**Business Rules:** Application code must never `UPDATE` or hard-`DELETE` a row here except via an explicit, separately-audited statutory-retention purge process — enforced by convention/application-layer discipline (a `REVOKE UPDATE, DELETE ON audit_logs FROM app_role` at the database-role level, with purges executed through a distinct, more privileged role, is the recommended production hardening, noted here for Phase 8).

**Soft Delete Strategy:** Not applicable — immutable append-only by design; there is no "soft delete" concept for an audit record, since deleting an audit record (even softly) undermines the entire purpose of the table.

**Audit Requirements:** N/A — this table *is* the audit requirement for the rest of the schema.

**Future Scalability Notes:** Explicitly named in the scale target (5 million rows) — first-class **partitioning candidate**, detailed in §3.11.

## 3.9 Security Requirements Mapping

| Requirement | Mechanism |
|---|---|
| RBAC | `roles` / `permissions` / `role_permissions` / `user_company_roles` |
| Principle of Least Privilege | System-seeded roles ship with minimal default grants; custom roles start empty and are additive-only via explicit `role_permissions` grants |
| Refresh Token Rotation | `refresh_tokens.replaced_by_token_id` chain; every refresh issues a new row and marks the old `revoked_reason='rotated'` |
| Session Revocation | `refresh_tokens.revoked_at`/`revoked_reason`; bulk revoke via `family_id` or `user_id` |
| Device Tracking | `refresh_tokens.device_fingerprint`/`device_name`; `login_history.device_fingerprint` |
| Login History | `login_history` table, full attempt log |
| Password Reset | `users.password_reset_token_hash`/`password_reset_expires_at` |
| Email Verification | `users.email_verified_at` |
| Account Lockout | `users.failed_login_attempts`/`locked_until` |
| Failed Login Attempts | `users.failed_login_attempts`; every attempt also logged in `login_history` regardless of outcome |
| Session Expiration | `refresh_tokens.expires_at`; access tokens (JWT, not persisted server-side) carry their own short expiry independently |
| MFA Ready | `users.mfa_enabled`/`mfa_secret_encrypted`/`mfa_type`; `login_history.status = 'failed_mfa'` |
| Audit Logging | `audit_logs` |
| Soft Delete | `deleted_at` on `users`, `user_company_roles`, `roles` — explicitly **not** on `refresh_tokens`/`login_history`/`audit_logs`, each with its own documented rationale above |
| Tenant Isolation | `company_id` + RLS on every tenant-scoped table in this module; `users`/`permissions` are the two intentional exceptions (platform-wide roots), detailed in Phase 8 |
| Secure Password Storage | Argon2id via `users.password_hash`, never reversible |
| Secure Token Storage | `refresh_tokens.token_hash` (SHA-256 of the token, never the raw token); `password_reset_token_hash` same pattern |
| Replay Attack Prevention | Rotation chain — a `rotated` token presented again is a hard replay signal |
| Refresh Token Theft Detection | `family_id` revocation cascade on replay detection |
| Suspicious Login Detection | `login_history.geolocation_country` + `ip_address`, enabling application-layer impossible-travel heuristics against recent rows |
| IP Address Tracking | `ip_address` (INET) on `refresh_tokens`, `login_history`, `audit_logs` |
| User Agent Tracking | `user_agent` on the same three tables |
| Session Fingerprinting | `device_fingerprint` on `refresh_tokens`/`login_history` |
| Concurrent Session Management | Multiple active `refresh_tokens` rows per `user_id` are valid by design (multi-device); "manage sessions" UI lists/revokes individually via `idx_refresh_tokens_user_id` |
| API Key Ready (future) | No dedicated table built now (not in the required 8) — the identical `token_hash`-lookup + `revoked_at` pattern used by `refresh_tokens` is the template a future `api_keys` table would reuse verbatim, requiring no change to this module's design philosophy |

## 3.10 Performance Optimization Explanation

- **Authentication:** partial unique indexes on `users.email`/`users.phone` give O(log n) lookup on the exact WHERE-clause shape the login query uses; keeping them partial (excluding soft-deleted rows) keeps the index size proportional to *active* accounts only.
- **Authorization / Permission Resolution:** the `role_permissions` composite unique index, used as a leftmost-prefix scan on `role_id`, resolves a user's full permission set in a single index range-scan — this is evaluated on effectively every authorized request, so its cost dominates the authorization-layer's total DB load, and it is the one index in this module optimized above all others.
- **Session Validation / JWT Refresh:** `refresh_tokens.token_hash` unique index gives direct, single-row lookup; access-token validation itself never touches the database at all (JWT signature verification is stateless), so only the *refresh* operation is a DB-bound path, and it's a single indexed point lookup.
- **Dashboard queries (audit trail, login activity, active sessions):** all three are served by composite indexes with the sort column embedded in index order (`occurred_at DESC` / `created_at DESC` as the trailing composite column), turning "filter + sort + paginate" into a single ordered index scan rather than filter-then-sort — this is the single most impactful repeated pattern across this module's index design.
- **Search / Filtering:** no free-text search requirement identified in this module (audit-log `metadata` search explicitly deferred, see §3.8) — all filtering needs here are equality/range on low-cardinality or indexed columns, fully served by the composites above without needing trigram/full-text infrastructure.
- **Pagination:** every composite index with a `DESC` timestamp trailing column directly supports keyset pagination (`WHERE (company_id, occurred_at) < ($1, $2) ORDER BY occurred_at DESC LIMIT 50`) — the recommended pagination strategy for these high-row-count tables, explicitly preferred over `OFFSET`-based pagination, which degrades linearly with offset depth and would become a real problem on a 5–20 million row table.
- **JOIN Performance:** every FK in this module is backed by an index either directly (the FK's own column) or as the leading column of a composite already justified above for its own query pattern — no FK in this module lacks index coverage on its own column, avoiding the classic "FK exists but isn't indexed, so every join on it sequential-scans the child table" failure mode.
- **GROUP BY / Aggregations:** the one realistic aggregation need in this module — "failed login count by IP in the last N minutes" — is served directly by `idx_login_history_ip_created_at`, since the aggregate's WHERE/GROUP shape matches the index's leading columns exactly.

## 3.11 Scalability at Target Scale

| Scale point | Impact |
|---|---|
| 10,000 companies | Trivial for every table in this module — `roles`, `permissions`, `role_permissions` stay in the tens-of-thousands of rows; `user_company_roles` in the low millions at most. No structural change needed. |
| 500,000 users | `users` table itself stays comfortably single-table-sized (a few hundred MB); partial unique indexes on email/phone remain small since they track only active accounts. No partitioning need for `users`. |
| 5,000,000 audit_logs rows | This is where unpartitioned design starts to strain: index maintenance cost on every INSERT (multiple composite indexes) grows, VACUUM cycles take longer, and any accidental unindexed query becomes very expensive very fast. **Recommendation: RANGE partition `audit_logs` by `occurred_at`, monthly.** Each partition stays in the low-hundred-thousand-row range at this volume, keeping indexes small and VACUUM fast per-partition; old partitions can be moved to cheaper storage or dropped wholesale once past the PDPL-driven retention window — a single `DROP PARTITION` instead of a slow `DELETE ... WHERE occurred_at < ...` that would otherwise have to scan and remove millions of rows with full index maintenance. |
| 20,000,000 login_history rows | Same reasoning, more acute given the larger volume. **Recommendation: RANGE partition `login_history` by `created_at`, monthly** (or weekly if login volume is heavily concentrated, revisit based on actual observed volume post-launch). This table's write pattern (every single login attempt, success or failure, from every user) makes it the single highest-write-throughput table in the whole schema, and partitioning is what keeps that throughput from degrading as history accumulates. |

**Should any other table in this module be partitioned?** No — `refresh_tokens` is kept unpartitioned because it's actively purged by the retention-cleanup job (§3.6), which bounds its steady-state size regardless of cumulative login volume, unlike `audit_logs`/`login_history` which are retained far longer for compliance/security reasons and therefore genuinely accumulate without bound.

**Partition key column requirement:** both `occurred_at` (audit_logs) and `created_at` (login_history) already exist as `NOT NULL` columns with sensible defaults — no schema addition needed to introduce partitioning later, only a table-rebuild migration (`CREATE TABLE ... PARTITION BY RANGE`, backfill, swap) — flagged here so this migration is planned for, not discovered as a surprise once these tables are already large and a live repartitioning migration becomes materially more disruptive.

## 3.12 Database Integrity

| Risk | Prevention mechanism |
|---|---|
| Orphan `user_company_roles` rows (dangling user/company/role) | All three FKs (`user_id`, `company_id`, `role_id`) are `NOT NULL` with real FK constraints — no orphan is representable at the schema level. |
| Circular dependency: `companies.created_by → users.id`, `users` has no direct `company_id` | Not actually circular — `users` is a platform-wide root with no FK back to `companies`; only `companies.created_by`/`updated_by`/`deleted_by` reference `users`, a one-directional edge. No cycle exists in the FK graph for this module. |
| Circular dependency: `roles.company_id → companies.id` alongside `user_company_roles.role_id → roles.id` and `user_company_roles.company_id → companies.id` | Also not a true cycle — both are independent edges into `companies` and `roles` respectively; `user_company_roles` is the join, not a participant in a cycle. |
| Duplicate sessions (same token issued twice) | `uq_refresh_tokens_token_hash` — a cryptographically near-impossible collision is still constraint-blocked. |
| Duplicate permissions (same permission granted to a role twice) | `uq_role_permissions_role_permission` on `(role_id, permission_id)`. |
| Duplicate roles (same code twice for the same scope) | `uq_roles_system_code` / `uq_roles_company_code`, both partial-scoped correctly to system vs. tenant roles. |
| Duplicate audit records | Deliberately **not** constrained — each audit entry is allowed to be identical in content to another if the same action genuinely occurred twice (e.g., two separate identical permission grants at different times); uniqueness here would be a false constraint on legitimate repeated real-world events. Deduplication, if ever needed, is an application/reporting-layer concern, not a database-integrity one. |
| Duplicate active membership (same user, same company, two active roles) | `uq_user_company_roles_user_company` partial-unique on `(user_id, company_id) WHERE deleted_at IS NULL`. |

---

## Module 3 (Security) — Architecture Review

- **Security:** Reviewed against every requirement in the module brief (§3.9 mapping) — full coverage confirmed, no gaps. One deliberate hardening note carried forward to Phase 8: `audit_logs` should be protected at the database-role level (`REVOKE UPDATE, DELETE`) in addition to application-layer discipline, since application-layer-only enforcement is not a sufficient guarantee against a compromised application credential.
- **PostgreSQL Performance / Index Quality:** One genuine redundancy was caught and corrected during this pass — the initially-drafted standalone `idx_role_permissions_role_id` was identified as fully redundant with the leftmost prefix of `uq_role_permissions_role_permission(role_id, permission_id)` and removed rather than shipped; documented in place (§3.5) rather than silently deleted, since showing the caught mistake is more useful than hiding it.
- **Constraint Quality:** Every partial-unique pattern in this module (`users` email/phone, `roles` system/company code, `user_company_roles` membership) consistently follows the same "exclude soft-deleted rows, allow value reuse after deletion" shape — verified consistent across all four instances, not ad-hoc per table.
- **Query Cost:** Every composite index in this module was reviewed for column-order correctness against its actual target query shape (equality columns before range/sort columns; sort-direction embedded to avoid a separate sort step) — no index found with the wrong leading column for its stated purpose.
- **Multi-Tenant Isolation / RLS Compatibility:** `users` and `permissions` are confirmed as the two intentional non-tenant-scoped tables in this module (platform-wide identity root and platform-wide permission catalog respectively) — every other table carries `company_id` (directly or, for `role_permissions`/`refresh_tokens`, transitively through `roles`/`users`) and is RLS-ready per the Global Convention. `refresh_tokens` and `login_history`, notably, have **no direct `company_id` column** — flagged here explicitly: their RLS policy (Phase 8) must instead be expressed as a join-based policy against `users`/`user_company_roles`, since a user's own session/login history is scoped to *the user*, not to a single company, which is correct given a user can belong to multiple companies — this is a deliberate design choice, not an oversight, but it does mean these two tables need a non-standard RLS policy shape, called out now so Phase 8 doesn't have to rediscover it.
- **Future Scalability:** `audit_logs` and `login_history` correctly identified and flagged for monthly RANGE partitioning ahead of reaching problematic scale — partition key columns already exist as NOT NULL with defaults, so no schema change is needed later, only the partitioning migration itself.
- **Maintainability:** The consistent application of the "explain and justify every deviation from the Global Convention" discipline (no soft-delete on `refresh_tokens`/`login_history`/`audit_logs`, each individually justified rather than silently inconsistent) keeps the schema predictable for a future engineer — a deviation without a documented reason is the real maintainability risk, not the deviation itself.

---

*End of Module 3. Say "CONTINUE" for MODULE 4 — PROPERTIES (buildings, floors, apartments, parking_spots, parking_assignments, utility_meters, meter_readings).*

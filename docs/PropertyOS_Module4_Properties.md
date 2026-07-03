# MODULE 4 — PROPERTIES

Continuation of `PropertyOS_Phase3_Physical_Design.md`. Applies the same Global Conventions (UUIDv7 PKs, `created_at`/`updated_at`/`created_by`/`updated_by`, soft delete, `company_id` + RLS tenant isolation, `snake_case` naming) unless a deviation is explicitly justified, exactly as Modules 1–3 do.

Scale target for this module: **100,000+ buildings, 2,000,000+ apartments, 100,000,000+ meter readings.** Every design decision below is made against those numbers, not against a demo-scale dataset.

---

## 4.0 Module-Wide Design Decisions (read before the tables)

**Denormalized `company_id` and `building_id` on child tables.** Per Phase 1 §1.6 and the Phase 2 relationship catalog, `apartments.building_id` is denormalized from `floors.building_id` specifically to avoid a JOIN through `floors` on every apartment listing/search query — this is the single highest-traffic query path in the entire schema (2M+ rows, hit by every dashboard, every search, every lease creation). This module extends that same principle consistently: `company_id` is denormalized onto **every** table in this module (`floors`, `apartments`, `parking_spots`, `parking_assignments`, `utility_meters`, `meter_readings`), not just where Phase 1 originally called it out. Justification: every one of these tables is either RLS-protected directly or queried at high volume in isolation from its full parent chain (e.g., "all electricity meters for company X" should not require joining through `apartments → floors → buildings` just to filter by tenant). The cost is a few extra denormalized UUID columns kept in sync by the same INSERT that already knows the building's `company_id`; the benefit is that RLS policies and hot-path indexes on every table in this module can filter on tenant directly, with no join required. This is a deliberate, module-wide extension of an already-approved Phase 1 pattern, not a new architectural risk.

**Governorate modeled as an ENUM, not a reference table.** Jordan has exactly 12 governorates (محافظات) — a small, stable, effectively-closed set that changes on the timescale of national administrative reform, not product iteration. A `governorate_enum` is therefore the correct normalization choice here: it gives referential-integrity-like guarantees (no free-text typos, no "Amman" vs "amman" vs "AMMAN" drift) without the overhead of a lookup table + FK + join for a 12-value domain. District (لواء) and area/neighborhood (منطقة) are **not** enums — Jordan has dozens of districts and effectively unbounded neighborhood-level area names, and this data genuinely benefits from being free text validated at the application layer rather than a rigid table PropertyOS would need to maintain and keep current as urban areas develop. This is a deliberate middle ground: enum where the domain is small and stable, free text where it is not, rather than mechanically normalizing everything into lookup tables per a textbook-3NF instinct that would add real maintenance overhead for no integrity benefit at the district/area level.

**Cached/denormalized count and status columns.** `buildings.total_apartments_count`, `floors.apartments_count`, `apartments.occupancy_status`, and `utility_meters.last_reading_value`/`last_reading_at` are all denormalized, trigger-maintained caches, not sources of truth. This mirrors the exact pattern and rationale already established in Phase 1 §1.6 for `apartments.occupancy_status` ("a cached field... refreshed transactionally... explained further in Phase 7"). Each is called out per-table below with its specific maintenance trigger.

**Forward references to not-yet-detailed tables.** `parking_assignments.lease_contract_id`, `utility_meters`/`meter_readings`' eventual billing consumers, and `meter_readings.photo_file_id` reference `lease_contracts` (Module 5) and `file_storage` (Documents module) respectively. These FKs are declared now, consistent with the Phase 2 relationship catalog which already established these relationships conceptually — the tables they point to will be physically detailed in their own modules, but the FK exists at the schema level from this module's migration forward (Postgres has no issue with FK target tables being created in a later migration within the same initial deployment; for incremental Prisma migrations, the target tables must simply be created in the same or an earlier migration file).

---

## 4.1 Table: `buildings`

**Purpose:** The core physical asset record. A building belongs to exactly one company and is the root of the entire physical-property hierarchy (floors → apartments → everything else).

**Business Description:** Represents one physical residential, commercial, or mixed-use structure owned/managed by a company. Anchors address, structural metadata, and the denormalized apartment-count cache used by every company dashboard.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Owning company |
| name | VARCHAR(255) | NO | — | Building name/label, e.g. "برج الأمير" or "Al-Rasheed Tower" |
| internal_code | VARCHAR(50) | YES | NULL | Company's own internal reference code (many PM companies use codes like "BLD-014" in their existing spreadsheets/paper records during migration) |
| building_type | building_type_enum | NO | 'residential' | `residential` \| `commercial` \| `mixed_use` |
| total_floors | SMALLINT | NO | — | Declared/nominal floor count (from municipal building permit); informational, not required to equal `COUNT(floors)` — see Business Rules |
| construction_year | SMALLINT | YES | NULL | — |
| gps_latitude | NUMERIC(10,7) | YES | NULL | For Marketplace map view and maintenance dispatch |
| gps_longitude | NUMERIC(10,7) | YES | NULL | — |
| total_apartments_count | INTEGER | NO | 0 | Denormalized cache, trigger-maintained — see below |
| is_active | BOOLEAN | NO | true | Operationally active vs. decommissioned/sold/under full renovation |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT — per Phase 2 relationship catalog, buildings are never hard-deleted while any historical lease/payment exists; a company cannot be hard-deleted while it owns buildings either way.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_buildings_company_internal_code` on `(company_id, internal_code)` WHERE `internal_code IS NOT NULL AND deleted_at IS NULL` — partial: allows NULL codes (a company that never adopts internal codes) and allows a retired building's code to be reused.

**Check Constraints:**
- `chk_buildings_total_floors_positive` — `total_floors > 0`.
- `chk_buildings_construction_year_range` — `construction_year IS NULL OR (construction_year BETWEEN 1900 AND EXTRACT(YEAR FROM CURRENT_DATE)::SMALLINT + 5)` — upper bound of +5 years accommodates buildings entered into the system while still under construction, against a permit-declared future completion year.
- `chk_buildings_gps_pair` — `(gps_latitude IS NULL) = (gps_longitude IS NULL)` — both coordinates present or neither; a lat-without-long (or vice versa) row is a data-entry error the database should reject, not silently store.

**Indexes:**
- `idx_buildings_company_id` on `(company_id)` WHERE `deleted_at IS NULL` — the base "list this company's buildings" query, hit on essentially every building-list screen and every dashboard that needs to enumerate a company's portfolio before drilling into apartments/occupancy. **Why partial:** the ratio of soft-deleted to active buildings grows over a company's lifetime; excluding them keeps the index proportional to the live portfolio.
- `idx_buildings_company_active` composite on `(company_id, is_active)` WHERE `deleted_at IS NULL` — supports the common "active buildings only" dashboard filter as a single index scan rather than a filter-after-scan; `company_id` leads because it's the mandatory tenant-scoping equality predicate present in literally every query against this table, `is_active` second because it's the next-most-common equality filter layered on top.
- `idx_buildings_name_trgm` — GIN index using `pg_trgm` on `name` — supports the stated "Building search" requirement via `ILIKE '%query%'` / fuzzy substring search, which a plain B-tree cannot serve efficiently. **Why this over a B-tree prefix index:** users searching for a building by partial/misremembered name (very common — "the tower near the circle", "Rasheed something") need substring matching, not just prefix matching; trigram GIN is the standard PostgreSQL answer to this and is proportionate here because building search is a genuinely user-facing, frequently-hit feature, not a speculative one.

**Business Rules:** `total_floors` is a **declared** attribute captured from the building's permit/registration at data-entry time — it is deliberately **not** enforced to equal `COUNT(floors WHERE building_id = ...)`. Reason: floors are frequently added incrementally as a company onboards a building into PropertyOS (starting with occupied floors, backfilling vacant/under-construction ones later), and some floors (mechanical rooms, unused roof space) may never get a `floors` row at all despite counting toward the permit's declared floor count. Enforcing equality via a CHECK/trigger would reject legitimate, common onboarding states. `total_apartments_count` is maintained by an `AFTER INSERT OR UPDATE OF deleted_at ON apartments` trigger that increments/decrements the parent building's counter — this keeps the hottest dashboard query ("total units under management") a single indexed row read instead of a `COUNT(*)` aggregate across up to millions of `apartments` rows.

**Soft Delete Strategy:** Standard. A soft-deleted building's floors/apartments/leases/payments all remain intact and queryable for statutory retention, consistent with the company-level soft-delete philosophy in Module 1.

**Audit Requirements:** All mutations logged to `audit_logs`; `is_active` transitions and `company_id` (should it ever need reassignment via an admin data-correction flow) are treated as high-severity events.

**Future Scalability Notes:** At 100,000 buildings this table is still trivially sized (low hundreds of MB) — no partitioning need. The real scale pressure in this module is entirely in `apartments` and `meter_readings`, addressed below.

---

## 4.2 Table: `building_addresses`

**Purpose:** 1:1 extension of `buildings` holding the Jordan-specific address hierarchy, split out to keep `buildings` itself lean (mirroring the `companies`/`company_settings` split from Module 1) and because address data has a distinct query pattern (location-based search/filtering) from the core building record.

**Business Description:** Jordanian addressing is governorate → district (لواء) → area/neighborhood (منطقة) → landmark-oriented, not postal-code-oriented (postal codes exist but are barely used in practical navigation). This table captures that hierarchy plus GPS-adjacent descriptive fields needed for both internal use and (indirectly, via the `apartments → floors → buildings → building_addresses` join) Marketplace INTERNAL listing display.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| building_id | UUID | NO | — | Owning building (1:1) |
| company_id | UUID | NO | — | Denormalized from `buildings.company_id` — see §4.0 |
| governorate | governorate_enum | NO | — | One of Jordan's 12 محافظات |
| district | VARCHAR(150) | NO | — | لواء |
| area | VARCHAR(150) | YES | NULL | منطقة / neighborhood — nullable since some rural/newly-developed districts aren't yet subdivided into named areas in practice |
| street_name | VARCHAR(255) | YES | NULL | — |
| building_plate_number | VARCHAR(50) | YES | NULL | The national addressing building-plate number (الترقيم الوطني), where assigned — increasingly common in Amman but not universal |
| nearest_landmark | VARCHAR(255) | YES | NULL | Critical in the Jordanian market — people navigate and describe locations by landmark far more than by street address |
| postal_code | VARCHAR(20) | YES | NULL | Nullable — practically unused for navigation (Phase 1 note), kept only for the rare formal-correspondence use case |
| full_address_text | TEXT | YES | NULL | Free-form composed display string (convenience cache for UI/printing, e.g. on a lease PDF letterhead) — not a source of truth; derived from the structured fields above at write time |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:**
- `building_id` → `buildings(id)` ON DELETE CASCADE — an address has no meaning without its building.
- `company_id` → `companies(id)` ON DELETE RESTRICT.

**Unique Constraints:** `uq_building_addresses_building_id` on `(building_id)` — enforces 1:1 cardinality.

**Check Constraints:** None beyond enum-typed `governorate` — district/area free text is validated at the application layer (a curated but editable district list per governorate, not DB-enforced, since new districts occasionally get administratively created and a CHECK/enum would require a migration to add one, an unacceptable operational cost for what's fundamentally reference data that changes on a slower-than-annual but nonzero cadence).

**Indexes:**
- `idx_building_addresses_governorate_district` composite on `(company_id, governorate, district)` — supports the primary location-filtering use case: a company browsing/filtering its own portfolio by governorate and district (e.g., a management company with buildings across Amman and Zarqa filtering to just Amman/Jabal Amman). `company_id` leads for tenant-scoping; `governorate` next since it's the coarsest and most commonly-applied filter; `district` trailing as the finer-grained refinement layered on top — this ordering matches how the UI's location filter is actually used (governorate selected first, district optionally narrows further).
- `idx_building_addresses_area_trgm` — GIN trigram index on `area` — supports landmark/neighborhood substring search (e.g., typing "Swelieh" or "الصويفية" partially), the same search-quality rationale as `idx_buildings_name_trgm` above, needed here specifically because Jordanian users search by neighborhood name at least as often as by building name.

**Business Rules:** `full_address_text` is regenerated by the application (not a DB trigger, since composing a properly-formatted bilingual address string is presentation logic, not a database concern) whenever any of the structured address fields change. Auto-created in the same transaction as the parent `buildings` row.

**Soft Delete Strategy:** None — no independent lifecycle; hard-deleted only as a CASCADE consequence of its parent building's (rare, admin-only) hard delete, same pattern as `company_settings`.

**Audit Requirements:** Address changes logged to `audit_logs` — a building's registered address is legally/contractually referenced material (appears on lease documents), so corrections must be traceable.

**Future Scalability Notes:** Scales 1:1 with `buildings` (100,000 rows at target scale) — no partitioning concern. The two indexes above are the only ones needed for this table's stated query patterns; no additional index is justified without a concrete new query requirement.

---

## 4.3 Table: `floors`

**Purpose:** Decomposes a building into its constituent floors, giving utility-meter allocation, maintenance requests, and building-document scoping (Phase 1 §1.5) a floor-level anchor point.

**Business Description:** Captures both a numeric sort order (for correct display ordering across basement/ground/roof) and a human-facing label, since Jordanian buildings commonly use "أرضي، أول، تسوية، روف" rather than pure integers.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| building_id | UUID | NO | — | Owning building |
| company_id | UUID | NO | — | Denormalized — see §4.0 |
| floor_number | SMALLINT | NO | — | Signed sort-order key: negative for basement levels (-1, -2...), 0 for ground, positive for upper floors. Roof is assigned the next integer above the highest regular floor. This is a pure ordering key, not a display label. |
| floor_label | VARCHAR(50) | NO | — | Human-facing display label, e.g. "أرضي", "الطابق الأول", "روف", "تسوية 2" |
| floor_type | floor_type_enum | NO | 'regular' | `basement` \| `ground` \| `regular` \| `roof` — used to scope floor-specific compliance documents (elevator machine room, water tank inspection) to the roof, and safety egress logic to basements |
| apartments_count | INTEGER | NO | 0 | Denormalized cache, trigger-maintained (same pattern as `buildings.total_apartments_count`) |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `building_id` → `buildings(id)` ON DELETE RESTRICT — per Phase 2, hard-delete blocked while apartments exist on any floor; soft-delete cascades in practice at the application layer.
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_floors_building_floor_number` on `(building_id, floor_number)` WHERE `deleted_at IS NULL` — a building cannot have two floors sharing the same sort-order position; this is the structural guarantee behind "prevent orphan/duplicate floors" from the stated integrity requirements.

**Check Constraints:** None requiring `floor_type`/`floor_number` correlation — by convention `floor_type = 'ground'` should pair with `floor_number = 0`, but this is deliberately left as an application-layer convention rather than a CHECK constraint, since a small number of buildings (particularly older ones with irregular historical numbering) genuinely don't follow the convention, and rejecting those at the database level would block legitimate data entry for an edge case with no real integrity risk — a roof mislabeled as "regular" doesn't threaten data correctness the way a duplicate `floor_number` would.

**Indexes:**
- `idx_floors_building_id` on `(building_id)` WHERE `deleted_at IS NULL` — "list floors for building X, ordered by `floor_number`" — the core navigation query every time a building's detail page is opened. This single index serves both the filter (`building_id = $1`) and, combined with a `ORDER BY floor_number` in the query, avoids a separate sort step since results within the `building_id` bucket can be returned in `floor_number` order directly off the B-tree if the index is defined as `(building_id, floor_number)` rather than `building_id` alone — **implemented as the composite `(building_id, floor_number)`** for exactly this reason, turning "filter + sort" into a single ordered index scan.

**Deliberately omitted index:** a standalone `company_id` index on `floors` is **not** created. Floors are essentially never queried directly at the company-wide level (a company dashboard asks about buildings or apartments, not floors, as its unit of aggregation); when a company-scoped floor query is genuinely needed it would go through `building_id` via a join from `buildings`, which is already indexed on `company_id`. Adding an index here on a column with no standalone query pattern would violate the stated "avoid unnecessary indexes" requirement for a table already carrying `company_id` purely for RLS enforcement (RLS policy evaluation on this table's low row-count-per-building doesn't require a dedicated index the way `apartments`/`meter_readings` do).

**Business Rules:** `apartments_count` is maintained by the same `AFTER INSERT OR UPDATE OF deleted_at ON apartments` trigger that updates `buildings.total_apartments_count` — one trigger function, two counter updates in the same transaction, since an apartment always has exactly one floor and one building.

**Soft Delete Strategy:** Standard — soft-deleting a floor is blocked (application-layer check, RESTRICT-equivalent) while it has active apartments, mirroring the FK-RESTRICT-for-hard-delete behavior at the soft-delete layer where the DB constraint itself doesn't apply.

**Audit Requirements:** Standard `audit_logs` coverage.

**Future Scalability Notes:** Scales linearly with buildings (roughly 10–20 floors/building on average → ~1.5–2M rows at 100,000 buildings) — comfortably within single-table territory, no partitioning need.

---

## 4.4 Table: `apartments`

**Purpose:** The primary rentable unit and, per Phase 1 §1.6, the true center of gravity of the entire schema. Every lease, tenant relationship, meter, and maintenance request ultimately traces back to a row here.

**Business Description:** Captures unit identity (building/floor/number), physical characteristics, ownership model, and a cached occupancy status used for fast vacancy dashboards without an aggregate join across the lease table on every page load.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized from `floors`/`buildings` — see §4.0; avoids a two-level join on every apartment query |
| building_id | UUID | NO | — | Denormalized from `floors.building_id` — see §4.0 |
| floor_id | UUID | NO | — | — |
| unit_number | VARCHAR(20) | NO | — | As it appears on the unit's door, e.g. "301", "G-2" — free text to accommodate non-numeric schemes |
| ownership_status | ownership_status_enum | NO | 'company_owned' | `company_owned` \| `third_party_owned` — see below |
| external_owner_name | VARCHAR(255) | YES | NULL | Required when `ownership_status = 'third_party_owned'` — the individual/entity PropertyOS's company account manages this unit *on behalf of* (common investment/management-company model in Jordan) |
| external_owner_phone | VARCHAR(20) | YES | NULL | — |
| occupancy_status | occupancy_status_enum | NO | 'vacant' | `vacant` \| `occupied` \| `under_maintenance` \| `listed` — **cached/derived**, see Business Rules |
| area_sqm | NUMERIC(7,2) | NO | — | Square meters — the standard Jordanian unit of measure, never square feet |
| bedrooms | SMALLINT | NO | 0 | — |
| bathrooms | SMALLINT | NO | 0 | — |
| base_rent_amount | NUMERIC(12,3) | YES | NULL | Default/asking rent used when drafting a new lease; nullable for units not yet priced |
| base_rent_currency | CHAR(3) | NO | 'JOD' | ISO 4217 |
| is_active | BOOLEAN | NO | true | Unit taken permanently out of service (merged into a neighboring unit, converted to non-residential use) — distinct from `occupancy_status`, which describes a still-viable unit's current tenancy state |
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
- `floor_id` → `floors(id)` ON DELETE RESTRICT.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_apartments_building_unit_number` on `(building_id, unit_number)` WHERE `deleted_at IS NULL` — the direct, explicit implementation of the stated requirement "apartment numbers only need to be unique within the same building." Deliberately scoped to `building_id`, not `floor_id` and not `company_id` — a `unit_number` may legitimately repeat across different buildings of the same company (two buildings can each have a "101"), but never within one building regardless of which floor it's nominally attached to (prevents a data-entry error creating two "301" units on different floors of the same tower).

**Check Constraints:**
- `chk_apartments_area_positive` — `area_sqm > 0`.
- `chk_apartments_bedrooms_nonneg` — `bedrooms >= 0`.
- `chk_apartments_bathrooms_nonneg` — `bathrooms >= 0`.
- `chk_apartments_base_rent_positive` — `base_rent_amount IS NULL OR base_rent_amount > 0`.
- `chk_apartments_external_owner_required` — `(ownership_status = 'company_owned') OR (external_owner_name IS NOT NULL)` — a third-party-owned unit must record who the actual owner is; enforced at the DB level because this is a structural completeness rule (a management company legally needs this on file), not a soft UX nicety.

**Indexes:**
- `idx_apartments_building_id` composite on `(building_id, floor_id)` WHERE `deleted_at IS NULL` — the building detail page's primary query: "all apartments in building X, grouped/ordered by floor." `building_id` leads as the mandatory equality filter for this screen; `floor_id` trails so apartments naturally group by floor without a separate sort/group step server-side.
- `idx_apartments_company_occupancy` composite on `(company_id, occupancy_status)` WHERE `deleted_at IS NULL` — the single most-hit index in this module: every company-wide occupancy dashboard, every "list vacant units" screen, and every KPI tile ("X% occupancy this month") filters exactly this shape. `company_id` leads (mandatory tenant scope, present in every query), `occupancy_status` second as the near-universal secondary equality filter (dashboards almost always ask for one specific status at a time, most commonly `vacant`).
- `idx_apartments_company_occupancy_bedrooms` composite on `(company_id, occupancy_status, bedrooms)` WHERE `deleted_at IS NULL AND occupancy_status = 'vacant'` — supports the stated "Apartment search" requirement's single most common concrete shape: "show me vacant units with N bedrooms" (both the internal leasing-agent search UI and, indirectly, Marketplace INTERNAL-listing browsing). Made partial on `occupancy_status = 'vacant'` specifically — not just `deleted_at IS NULL` — because searchable/available-unit search is *only ever* performed against vacant units; indexing occupied/under-maintenance rows for this exact query shape would be pure dead weight, since a search UI never asks "show me occupied units with 2 bedrooms." This is a deliberately narrow, single-purpose partial index earning its keep against a very specific, very frequent query.
- `idx_apartments_floor_id` on `(floor_id)` WHERE `deleted_at IS NULL` — supports the floor-detail-page "apartments on this floor" query independent of the building-scoped composite above (a floor can be looked up directly from a maintenance-request or utility-meter context without the caller already knowing/filtering by `building_id` first).

**Business Rules:** `occupancy_status` is **not** a source of truth — per Phase 1 §1.6, the authoritative state is "does an *active* `lease_contracts` row exist for this apartment right now," and this column is a denormalized cache refreshed transactionally whenever a lease is created, activated, or terminated (an `AFTER INSERT OR UPDATE OF status ON lease_contracts` trigger, defined in Module 5, writes back to this column). This tradeoff — cached status with a trigger-enforced consistency mechanism rather than a live aggregate query on every dashboard load — is what makes the `idx_apartments_company_occupancy*` indexes above possible at all; a live "is there an active lease" subquery per apartment on every dashboard render would not scale at 2M+ apartments. `total_apartments_count`/`apartments_count` on the parent `buildings`/`floors` rows are maintained by this table's own insert/soft-delete trigger, described in §4.1/§4.3.

**Soft Delete Strategy:** Standard. A soft-deleted apartment's full lease/payment/maintenance history remains intact; `total_apartments_count`/`apartments_count` on the parent building/floor are decremented accordingly by the maintenance trigger.

**Audit Requirements:** All mutations audit-logged; `ownership_status` and `base_rent_amount` changes are financially/legally significant and always captured with before/after values in `previous_values`/`new_values`.

**Future Scalability Notes:** **Explicitly named in the scale target (2,000,000+ rows).** This table stays comfortably as a single unpartitioned table at that scale — 2M rows with a handful of small fixed-width columns plus a few `VARCHAR`s is on the order of a few GB, well within what PostgreSQL handles natively with proper indexing (no TOAST-heavy columns, no unbounded text). **Partitioning is not recommended for `apartments`** despite its row count, because unlike `audit_logs`/`login_history`/`meter_readings`, this table's queries are not naturally time-range-scoped — apartments don't have a meaningful "age out" dimension the way logs and readings do, so there's no clean partition key that would actually reduce the working set for the table's real query patterns (occupancy/search queries scan by `company_id`, not by a time range). The composite indexes above, keyed on the actual filter columns, are the correct scaling mechanism here, not partitioning.

---

## 4.5 Table: `parking_spots`

**Purpose:** A separately allocatable building asset, modeled independently from `apartments` per Phase 1 §1.7, since parking in the Jordanian market is frequently negotiated, assigned, and re-assigned independently of a specific unit.

**Business Description:** Represents one physical parking spot within a building's inventory, with an optional default/owned pairing to a specific apartment that can be overridden per-lease via `parking_assignments`.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized — see §4.0 |
| building_id | UUID | NO | — | — |
| default_apartment_id | UUID | YES | NULL | Optional default/owned pairing — nullable since not every spot has a fixed apartment association (visitor spots, spots owned independently of any unit) |
| spot_code | VARCHAR(20) | NO | — | e.g. "P-12", "B1-05" |
| parking_type | parking_type_enum | NO | 'standard' | `standard` \| `covered` \| `visitor` \| `disabled_access` |
| location_description | VARCHAR(255) | YES | NULL | e.g. "Basement Level 1, Row A" |
| is_active | BOOLEAN | NO | true | Spot decommissioned/repurposed (e.g., converted to storage) |
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
- `default_apartment_id` → `apartments(id)` ON DELETE SET NULL — per Phase 2, unassigning preserves the spot as building-level inventory rather than deleting it.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_parking_spots_building_spot_code` on `(building_id, spot_code)` WHERE `deleted_at IS NULL` — same per-building-scoped-uniqueness pattern as apartment unit numbers, for the same reason (a spot code is a physical building label, not a globally unique identifier).

**Check Constraints:** None beyond enum typing — `parking_type` fully captures the domain's variability without needing range/format checks.

**Indexes:**
- `idx_parking_spots_building_id` on `(building_id)` WHERE `deleted_at IS NULL` — building parking-inventory listing, the standard "manage this building's parking" screen.
- `idx_parking_spots_default_apartment_id` on `(default_apartment_id)` WHERE `default_apartment_id IS NOT NULL AND deleted_at IS NULL` — "which spot(s) does apartment X own by default" on the apartment detail page; partial because most spots may not carry a default apartment pairing at all (visitor/pool spots), and rows without one are irrelevant to this specific lookup.
- `idx_parking_spots_company_active` composite on `(company_id, is_active)` WHERE `deleted_at IS NULL` — supports the company-wide "available spots to assign" admin workflow, which needs active spots across the whole portfolio, not just one building, when handling a cross-building parking request.

**Business Rules:** `default_apartment_id` is a default/informational pairing only — the actual, currently-in-effect assignment for lease purposes is always read from `parking_assignments`, never assumed from this column, since a lease can override the default.

**Soft Delete Strategy:** Standard.

**Audit Requirements:** Standard `audit_logs` coverage.

**Future Scalability Notes:** Scales sub-linearly with apartments (not every unit has a dedicated spot, and some spots are shared/visitor-designated) — low hundreds of thousands of rows at full target scale, no partitioning need.

---

## 4.6 Table: `parking_assignments`

**Purpose:** The M:M junction between `parking_spots` and `lease_contracts`, carrying its own business-meaningful attributes (assignment date range) per the Phase 2 principle that every real M:M relationship in this schema is modeled as a first-class entity, not a bare bridge table.

**Business Description:** Records which lease currently has (or historically had) a given parking spot, allowing a per-lease override of the spot's `default_apartment_id` pairing — e.g., a tenant negotiates a second spot, or a spot's default-owning apartment's tenant doesn't actually use it and it's reassigned elsewhere for the term of another lease.

**Note on forward reference:** `lease_contract_id` references `lease_contracts`, physically detailed in **Module 5**. This relationship is already established in the Phase 2 relationship catalog (§2.1.B) and Mermaid ERD; the FK is declared here as part of this module's migration and will resolve once Module 5's `lease_contracts` table is created (same or earlier migration in the deployment sequence).

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized — see §4.0 |
| parking_spot_id | UUID | NO | — | — |
| lease_contract_id | UUID | NO | — | Forward reference to Module 5 |
| assigned_from | DATE | NO | — | — |
| assigned_to | DATE | YES | NULL | NULL = open-ended, tied to the lease's own end (closed out when the lease ends/terminates) |
| status | parking_assignment_status_enum | NO | 'active' | `active` \| `ended` |
| created_at | TIMESTAMPTZ | NO | now() | — |
| updated_at | TIMESTAMPTZ | NO | now() | — |
| created_by | UUID | YES | NULL | — |
| updated_by | UUID | YES | NULL | — |
| deleted_at | TIMESTAMPTZ | YES | NULL | Soft delete |
| deleted_by | UUID | YES | NULL | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `parking_spot_id` → `parking_spots(id)` ON DELETE RESTRICT.
- `lease_contract_id` → `lease_contracts(id)` ON DELETE RESTRICT (forward reference — Module 5).
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints:** `uq_parking_assignments_active_spot` — **partial unique index** on `(parking_spot_id)` WHERE `status = 'active' AND deleted_at IS NULL` — a spot can be actively assigned to at most one lease at a time. This is the exact same structural pattern already used for "exactly one active lease per apartment" (Phase 1 §1.13) and "exactly one active subscription per company" (Module 2) — the recurring "at most one active row per resource" shape in this schema, solved identically each time via a partial unique index rather than a trigger, since a partial unique index is enforced atomically by the database with no race-condition window that an application-level check-then-insert would have.

**Check Constraints:** `chk_parking_assignments_dates` — `assigned_to IS NULL OR assigned_to > assigned_from`.

**Indexes:**
- `idx_parking_assignments_lease_contract_id` on `(lease_contract_id)` WHERE `deleted_at IS NULL` — "what parking spots does this lease include" on the lease detail page — a lease can have multiple spots, so this is a one-to-many lookup from the lease side.
- `idx_parking_assignments_parking_spot_id` on `(parking_spot_id)` WHERE `deleted_at IS NULL` — assignment history for a given spot ("who has had this spot over time"); the partial unique index above already covers the *current active* assignment lookup efficiently as a leftmost-prefix match, but this broader index is still needed because it must also find *past* (`status = 'ended'`) rows for the same spot, which the `status = 'active'`-scoped partial unique index cannot serve.

**Business Rules:** When a lease is terminated or expires, the application transitions any `active` `parking_assignments` rows for that lease to `status = 'ended'` and sets `assigned_to = <termination date>` in the same transaction — this is application-orchestrated (not a DB trigger) because it's part of the broader lease-termination business transaction defined in Module 5, not an isolated parking-table concern.

**Soft Delete Strategy:** Standard — retained for assignment-history/dispute-resolution purposes (per Phase 1 §1.7's parking-dispute rationale), not hard-deleted on ordinary reassignment (a reassignment is: end the current row via `status = 'ended'`, create a new row for the new lease).

**Audit Requirements:** Standard `audit_logs` coverage.

**Future Scalability Notes:** Bounded by (spots × average reassignment frequency) — well under apartment-table scale even at 100,000 buildings' worth of parking inventory; no partitioning need.

---

## 4.7 Table: `utility_meters`

**Purpose:** Represents a physical electricity or water meter, scoped to a building, floor, or apartment per the Jordanian market's mixed metering reality (Phase 1 §1.8).

**Business Description:** Not all buildings have per-apartment metering — many older buildings share a single building- or floor-level meter with proportional allocation. This table models all three scopes uniformly via a `meter_scope` enum plus scope-appropriate nullable FKs, with an `allocation_method` required specifically for shared (non-apartment-scoped) meters.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized — see §4.0 |
| building_id | UUID | NO | — | Always populated regardless of scope — every meter belongs to a building at minimum, and denormalizing this avoids resolving building context through `floor_id`/`apartment_id` on every meter query |
| floor_id | UUID | YES | NULL | Populated only when `meter_scope = 'floor'` |
| apartment_id | UUID | YES | NULL | Populated only when `meter_scope = 'apartment'` |
| meter_scope | meter_scope_enum | NO | — | `building` \| `floor` \| `apartment` |
| meter_type | meter_type_enum | NO | — | `electricity` \| `water` |
| meter_number | VARCHAR(50) | NO | — | Physical serial/registration number issued by the utility authority (JEPCO/EDCO/IDECO for electricity, WAJ/Miyahuna for water) |
| provider_name | VARCHAR(100) | YES | NULL | e.g. "JEPCO", "Miyahuna"; nullable for internal/unregistered sub-meters used purely for internal cost allocation |
| allocation_method | allocation_method_enum | YES | NULL | `equal_split` \| `area_weighted` \| `occupancy_weighted`; required when `meter_scope IN ('building','floor')`, must be NULL for `apartment`-scoped meters |
| installation_date | DATE | YES | NULL | — |
| status | meter_status_enum | NO | 'active' | `active` \| `inactive` \| `decommissioned` \| `faulty` |
| last_reading_value | NUMERIC(12,3) | YES | NULL | Denormalized cache of the most recent reading — see Business Rules |
| last_reading_at | TIMESTAMPTZ | YES | NULL | Denormalized cache |
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
- `floor_id` → `floors(id)` ON DELETE RESTRICT.
- `apartment_id` → `apartments(id)` ON DELETE RESTRICT.
- `created_by`, `updated_by`, `deleted_by` → `users(id)` ON DELETE SET NULL.

**Unique Constraints — two, deliberately split:**
- `uq_utility_meters_meter_number_authority` on `(meter_number)` WHERE `deleted_at IS NULL AND provider_name IS NOT NULL` — **global** uniqueness for authority-registered meters. Justification: a JEPCO/Miyahuna-issued meter number is a real-world globally unique physical identifier, not a company- or building-scoped label the way `unit_number`/`spot_code` are — two different PropertyOS companies could never legitimately have the same authority-issued meter number, so global uniqueness correctly reflects physical reality and catches a real class of data-entry error (transposed digits colliding with another company's genuine meter would otherwise go undetected).
- `uq_utility_meters_meter_number_internal` on `(company_id, meter_number)` WHERE `deleted_at IS NULL AND provider_name IS NULL` — **company-scoped** uniqueness for internal/unregistered sub-meters, since these numbers are company-invented labels (e.g., a company's own "SUB-04" internal sub-meter code) with no external authority guaranteeing global uniqueness, and two unrelated companies could reasonably invent the same internal label independently.

**Check Constraints:**
- `chk_utility_meters_scope_fk_consistency` — `(meter_scope = 'building' AND floor_id IS NULL AND apartment_id IS NULL) OR (meter_scope = 'floor' AND floor_id IS NOT NULL AND apartment_id IS NULL) OR (meter_scope = 'apartment' AND apartment_id IS NOT NULL AND floor_id IS NULL)` — the structural implementation of "prevent invalid meter assignments": a meter's populated scope-FK must exactly match its declared `meter_scope`, with no ambiguous or dangling combination representable.
- `chk_utility_meters_allocation_method_consistency` — `(meter_scope IN ('building','floor') AND allocation_method IS NOT NULL) OR (meter_scope = 'apartment' AND allocation_method IS NULL)` — a shared meter must declare how its readings get split across tenants; a direct per-apartment meter has nothing to allocate.

**Indexes:**
- `idx_utility_meters_apartment_id` on `(apartment_id)` WHERE `apartment_id IS NOT NULL AND deleted_at IS NULL` — "meters for this apartment," the apartment detail page's utility tab — a hot, frequently-hit lookup.
- `idx_utility_meters_building_id` on `(building_id)` WHERE `deleted_at IS NULL` — building-wide utility-management screen listing every meter regardless of scope (needed since building-scoped meters have no `apartment_id`/`floor_id` to otherwise find them by).
- `idx_utility_meters_floor_id` on `(floor_id)` WHERE `floor_id IS NOT NULL AND deleted_at IS NULL` — floor-scoped meter lookup.
- `idx_utility_meters_company_type_status` composite on `(company_id, meter_type, status)` WHERE `deleted_at IS NULL` — supports the stated "utility meter lookup" requirement at the company-wide operational level (e.g., "list all active electricity meters across the portfolio" for a maintenance/reading-collection scheduling run). `company_id` leads for tenant scope, `meter_type` next since electricity/water are managed as operationally distinct workflows (different authorities, different reading schedules), `status` trailing as the near-universal "active only" filter on top.

**Business Rules:** `last_reading_value`/`last_reading_at` are maintained by an `AFTER INSERT ON meter_readings` trigger that updates the parent meter row — safe and simple specifically *because* `meter_readings` is append-only (no updates/deletes to reconcile against), so "the last reading" is unambiguously "the most recent insert for this meter," refreshed in the same transaction as the reading itself. This cache exists purely so that "current reading" — the single most commonly displayed utility fact on any apartment/building/meter screen — never requires an `ORDER BY reading_date DESC LIMIT 1` query against a table that will hold 100M+ rows.

**Soft Delete Strategy:** Standard — a soft-deleted (decommissioned) meter's full `meter_readings` history remains intact, since readings are immutable billing evidence per Phase 1 §1.8 and Phase 2's relationship catalog (`utility_meters → meter_readings`: RESTRICT, "readings preserved even if meter decommissioned").

**Audit Requirements:** Standard `audit_logs` coverage; `status` transitions to `faulty`/`decommissioned` are operationally significant and drive maintenance-dispatch workflows (Module 6).

**Future Scalability Notes:** Scales with (apartments + shared meters), comfortably in the low millions at full target scale (2M apartments, most single-electricity-metered, many single-water-metered, plus building/floor-level shared meters) — no partitioning need for this table itself; the real volume pressure from meters is entirely in its child `meter_readings` table below.

---

## 4.8 Table: `meter_readings`

**Purpose:** Immutable time-series readings feeding utility bill generation (Module 7).

**Business Description:** Every entry is a permanent, append-only historical fact — per the explicit requirement "meter readings must be immutable, never update historical readings," this table follows the same architectural pattern already established for `login_history` in Module 3: a pure event log, not a mutable business record.

**Deliberate deviation from the Global Convention (documented per the established house style):** this table has **no** `updated_at`, `updated_by`, `deleted_at`, or `deleted_by` columns. Applying the standard mutable-row audit/soft-delete pattern to a table whose entire business meaning is "this fact, once recorded, is never altered" would be actively wrong — it would imply a row *could* legitimately be updated or soft-deleted, which contradicts the stated business rule. A data-entry correction is handled by inserting a **new** reading row with a note, never by touching the erroneous one — exactly mirroring how `lease_contracts` handles renewals (new row, not a mutation) and consistent with `login_history`'s "append-only, no UPDATEs ever" rule in Module 3.

**Columns:**

| Column | Type | Nullable | Default | Description |
|---|---|---|---|---|
| id | UUID | NO | uuid_generate_v7() | Primary key |
| company_id | UUID | NO | — | Denormalized — see §4.0; essential here specifically because this table will be partitioned (see Scalability), and RLS + partition-pruning both benefit from `company_id` being directly present rather than requiring a join through `utility_meters` |
| utility_meter_id | UUID | NO | — | — |
| reading_value | NUMERIC(12,3) | NO | — | Cumulative meter dial value (odometer-style), not a delta — deltas for billing are computed at query/billing-job time via `LAG()` over `(utility_meter_id, reading_date)`, deliberately **not** stored as a precomputed/chained delta, since a stored delta would require a fragile "previous reading" pointer that backfilled or corrected out-of-order entries could invalidate; computing it on read is simple, always-correct, and cheap given the composite index below |
| reading_date | DATE | NO | — | The date the reading represents |
| entry_source | entry_source_enum | NO | 'manual' | `manual` \| `staff` \| `api` — per Phase 1 §1.8, supports today's manual/staff workflow and tomorrow's smart-meter API ingestion without a schema change |
| entered_by | UUID | YES | NULL | FK to `users`; nullable for `api`-sourced readings with no human actor |
| photo_file_id | UUID | YES | NULL | FK to `file_storage` (Documents module, forward reference) — photo evidence of the physical meter dial, a real dispute-prevention practice |
| notes | TEXT | YES | NULL | — |
| created_at | TIMESTAMPTZ | NO | now() | — |

**Primary Key:** `id`

**Foreign Keys:**
- `company_id` → `companies(id)` ON DELETE RESTRICT.
- `utility_meter_id` → `utility_meters(id)` ON DELETE RESTRICT — per Phase 2, readings are preserved even if the meter is later decommissioned; a reading can never be silently orphaned.
- `entered_by` → `users(id)` ON DELETE SET NULL.
- `photo_file_id` → `file_storage(id)` ON DELETE SET NULL (forward reference — Documents module).

**Unique Constraints:** None. Two readings for the same meter on the same date are **not** rejected — this is a deliberate choice, not an oversight: a same-day correction is legitimately recorded as a second row with a later `created_at` and an explanatory `notes` value, never as an UPDATE to the first (per the immutability rule above). Deduplication of genuinely accidental double-entries is an application-layer / billing-job concern (the billing job always takes the most-recently-`created_at` reading for a given `reading_date` if duplicates exist), not a database-integrity one — mirroring the exact reasoning already given for why `audit_logs` doesn't deduplicate repeated identical events (Module 3 §3.12).

**Check Constraints:**
- `chk_meter_readings_value_nonneg` — `reading_value >= 0`.
- `chk_meter_readings_date_not_future` — `reading_date <= CURRENT_DATE` — a reading cannot be logged for a date that hasn't happened yet.

**Indexes:**
- `idx_meter_readings_meter_date` composite on `(utility_meter_id, reading_date DESC)` — **the** core query of this entire table: "reading history for meter X, most recent first" (meter detail screen) and "latest reading for meter X as of a billing cutoff date" (the billing job). `utility_meter_id` leads as the mandatory equality filter; `reading_date DESC` trails so the single most common access pattern — "give me the latest reading" — is a direct index-ordered `LIMIT 1` scan, and so the billing job's `LAG()`-based delta computation over a bounded date range for one meter never needs a sort step.
- `idx_meter_readings_company_date` composite on `(company_id, reading_date DESC)` — supports company-wide operational queries: "readings entered this month across the portfolio" (reconciliation/QA screen), "readings missing this cycle" (an anti-join against `utility_meters` filtered by this index's date range). Given the partitioning strategy below, this index exists *per partition* and stays small and fast indefinitely, since each partition only ever holds one month's rows.

**Deliberately omitted index:** no index on `entry_source`. Filtering "manual vs. api-sourced readings" is a low-frequency reconciliation query, not a hot path, and — critically — once the table is partitioned monthly (below), any single partition is small enough that a sequential scan filtered by `entry_source` within one month's partition is fast without an index at all; adding one would be pure write-amplification for a query pattern that doesn't need it, precisely the "avoid unnecessary indexes" discipline applied consistently across every module so far.

**Business Rules:** Application code must never `UPDATE` or hard-`DELETE` a row here (enforced the same way `audit_logs` is protected in Module 3 — `REVOKE UPDATE, DELETE` at the database-role level is the recommended production hardening, detailed fully in Phase 8). Corrections are always additive new rows.

**Soft Delete Strategy:** Not applicable — see the deviation note at the top of this table's section.

**Audit Requirements:** Not separately mirrored into `audit_logs` for every row (pure duplication of an already-immutable, already-timestamped, already-actor-attributed log) — consistent with how `login_history` is treated in Module 3. Only exceptional events (e.g., a reading manually flagged as anomalous/disputed by staff) would generate a corresponding `audit_logs` entry cross-referencing this table.

**Future Scalability Notes:** **Explicitly named in the scale target (100,000,000+ rows).** This is unambiguously a **first-class RANGE partitioning candidate**, exactly the same reasoning already applied to `audit_logs`/`login_history` in Module 3 §3.11: append-only, time-ordered, every realistic query (billing, dashboards, reconciliation) is naturally bounded to a specific meter over a bounded recent date range or a specific company over a recent month, and old partitions can eventually be moved to cheaper storage or dropped wholesale once past the utility-billing dispute window, rather than requiring a slow `DELETE ... WHERE reading_date < ...` that would otherwise have to scan and remove tens of millions of rows with full index maintenance.

**Recommendation: RANGE partition `meter_readings` by `reading_date`, monthly**, identical mechanism to `audit_logs`/`login_history`. At 100M rows and (conservatively) 2M meters read roughly monthly, each monthly partition holds on the order of 2–4M rows — keeps per-partition indexes small, VACUUM cycles fast, and INSERT throughput (the dominant operation on this table by a wide margin — readings are written far more than they're ever individually read) stable as history accumulates. `reading_date` already exists as a `NOT NULL` column with no default dependency, so no schema addition is needed to introduce partitioning later — only the table-rebuild migration itself, flagged here so it's planned for rather than discovered as a surprise at scale, exactly per the standing convention from Module 3 §3.11.

---

## 4.9 Database Integrity Summary

| Risk | Prevention mechanism |
|---|---|
| Duplicate apartment numbers inside the same building | `uq_apartments_building_unit_number` on `(building_id, unit_number)` |
| Duplicate meter numbers | Split global/company-scoped partial unique constraints on `utility_meters.meter_number`, matching the real-world global-vs-internal distinction |
| Duplicate floor sort position within a building | `uq_floors_building_floor_number` on `(building_id, floor_number)` |
| Duplicate parking spot codes within a building | `uq_parking_spots_building_spot_code` on `(building_id, spot_code)` |
| Orphan floors (dangling `building_id`) | `floors.building_id` is `NOT NULL` with a real FK, RESTRICT on hard delete |
| Orphan apartments (dangling `building_id`/`floor_id`) | Both FKs `NOT NULL`, real FK constraints, RESTRICT on hard delete |
| Invalid meter scope/FK combinations (e.g., `meter_scope='apartment'` with `apartment_id NULL`) | `chk_utility_meters_scope_fk_consistency` |
| Invalid/missing allocation method on shared meters | `chk_utility_meters_allocation_method_consistency` |
| Two leases simultaneously claiming the same parking spot | `uq_parking_assignments_active_spot` partial unique on `(parking_spot_id) WHERE status='active'` |
| Third-party-owned apartment with no recorded owner | `chk_apartments_external_owner_required` |
| Future-dated meter readings | `chk_meter_readings_date_not_future` |
| Negative physical measurements (area, reading values) | `chk_apartments_area_positive`, `chk_meter_readings_value_nonneg` |

---

## 4.10 Scalability at Target Scale

| Scale point | Impact |
|---|---|
| 100,000 buildings | Trivial — `buildings`, `building_addresses` stay in the low hundreds-of-MB range. No structural change needed. |
| ~1.5–2,000,000 floors | Comfortably single-table; `idx_floors_building_id` composite keeps per-building floor listing fast indefinitely. |
| 2,000,000 apartments | The table stays unpartitioned by design (see §4.4 — no natural time-range partition key for this table's actual query patterns); the composite indexes on `(company_id, occupancy_status[, bedrooms])` and `(building_id, floor_id)` are the scaling mechanism, keeping every stated dashboard/search query a targeted index scan rather than a sequential scan across 2M rows. |
| Low millions of `utility_meters` rows | Comfortably single-table; indexed identically to `apartments` on its own hot-path columns. |
| 100,000,000 `meter_readings` rows | **Requires monthly RANGE partitioning by `reading_date`** — the single highest-volume table in this module by a wide margin, and the only one in Module 4 that needs it, for the same reasons already established for `audit_logs`/`login_history` in Module 3. |

**Should any other table in this module be partitioned?** No. `parking_assignments` and `utility_meters` both accumulate history but at volumes (low millions at most, bounded by physical spot/meter counts rather than an ever-repeating time series) that stay comfortably within single-table territory through the full stated target scale — partitioning them would add operational complexity (partition management, cross-partition query planning) with no corresponding performance benefit, which would violate the same "don't over-engineer for a scale concern that doesn't exist" discipline already applied when `refresh_tokens` was explicitly *not* partitioned in Module 3.

---

## Module 4 (Properties) — Architecture Review

- **Security:** Every table in this module carries `company_id` (directly, denormalized per §4.0) and is RLS-ready under the standard `USING (company_id = current_setting('app.current_company_id')::uuid)` policy, detailed fully in Phase 8. No table in this module needs a non-standard RLS policy shape (unlike `refresh_tokens`/`login_history` in Module 3) — every row in every table here belongs to exactly one company with no cross-tenant legitimate-access case, since the Marketplace's cross-tenant read path operates on `marketplace_listings` (a different module), not directly on `apartments`/`buildings`.
- **PostgreSQL Best Practices:** UUIDv7 PKs, `TIMESTAMPTZ` throughout, `NUMERIC` for all physical/financial measurements (never `FLOAT`/`REAL`, which would introduce rounding error into area measurements and rent amounts), partial indexes used consistently to keep index size proportional to *live/relevant* rows rather than cumulative history — the same discipline as Modules 1–3, applied without exception in this module.
- **Index Quality:** Every composite index in this module was checked for column-order correctness against its actual target query (equality-before-range/sort, sort direction embedded where a query needs ordering) — no index found with the wrong leading column. Two indexes were deliberately **not** created and the omission explicitly justified rather than silently skipped: a standalone `company_id` index on `floors` (§4.3) and an `entry_source` index on `meter_readings` (§4.8) — both real candidates a less careful pass might have added reflexively, both correctly identified as adding write cost without a corresponding read benefit given this module's actual stated query patterns.
- **Constraint Quality:** The apartment-numbering, meter-numbering, floor-numbering, and parking-spot-numbering uniqueness rules all follow a consistent, deliberately-chosen scoping logic (building-scoped for physical on-site labels vs. global/company-scoped for authority-issued or company-invented identifiers respectively) — each scoping choice is justified against what the identifier actually represents in the real world, not applied mechanically the same way everywhere.
- **Query Cost:** The `apartments` table — this module's highest-traffic table by query frequency — has its indexing built directly around the four query shapes explicitly named in the brief (building drill-down, company-wide occupancy dashboard, bedroom-filtered vacancy search, floor-scoped lookup), with the narrowest, most surgical partial index (`idx_apartments_company_occupancy_bedrooms`, scoped to `occupancy_status='vacant'` only) reserved for the single highest-value, highest-frequency search pattern rather than a generic broad index that would serve it less precisely.
- **Multi-Tenant Isolation:** Confirmed — see Security above. The module-wide decision to denormalize `company_id` onto every table (§4.0), extending Phase 1's original `apartments`-only denormalization to the whole module, was made specifically so that RLS policy evaluation and tenant-scoped indexing never require a multi-level join through the physical hierarchy on any table in this module.
- **Normalization (3NF):** The one deliberate, explicitly-justified departure from mechanical 3NF is `governorate` as an enum rather than a reference table (§4.0) — evaluated and confirmed as the right call given the domain's genuine stability (12 fixed values), while `district`/`area` were correctly left as free text rather than force-normalized into a lookup table that would add maintenance burden without a real integrity payoff. All cached/denormalized count and status columns (`total_apartments_count`, `apartments_count`, `occupancy_status`, `last_reading_value`/`last_reading_at`) are explicitly flagged as derived, trigger-maintained caches, not silent 3NF violations — each has a named, single-purpose maintenance trigger and a stated performance justification, consistent with how `users.last_login_at` was already handled the same way in Module 3.
- **Future Scalability:** `meter_readings` correctly identified and flagged for monthly RANGE partitioning ahead of reaching problematic scale, with its partition key (`reading_date`) already present as `NOT NULL` — no later schema change needed, only the partitioning migration itself. `apartments`, despite its large row count, was deliberately evaluated *against* partitioning and found not to need it, with the reasoning made explicit rather than defaulting to "large table therefore partition it" — the correct engineering call is query-pattern-driven, not row-count-driven alone.
- **Maintainability:** Every deviation from the Global Convention in this module (`meter_readings`' immutability, the split global/company-scoped meter-number uniqueness, the enum-vs-lookup-table call on governorate) is documented in place with its specific reasoning, keeping the "no undocumented deviation" discipline established in Module 3 consistent through this module as well.

---

*End of Module 4. Say "CONTINUE" for MODULE 5 — LEASING (tenants, tenant_family_members, tenant_emergency_contacts, tenant_vehicles, lease_contracts, contract_terminations, rent_payments, cheque_details).*

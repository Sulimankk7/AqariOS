using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module1_Companies_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===================================================================
            // MODULE 1 — CORE: Companies Foundation
            // Physical Design §1.1 (companies) and §1.2 (company_settings)
            //
            // MIGRATION STRUCTURE:
            //   1. PostgreSQL extensions
            //   2. UUID v7 function (PL/pgSQL — no C extension required)
            //   3. Shared updated_at trigger function
            //   4. PostgreSQL enum types
            //   5. companies table (with all approved constraints)
            //   6. company_settings table (with all approved constraints)
            //   7. Approved indexes (partial and regular)
            //   8. Per-table updated_at triggers
            //   9. Row-Level Security policies
            //      (stub only for Module 1 — companies is the tenant root;
            //       full RLS policy enforcement is wired in Phase 8)
            //
            // NOTE ON PARTIAL INDEXES:
            //   EF Core cannot express WHERE clauses on indexes via its fluent API.
            //   The partial indexes below are created via raw SQL in this migration.
            //   The EF Core-generated index DDL for these indexes is dropped and
            //   replaced with the correct partial index SQL.
            // ===================================================================

            // -------------------------------------------------------------------
            // 1. PostgreSQL extensions
            // -------------------------------------------------------------------
            // citext is needed later (users.email); created here so it's available
            // in the same migration if a future re-run happens on a fresh DB.
            // pg_trgm enables the GIN trigram indexes approved in later modules.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // -------------------------------------------------------------------
            // 2. UUID v7 function
            //
            // Pure PL/pgSQL — zero C-extension dependency.
            // PostgreSQL 17 does not ship a native uuidv7() (that's PG18+).
            // This implementation is bit-compatible with the RFC 9562 draft:
            //   • Bits 0–47:  48-bit Unix millisecond timestamp
            //   • Bits 48–51: version nibble = 0x7
            //   • Bits 52–63: random
            //   • Bit 64–65:  variant = 0b10
            //   • Bits 66–127: random
            // When PG18+ is adopted, this function's DEFAULT can be replaced with
            // the built-in uuidv7() — no data migration required (same wire format).
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION uuid_generate_v7()
RETURNS UUID
LANGUAGE plpgsql
PARALLEL SAFE
AS $$
DECLARE
    v_time  BIGINT;
    v_unix  BIGINT;
    v_b     BIT(128);
    v_uuid  TEXT;
BEGIN
    -- Unix timestamp in milliseconds (48-bit)
    v_unix  := (EXTRACT(EPOCH FROM clock_timestamp()) * 1000)::BIGINT;

    -- Build the 128-bit UUID value
    -- Bits 0-47  : timestamp_ms
    -- Bits 48-51 : version = 7
    -- Bits 52-63 : random
    -- Bits 64-65 : variant = 10
    -- Bits 66-127: random
    v_b := v_unix::BIT(48)                           -- 48 timestamp bits
        || B'0111'                                   -- version nibble = 7
        || (trunc(random() * 4096)::INT)::BIT(12)    -- 12 random bits
        || B'10'                                     -- variant bits = 10
        || (trunc(random() * 1073741824)::INT)::BIT(30) -- 30 random bits
        || (trunc(random() * 1073741824)::INT)::BIT(30) -- 30 random bits
        || (trunc(random() * 4)::INT)::BIT(2);       -- 2 random bits

    v_uuid := CONCAT(
        LPAD(UPPER(TO_HEX((v_b::BIT(32))::INT)), 8, '0'), '-',
        LPAD(UPPER(TO_HEX(((v_b << 32)::BIT(16))::INT)), 4, '0'), '-',
        LPAD(UPPER(TO_HEX(((v_b << 48)::BIT(16))::INT)), 4, '0'), '-',
        LPAD(UPPER(TO_HEX(((v_b << 64)::BIT(16))::INT)), 4, '0'), '-',
        LPAD(UPPER(TO_HEX(((v_b << 80)::BIT(48))::BIGINT)), 12, '0')
    );

    RETURN v_uuid::UUID;
END;
$$;
");

            // -------------------------------------------------------------------
            // 3. Shared updated_at trigger function
            //
            // Per Global Conventions: updated_at is maintained by this trigger
            // on every table that has it, NOT by application code.
            // This eliminates the category of bug where application code forgets
            // to set updated_at on a direct SQL write (migrations, admin scripts,
            // future microservices, etc.).
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;
");

            // -------------------------------------------------------------------
            // 4. PostgreSQL enum types
            //
            // Created before the tables that reference them.
            // Enum label casing is lowercase snake_case (Npgsql DefaultNameTranslator
            // maps: IndividualOwner → 'individual_owner' etc.)
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'company_type_enum') THEN
        CREATE TYPE company_type_enum AS ENUM (
            'individual_owner',
            'property_management_company',
            'investment_company'
        );
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'late_fee_type_enum') THEN
        CREATE TYPE late_fee_type_enum AS ENUM (
            'none',
            'fixed',
            'percentage'
        );
    END IF;
END $$;
");

            // -------------------------------------------------------------------
            // 5. companies table
            //
            // Physical Design §1.1
            // companies is the tenant root — every other tenant-scoped table
            // traces back to a row here directly or transitively.
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS companies (
    -- Primary key — UUID v7 for B-tree append-locality (see Global Conventions)
    id                          UUID            NOT NULL DEFAULT uuid_generate_v7(),

    -- Core identity
    legal_name                  VARCHAR(255)    NOT NULL,
    display_name                VARCHAR(255)    NOT NULL,
    commercial_registration_no  VARCHAR(50)     NULL,     -- رقم السجل التجاري; nullable (unregistered owners)
    tax_number                  VARCHAR(50)     NULL,     -- الرقم الضريبي; nullable (same reason)
    company_type                company_type_enum NOT NULL DEFAULT 'individual_owner',
    primary_phone               VARCHAR(20)     NOT NULL,
    primary_email               VARCHAR(255)    NULL,     -- nullable (phone-first onboarding common in Jordan)
    country_code                CHAR(2)         NOT NULL DEFAULT 'JO',  -- ISO 3166-1 alpha-2
    is_active                   BOOLEAN         NOT NULL DEFAULT TRUE,

    -- Audit (Global Conventions — all TIMESTAMPTZ)
    created_at                  TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at                  TIMESTAMPTZ     NOT NULL DEFAULT now(),
    created_by                  UUID            NULL,     -- FK → users(id); nullable (system/bootstrap)
    updated_by                  UUID            NULL,     -- FK → users(id); nullable (same reason)

    -- Soft delete (Global Conventions)
    deleted_at                  TIMESTAMPTZ     NULL,
    deleted_by                  UUID            NULL,     -- FK → users(id)

    -- Optimistic concurrency via PostgreSQL system column xmin
    -- (EF Core reads this automatically; no explicit column definition needed here —
    --  xmin is a system column present on every PostgreSQL table by default)

    CONSTRAINT pk_companies PRIMARY KEY (id),

    -- Format: +962 followed by 8 or 9 digits (Jordanian E.164 format)
    CONSTRAINT chk_companies_primary_phone_format
        CHECK (primary_phone ~ '^\+962[0-9]{8,9}$')
);
");

            // -------------------------------------------------------------------
            // 6. company_settings table
            //
            // Physical Design §1.2
            // One-to-one configuration extension of companies.
            // No soft delete. No created_by / updated_by. CASCADE from parent only.
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS company_settings (
    -- Primary key
    id                      UUID            NOT NULL DEFAULT uuid_generate_v7(),

    -- Owning company (1:1, enforced by unique constraint below)
    company_id              UUID            NOT NULL,

    -- Configuration
    default_currency        CHAR(3)         NOT NULL DEFAULT 'JOD',   -- ISO 4217
    rent_grace_period_days  SMALLINT        NOT NULL DEFAULT 5,
    late_fee_type           late_fee_type_enum NOT NULL DEFAULT 'none',
    late_fee_value          NUMERIC(12,3)   NULL,   -- NULL when late_fee_type = 'none'
    fiscal_year_start_month SMALLINT        NOT NULL DEFAULT 1,       -- 1–12
    default_language        CHAR(2)         NOT NULL DEFAULT 'ar',    -- 'ar' | 'en'
    timezone                VARCHAR(50)     NOT NULL DEFAULT 'Asia/Amman',

    -- Audit (only created_at / updated_at per Physical Design §1.2)
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT pk_company_settings PRIMARY KEY (id),

    -- 1:1 cardinality enforcement
    CONSTRAINT uq_company_settings_company_id UNIQUE (company_id),

    -- Fiscal month must be a valid calendar month
    CONSTRAINT chk_company_settings_fiscal_month
        CHECK (fiscal_year_start_month BETWEEN 1 AND 12),

    -- Late fee value must be provided iff late_fee_type is not 'none'
    CONSTRAINT chk_company_settings_late_fee_value
        CHECK (
            (late_fee_type = 'none' AND late_fee_value IS NULL)
            OR
            (late_fee_type != 'none' AND late_fee_value IS NOT NULL)
        ),

    -- FK ON DELETE CASCADE — no independent lifecycle
    CONSTRAINT fk_company_settings_companies
        FOREIGN KEY (company_id)
        REFERENCES companies (id)
        ON DELETE CASCADE
);
");

            // -------------------------------------------------------------------
            // 7. Approved indexes
            //
            // PARTIAL INDEXES — cannot be expressed by EF Core's index fluent API.
            // Created here in raw SQL. EF Core's generated non-partial index DDL
            // is NOT executed — the EF Core CreateTable above generates the schema
            // via Sql() calls, so there is no generated CreateIndex to drop.
            // -------------------------------------------------------------------

            // idx_companies_deleted_at — partial, WHERE deleted_at IS NULL
            // Rationale: every "list active companies" query filters deleted_at IS NULL;
            // partial index keeps it tiny (most rows are active) and removes deleted rows
            // from the index entirely, so admin-dashboard queries never touch deleted rows.
            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS idx_companies_deleted_at
    ON companies (deleted_at)
    WHERE deleted_at IS NULL;
");

            // uq_companies_commercial_registration_no — partial unique, WHERE IS NOT NULL
            // Rationale: multiple individual owners (unregistered) may have NULL registration
            // numbers; the uniqueness constraint must allow unlimited NULLs. A partial unique
            // index on IS NOT NULL achieves exactly this behavior atomically (race-condition-safe).
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS uq_companies_commercial_registration_no
    ON companies (commercial_registration_no)
    WHERE commercial_registration_no IS NOT NULL;
");

            // -------------------------------------------------------------------
            // 8. Per-table updated_at triggers
            //
            // BEFORE UPDATE — fires before each row update, sets updated_at = now().
            // Applied to every table with an updated_at column, per Global Conventions.
            // company_settings also gets this trigger (even though it has no created_by/
            // updated_by) because its updated_at column is authoritative for change tracking.
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
CREATE TRIGGER set_companies_updated_at
    BEFORE UPDATE ON companies
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
");

            migrationBuilder.Sql(@"
CREATE TRIGGER set_company_settings_updated_at
    BEFORE UPDATE ON company_settings
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
");

            // -------------------------------------------------------------------
            // 9. Row-Level Security — companies (Module 1)
            //
            // Full RLS policies for companies and company_settings.
            // -------------------------------------------------------------------
            migrationBuilder.Sql("ALTER TABLE companies ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE company_settings ENABLE ROW LEVEL SECURITY;");
            
            // Explicitly force RLS so even table owners are blocked without a valid policy.
            migrationBuilder.Sql("ALTER TABLE companies FORCE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE company_settings FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation_policy ON companies
    AS PERMISSIVE FOR ALL
    USING (id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation_policy ON company_settings
    AS PERMISSIVE FOR ALL
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse dependency order.
            // Triggers are dropped automatically when their tables are dropped.

            // Drop tables (CASCADE drops the FK from company_settings → companies
            // and all dependent constraints/indexes)
            migrationBuilder.Sql("DROP TABLE IF EXISTS company_settings;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS companies;");

            // Drop enum types (only if no other tables reference them)
            migrationBuilder.Sql("DROP TYPE IF EXISTS late_fee_type_enum;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS company_type_enum;");

            // Drop shared functions last (triggers depending on them are already gone)
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS uuid_generate_v7();");
        }
    }
}

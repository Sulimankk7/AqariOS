using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module4_Properties_Hierarchy_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_action_enum.audit_action", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum.audit_severity", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum.audit_source", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:building_type_enum.building_type", "residential,commercial,mixed_use")
                .Annotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:floor_type_enum.floor_type", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum.governorate", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:login_status_enum.login_status", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:membership_status_enum.membership_status", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum.mfa_type", "totp,sms")
                .Annotation("Npgsql:Enum:occupancy_status_enum.occupancy_status", "vacant,occupied,under_maintenance,listed")
                .Annotation("Npgsql:Enum:otp_purpose_enum.otp_purpose", "login,phone_verification")
                .Annotation("Npgsql:Enum:ownership_status_enum.ownership_status", "company_owned,third_party_owned")
                .Annotation("Npgsql:Enum:revoke_reason_enum.revoke_reason", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:audit_action_enum.audit_action", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum.audit_severity", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum.audit_source", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:login_status_enum.login_status", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:membership_status_enum.membership_status", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum.mfa_type", "totp,sms")
                .OldAnnotation("Npgsql:Enum:otp_purpose_enum.otp_purpose", "login,phone_verification")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum.revoke_reason", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            // -----------------------------------------------------------------------
            // CREATE MODULE 4 POSTGRESQL ENUM TYPES
            // Must run before CreateTable calls that reference these types.
            // EF Core's AlterDatabase() annotations track metadata only — they do
            // NOT emit CREATE TYPE DDL. The types must be explicitly created here.
            // suppressTransaction: true ensures these run in an auto-commit context
            // before the main migration transaction opens its CreateTable batch.
            // -----------------------------------------------------------------------
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'building_type_enum') THEN
        CREATE TYPE building_type_enum AS ENUM ('residential', 'commercial', 'mixed_use');
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'governorate_enum') THEN
        CREATE TYPE governorate_enum AS ENUM (
            'amman', 'zarqa', 'irbid', 'balqa', 'madaba',
            'karak', 'tafilah', 'maan', 'aqaba', 'ajloun', 'jerash', 'mafraq'
        );
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'floor_type_enum') THEN
        CREATE TYPE floor_type_enum AS ENUM ('basement', 'ground', 'regular', 'roof');
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ownership_status_enum') THEN
        CREATE TYPE ownership_status_enum AS ENUM ('company_owned', 'third_party_owned');
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'occupancy_status_enum') THEN
        CREATE TYPE occupancy_status_enum AS ENUM ('vacant', 'occupied', 'under_maintenance', 'listed');
    END IF;
END $$;
            ");

            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    internal_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    building_type = table.Column<int>(type: "building_type_enum", nullable: false, defaultValueSql: "'residential'"),
                    total_floors = table.Column<short>(type: "smallint", nullable: false),
                    construction_year = table.Column<short>(type: "smallint", nullable: true),
                    gps_latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    gps_longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    total_apartments_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                    table.UniqueConstraint("uq_buildings_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_buildings_construction_year_range", "construction_year IS NULL OR (construction_year BETWEEN 1900 AND EXTRACT(YEAR FROM CURRENT_DATE)::SMALLINT + 5)");
                    table.CheckConstraint("chk_buildings_gps_pair", "(gps_latitude IS NULL) = (gps_longitude IS NULL)");
                    table.CheckConstraint("chk_buildings_total_floors_positive", "total_floors > 0");
                    table.ForeignKey(
                        name: "FK_buildings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_buildings_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_buildings_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_buildings_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "building_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    governorate = table.Column<int>(type: "governorate_enum", nullable: false),
                    district = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    area = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    street_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    building_plate_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nearest_landmark = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    full_address_text = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_building_addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "floors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_number = table.Column<short>(type: "smallint", nullable: false),
                    floor_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    floor_type = table.Column<int>(type: "floor_type_enum", nullable: false, defaultValueSql: "'regular'"),
                    apartments_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_floors", x => x.id);
                    table.UniqueConstraint("uq_floors_company_building_id", x => new { x.company_id, x.building_id, x.id });
                    table.ForeignKey(
                        name: "FK_floors_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_floors_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_floors_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_floors_buildings_company_building",
                        columns: x => new { x.company_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "apartments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ownership_status = table.Column<int>(type: "ownership_status_enum", nullable: false, defaultValueSql: "'company_owned'"),
                    external_owner_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    external_owner_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    occupancy_status = table.Column<int>(type: "occupancy_status_enum", nullable: false, defaultValueSql: "'vacant'"),
                    area_sqm = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    bedrooms = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    bathrooms = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    base_rent_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                    base_rent_currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartments", x => x.id);
                    table.CheckConstraint("chk_apartments_area_positive", "area_sqm > 0");
                    table.CheckConstraint("chk_apartments_base_rent_positive", "base_rent_amount IS NULL OR base_rent_amount > 0");
                    table.CheckConstraint("chk_apartments_bathrooms_nonneg", "bathrooms >= 0");
                    table.CheckConstraint("chk_apartments_bedrooms_nonneg", "bedrooms >= 0");
                    table.CheckConstraint("chk_apartments_external_owner_required", "(ownership_status = 'company_owned') OR (external_owner_name IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_apartments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_apartments_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_apartments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_apartments_floors_company_building_floor",
                        columns: x => new { x.company_id, x.building_id, x.floor_id },
                        principalTable: "floors",
                        principalColumns: new[] { "company_id", "building_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_apartments_building_id",
                table: "apartments",
                columns: new[] { "building_id", "floor_id" });

            migrationBuilder.CreateIndex(
                name: "idx_apartments_company_occupancy",
                table: "apartments",
                columns: new[] { "company_id", "occupancy_status" });

            migrationBuilder.CreateIndex(
                name: "idx_apartments_company_occupancy_bedrooms",
                table: "apartments",
                columns: new[] { "company_id", "occupancy_status", "bedrooms" });

            migrationBuilder.CreateIndex(
                name: "idx_apartments_floor_id",
                table: "apartments",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "IX_apartments_company_id_building_id_floor_id",
                table: "apartments",
                columns: new[] { "company_id", "building_id", "floor_id" });

            migrationBuilder.CreateIndex(
                name: "IX_apartments_created_by",
                table: "apartments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_apartments_deleted_by",
                table: "apartments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_apartments_updated_by",
                table: "apartments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_apartments_building_unit_number",
                table: "apartments",
                columns: new[] { "building_id", "unit_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_building_addresses_governorate_district",
                table: "building_addresses",
                columns: new[] { "company_id", "governorate", "district" });

            migrationBuilder.CreateIndex(
                name: "uq_building_addresses_building_id",
                table: "building_addresses",
                column: "building_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_buildings_created_by",
                table: "buildings",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_buildings_deleted_by",
                table: "buildings",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_buildings_updated_by",
                table: "buildings",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_buildings_company_internal_code",
                table: "buildings",
                columns: new[] { "company_id", "internal_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_floors_created_by",
                table: "floors",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_floors_deleted_by",
                table: "floors",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_floors_updated_by",
                table: "floors",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_floors_building_floor_number",
                table: "floors",
                columns: new[] { "building_id", "floor_number" },
                unique: true);

            // -------------------------------------------------------------------
            // MANUAL SQL: Composite FK, Partial Indexes, Counter Trigger, RLS, Grants
            // Must run AFTER all EF-generated CreateTable and CreateIndex calls above.
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
-- =============================================================================
-- 0. PREREQUISITE ROLE VALIDATION
-- =============================================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'propertyos_owner') THEN
        RAISE EXCEPTION 'Required role propertyos_owner does not exist.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'propertyos_app') THEN
        RAISE EXCEPTION 'Required role propertyos_app does not exist.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'propertyos_auth') THEN
        RAISE EXCEPTION 'Required role propertyos_auth does not exist.';
    END IF;
END
$$;

-- =============================================================================
-- 1. COMPOSITE FK: building_addresses(company_id, building_id)
--                  → buildings(company_id, id)
--
-- EF Core cannot express this composite FK alongside the independent
-- company_id FK in a single fluent mapping without relationship conflicts.
-- This raw SQL FK is the physical hierarchy integrity mechanism that prevents
-- an address from referencing a building owned by a different company.
-- The referenced key is the candidate key uq_buildings_company_id.
-- =============================================================================
ALTER TABLE building_addresses
    ADD CONSTRAINT fk_building_addresses_buildings_company_building
    FOREIGN KEY (company_id, building_id)
    REFERENCES buildings(company_id, id)
    ON DELETE CASCADE;

-- =============================================================================
-- 2. PARTIAL UNIQUE INDEXES (drop EF full indexes, recreate with WHERE clause)
-- =============================================================================

-- buildings: partial unique on internal_code
DROP INDEX IF EXISTS uq_buildings_company_internal_code;
CREATE UNIQUE INDEX uq_buildings_company_internal_code
    ON buildings(company_id, internal_code)
    WHERE internal_code IS NOT NULL AND deleted_at IS NULL;

-- floors: partial unique on floor_number per building
DROP INDEX IF EXISTS uq_floors_building_floor_number;
CREATE UNIQUE INDEX uq_floors_building_floor_number
    ON floors(building_id, floor_number)
    WHERE deleted_at IS NULL;

-- apartments: partial unique on unit_number per building
DROP INDEX IF EXISTS uq_apartments_building_unit_number;
CREATE UNIQUE INDEX uq_apartments_building_unit_number
    ON apartments(building_id, unit_number)
    WHERE deleted_at IS NULL;

-- =============================================================================
-- 3. APPROVED HOT-PATH PARTIAL INDEXES
-- =============================================================================

-- buildings: tenant-scoped listing
DROP INDEX IF EXISTS idx_buildings_company_id;
CREATE INDEX idx_buildings_company_id
    ON buildings(company_id)
    WHERE deleted_at IS NULL;

-- buildings: active-only filter
DROP INDEX IF EXISTS idx_buildings_company_active;
CREATE INDEX idx_buildings_company_active
    ON buildings(company_id, is_active)
    WHERE deleted_at IS NULL;

-- buildings: trigram name search (requires pg_trgm — present since Module 1)
DROP INDEX IF EXISTS idx_buildings_name_trgm;
CREATE INDEX idx_buildings_name_trgm
    ON buildings USING GIN (name gin_trgm_ops)
    WHERE deleted_at IS NULL;

-- building_addresses: location filter hot path
DROP INDEX IF EXISTS idx_building_addresses_governorate_district;
CREATE INDEX idx_building_addresses_governorate_district
    ON building_addresses(company_id, governorate, district);

-- building_addresses: neighborhood trigram search
DROP INDEX IF EXISTS idx_building_addresses_area_trgm;
CREATE INDEX idx_building_addresses_area_trgm
    ON building_addresses USING GIN (area gin_trgm_ops)
    WHERE area IS NOT NULL;

-- floors: building drill-down ordered by floor_number
DROP INDEX IF EXISTS idx_floors_building_id;
CREATE INDEX idx_floors_building_id
    ON floors(building_id, floor_number)
    WHERE deleted_at IS NULL;

-- apartments: building detail page by floor
DROP INDEX IF EXISTS idx_apartments_building_id;
CREATE INDEX idx_apartments_building_id
    ON apartments(building_id, floor_id)
    WHERE deleted_at IS NULL;

-- apartments: company-wide occupancy dashboard (hottest index in module)
DROP INDEX IF EXISTS idx_apartments_company_occupancy;
CREATE INDEX idx_apartments_company_occupancy
    ON apartments(company_id, occupancy_status)
    WHERE deleted_at IS NULL;

-- apartments: vacant unit search with bedroom filter
DROP INDEX IF EXISTS idx_apartments_company_occupancy_bedrooms;
CREATE INDEX idx_apartments_company_occupancy_bedrooms
    ON apartments(company_id, occupancy_status, bedrooms)
    WHERE deleted_at IS NULL AND occupancy_status = 'vacant';

-- apartments: floor-scoped lookup
DROP INDEX IF EXISTS idx_apartments_floor_id;
CREATE INDEX idx_apartments_floor_id
    ON apartments(floor_id)
    WHERE deleted_at IS NULL;

-- =============================================================================
-- 4. APARTMENT COUNTER TRIGGER
--
-- Maintains buildings.total_apartments_count and floors.apartments_count
-- transactionally on every INSERT or soft-delete of an apartment.
--
-- Logic:
--   INSERT of active apartment (deleted_at IS NULL): +1 to both counters.
--   UPDATE setting deleted_at NULL → value (soft-delete): -1 to both.
--   UPDATE setting deleted_at value → NULL (restore): +1 to both.
--   Other UPDATEs: no change.
-- =============================================================================
CREATE OR REPLACE FUNCTION trg_apartments_update_counters()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
DECLARE
    v_delta INTEGER := 0;
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW.deleted_at IS NULL THEN
            v_delta := 1;
        END IF;
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.deleted_at IS NULL AND NEW.deleted_at IS NOT NULL THEN
            v_delta := -1;
        ELSIF OLD.deleted_at IS NOT NULL AND NEW.deleted_at IS NULL THEN
            v_delta := 1;
        END IF;
    END IF;

    IF v_delta != 0 THEN
        UPDATE public.buildings
        SET    total_apartments_count = GREATEST(0, total_apartments_count + v_delta)
        WHERE  company_id = NEW.company_id 
          AND  id = NEW.building_id;

        UPDATE public.floors
        SET    apartments_count = GREATEST(0, apartments_count + v_delta)
        WHERE  company_id = NEW.company_id 
          AND  building_id = NEW.building_id 
          AND  id = NEW.floor_id;
    END IF;

    RETURN NEW;
END;
$$;

ALTER FUNCTION public.trg_apartments_update_counters() OWNER TO propertyos_owner;
REVOKE EXECUTE ON FUNCTION public.trg_apartments_update_counters() FROM PUBLIC;

-- =============================================================================
-- 5. OWNERSHIP
-- =============================================================================
ALTER TABLE buildings OWNER TO propertyos_owner;
ALTER TABLE building_addresses OWNER TO propertyos_owner;
ALTER TABLE floors OWNER TO propertyos_owner;
ALTER TABLE apartments OWNER TO propertyos_owner;

CREATE TRIGGER trg_apartments_counters
    AFTER INSERT OR UPDATE OF deleted_at
    ON apartments
    FOR EACH ROW
    EXECUTE FUNCTION trg_apartments_update_counters();

-- =============================================================================
-- 5. ROW LEVEL SECURITY
-- =============================================================================

-- buildings
ALTER TABLE buildings ENABLE ROW LEVEL SECURITY;
ALTER TABLE buildings FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_buildings_app ON buildings
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

-- building_addresses
ALTER TABLE building_addresses ENABLE ROW LEVEL SECURITY;
ALTER TABLE building_addresses FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_building_addresses_app ON building_addresses
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

-- floors
ALTER TABLE floors ENABLE ROW LEVEL SECURITY;
ALTER TABLE floors FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_floors_app ON floors
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

CREATE POLICY rls_buildings_owner_select ON buildings
    FOR SELECT TO propertyos_owner
    USING (true);

CREATE POLICY rls_buildings_owner_update ON buildings
    FOR UPDATE TO propertyos_owner
    USING (true) WITH CHECK (true);

CREATE POLICY rls_floors_owner_select ON floors
    FOR SELECT TO propertyos_owner
    USING (true);

CREATE POLICY rls_floors_owner_update ON floors
    FOR UPDATE TO propertyos_owner
    USING (true) WITH CHECK (true);

-- apartments
ALTER TABLE apartments ENABLE ROW LEVEL SECURITY;
ALTER TABLE apartments FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_apartments_app ON apartments
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

-- =============================================================================
-- 6. GRANTS
-- =============================================================================
GRANT SELECT, INSERT ON buildings, building_addresses, floors, apartments TO propertyos_app;

GRANT UPDATE (name, internal_code, building_type, total_floors, construction_year, gps_latitude, gps_longitude, is_active, updated_at, updated_by, deleted_at, deleted_by) ON buildings TO propertyos_app;
GRANT UPDATE (governorate, district, area, street_name, building_plate_number, nearest_landmark, postal_code, full_address_text, updated_at) ON building_addresses TO propertyos_app;
GRANT UPDATE (floor_number, floor_label, floor_type, updated_at, updated_by, deleted_at, deleted_by) ON floors TO propertyos_app;
GRANT UPDATE (unit_number, ownership_status, external_owner_name, external_owner_phone, occupancy_status, area_sqm, bedrooms, bathrooms, base_rent_amount, base_rent_currency, is_active, updated_at, updated_by, deleted_at, deleted_by) ON apartments TO propertyos_app;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apartments");

            migrationBuilder.DropTable(
                name: "building_addresses");

            migrationBuilder.DropTable(
                name: "floors");

            migrationBuilder.DropTable(
                name: "buildings");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_action_enum.audit_action", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum.audit_severity", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum.audit_source", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:login_status_enum.login_status", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:membership_status_enum.membership_status", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum.mfa_type", "totp,sms")
                .Annotation("Npgsql:Enum:otp_purpose_enum.otp_purpose", "login,phone_verification")
                .Annotation("Npgsql:Enum:revoke_reason_enum.revoke_reason", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:audit_action_enum.audit_action", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum.audit_severity", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum.audit_source", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:building_type_enum.building_type", "residential,commercial,mixed_use")
                .OldAnnotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:floor_type_enum.floor_type", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum.governorate", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:login_status_enum.login_status", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:membership_status_enum.membership_status", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum.mfa_type", "totp,sms")
                .OldAnnotation("Npgsql:Enum:occupancy_status_enum.occupancy_status", "vacant,occupied,under_maintenance,listed")
                .OldAnnotation("Npgsql:Enum:otp_purpose_enum.otp_purpose", "login,phone_verification")
                .OldAnnotation("Npgsql:Enum:ownership_status_enum.ownership_status", "company_owned,third_party_owned")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum.revoke_reason", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}

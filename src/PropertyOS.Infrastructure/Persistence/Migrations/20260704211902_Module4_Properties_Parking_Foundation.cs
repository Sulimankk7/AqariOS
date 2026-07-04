using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module4_Properties_Parking_Foundation : Migration
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
                .Annotation("Npgsql:Enum:parking_assignment_status_enum.parking_assignment_status", "active,ended")
                .Annotation("Npgsql:Enum:parking_type_enum.parking_type", "standard,covered,visitor,disabled_access")
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

            migrationBuilder.AddUniqueConstraint(
                name: "uq_apartments_company_building_id",
                table: "apartments",
                columns: new[] { "company_id", "building_id", "id" });

            // -----------------------------------------------------------------------
            // CREATE MODULE 4 PARKING POSTGRESQL ENUM TYPES
            // Must run before CreateTable calls that reference these types.
            // EF Core's AlterDatabase() annotations track metadata only — they do
            // NOT emit CREATE TYPE DDL. The types must be explicitly created here.
            // -----------------------------------------------------------------------
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'parking_type_enum') THEN
        CREATE TYPE parking_type_enum AS ENUM ('standard', 'covered', 'visitor', 'disabled_access');
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'parking_assignment_status_enum') THEN
        CREATE TYPE parking_assignment_status_enum AS ENUM ('active', 'ended');
    END IF;
END $$;
            ");

            migrationBuilder.CreateTable(
                name: "parking_spots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_apartment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    spot_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parking_type = table.Column<int>(type: "parking_type_enum", nullable: false, defaultValueSql: "'standard'"),
                    location_description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_spots", x => x.id);
                    table.UniqueConstraint("uq_parking_spots_company_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_parking_spots_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_parking_spots_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_parking_spots_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    // The fk_parking_spots_apartments_company_building_apartment constraint is omitted
                    // from EF generation and handled via raw SQL to enable column-list SET NULL semantics.
                    table.ForeignKey(
                        name: "fk_parking_spots_buildings_company_building",
                        columns: x => new { x.company_id, x.building_id },
                        principalTable: "buildings",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parking_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parking_spot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from = table.Column<DateOnly>(type: "date", nullable: false),
                    assigned_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "parking_assignment_status_enum", nullable: false, defaultValueSql: "'active'"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parking_assignments", x => x.id);
                    table.CheckConstraint("chk_parking_assignments_dates", "assigned_to IS NULL OR assigned_to > assigned_from");
                    table.ForeignKey(
                        name: "FK_parking_assignments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_parking_assignments_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_parking_assignments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_parking_assignments_parking_spots_company_spot",
                        columns: x => new { x.company_id, x.parking_spot_id },
                        principalTable: "parking_spots",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_parking_assignments_lease_contract_id",
                table: "parking_assignments",
                column: "lease_contract_id",
                filter: "deleted_at IS NULL");

        // The idx_parking_assignments_parking_spot_id history index is intentionally omitted
        // from EF generation and manually created to avoid same-property-set EF metadata collision.

            migrationBuilder.CreateIndex(
                name: "uq_parking_assignments_active_spot",
                table: "parking_assignments",
                column: "parking_spot_id",
                unique: true,
                filter: "status = 'active' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_parking_assignments_company_id_parking_spot_id",
                table: "parking_assignments",
                columns: new[] { "company_id", "parking_spot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_assignments_created_by",
                table: "parking_assignments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_parking_assignments_deleted_by",
                table: "parking_assignments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_parking_assignments_updated_by",
                table: "parking_assignments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_parking_spots_building_id",
                table: "parking_spots",
                column: "building_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_parking_spots_company_active",
                table: "parking_spots",
                columns: new[] { "company_id", "is_active" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_parking_spots_default_apartment_id",
                table: "parking_spots",
                column: "default_apartment_id",
                filter: "default_apartment_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_parking_spots_company_id_building_id_default_apartment_id",
                table: "parking_spots",
                columns: new[] { "company_id", "building_id", "default_apartment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_parking_spots_created_by",
                table: "parking_spots",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_parking_spots_deleted_by",
                table: "parking_spots",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_parking_spots_updated_by",
                table: "parking_spots",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_parking_spots_building_spot_code",
                table: "parking_spots",
                columns: new[] { "building_id", "spot_code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.Sql(@"
-- =============================================================================
-- 1. COMPOSITE FK WITH SET NULL (COLUMN LIST) SEMANTICS
-- =============================================================================
ALTER TABLE parking_spots ADD CONSTRAINT fk_parking_spots_apartments_company_building_apartment
FOREIGN KEY (company_id, building_id, default_apartment_id)
REFERENCES apartments(company_id, building_id, id) ON DELETE SET NULL (default_apartment_id);

-- =============================================================================
-- 2. OWNERSHIP
-- =============================================================================
ALTER TABLE parking_spots OWNER TO propertyos_owner;
ALTER TABLE parking_assignments OWNER TO propertyos_owner;

-- =============================================================================
-- 3. RLS / FORCE RLS
-- =============================================================================
ALTER TABLE parking_spots ENABLE ROW LEVEL SECURITY;
ALTER TABLE parking_spots FORCE ROW LEVEL SECURITY;

CREATE POLICY rls_parking_spots_app ON parking_spots
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

ALTER TABLE parking_assignments ENABLE ROW LEVEL SECURITY;
ALTER TABLE parking_assignments FORCE ROW LEVEL SECURITY;

CREATE POLICY rls_parking_assignments_app ON parking_assignments
    TO propertyos_app
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

-- =============================================================================
-- 4. GRANTS
-- =============================================================================
GRANT SELECT, INSERT ON parking_spots, parking_assignments TO propertyos_app;

GRANT UPDATE (default_apartment_id, spot_code, parking_type, location_description, is_active, updated_at, updated_by, deleted_at, deleted_by) ON parking_spots TO propertyos_app;

GRANT UPDATE (assigned_to, status, updated_at, updated_by, deleted_at, deleted_by) ON parking_assignments TO propertyos_app;

-- =============================================================================
-- 5. PERFORMANCE INDEXES (MANUALLY OWNED)
-- =============================================================================
-- EF Core cannot reliably model two distinct indexes over the exact same
-- property set without metadata collision. The unique active-assignment index
-- is modeled by EF. This non-unique history query index is manually created.
CREATE INDEX idx_parking_assignments_parking_spot_id
ON parking_assignments(parking_spot_id) WHERE deleted_at IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parking_assignments");

            migrationBuilder.DropTable(
                name: "parking_spots");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_apartments_company_building_id",
                table: "apartments");

            migrationBuilder.Sql(@"
DROP TYPE IF EXISTS parking_assignment_status_enum;
DROP TYPE IF EXISTS parking_type_enum;
            ");

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
                .OldAnnotation("Npgsql:Enum:parking_assignment_status_enum.parking_assignment_status", "active,ended")
                .OldAnnotation("Npgsql:Enum:parking_type_enum.parking_type", "standard,covered,visitor,disabled_access")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum.revoke_reason", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}

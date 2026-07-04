using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module3_Security_Identity_Rbac_Audit_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired");

            // -------------------------------------------------------------------
            // Module 3 PostgreSQL enum type creation
            // Idempotent guards mirror the pattern established in Module 1/2.
            // These must run BEFORE any table that references the enum type.
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'audit_action_enum') THEN
        CREATE TYPE audit_action_enum AS ENUM (
            'create', 'update', 'delete', 'soft_delete', 'restore',
            'login', 'logout', 'permission_change', 'export', 'status_change'
        );
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'audit_severity_enum') THEN
        CREATE TYPE audit_severity_enum AS ENUM ('info', 'warning', 'critical');
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'audit_source_enum') THEN
        CREATE TYPE audit_source_enum AS ENUM ('api', 'web', 'mobile', 'system_job', 'admin_console');
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'login_status_enum') THEN
        CREATE TYPE login_status_enum AS ENUM (
            'success', 'failed_password', 'failed_locked', 'failed_mfa', 'failed_not_found'
        );
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'membership_status_enum') THEN
        CREATE TYPE membership_status_enum AS ENUM ('invited_pending', 'active', 'suspended');
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'mfa_type_enum') THEN
        CREATE TYPE mfa_type_enum AS ENUM ('totp', 'sms');
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'otp_purpose_enum') THEN
        CREATE TYPE otp_purpose_enum AS ENUM ('login', 'phone_verification');
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'revoke_reason_enum') THEN
        CREATE TYPE revoke_reason_enum AS ENUM (
            'rotated', 'logout', 'theft_detected', 'admin_revoked', 'expired'
        );
    END IF;
END $$;
");

            migrationBuilder.CreateTable(
                name: "otp_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    purpose = table.Column<int>(type: "otp_purpose_enum", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    max_attempts = table.Column<short>(type: "smallint", nullable: false),
                    requested_ip = table.Column<IPAddress>(type: "inet", nullable: false),
                    verified_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_challenges", x => x.id);
                    table.CheckConstraint("chk_otp_challenges_expiry", "expires_at > created_at");
                    table.CheckConstraint("chk_otp_challenges_failed_attempts", "failed_attempts >= 0 AND max_attempts > 0 AND failed_attempts <= max_attempts");
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description_en = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description_ar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_deprecated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("chk_roles_is_system_consistency", "(is_system = true AND company_id IS NULL) OR (is_system = false AND company_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_roles_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    email = table.Column<string>(type: "citext", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    password_algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "argon2id"),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    preferred_language = table.Column<string>(type: "character(2)", maxLength: 2, nullable: false, defaultValue: "ar"),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    phone_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    password_reset_token_hash = table.Column<string>(type: "text", nullable: true),
                    password_reset_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    mfa_secret_encrypted = table.Column<string>(type: "text", nullable: true),
                    mfa_type = table.Column<int>(type: "mfa_type_enum", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("chk_users_email_or_phone", "email IS NOT NULL OR phone IS NOT NULL");
                    table.CheckConstraint("chk_users_mfa_secret_required", "(mfa_enabled = false) OR (mfa_secret_encrypted IS NOT NULL AND mfa_type IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_users_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<int>(type: "audit_action_enum", nullable: false),
                    previous_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    severity = table.Column<int>(type: "audit_severity_enum", nullable: false, defaultValueSql: "'info'::audit_severity_enum"),
                    source = table.Column<int>(type: "audit_source_enum", nullable: false, defaultValueSql: "'api'::audit_source_enum"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "login_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempted_identifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "login_status_enum", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    geolocation_country = table.Column<string>(type: "character(2)", maxLength: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_history", x => x.id);
                    table.CheckConstraint("chk_login_history_success_has_user", "status != 'success' OR user_id IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_login_history_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_login_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    device_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<int>(type: "revoke_reason_enum", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("chk_refresh_tokens_revoked_reason", "revoked_reason IS NOT NULL OR revoked_at IS NULL");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_replaced_by_token_id",
                        column: x => x.replaced_by_token_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_company_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "membership_status_enum", nullable: false, defaultValueSql: "'invited_pending'::membership_status_enum"),
                    invited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company_roles", x => x.id);
                    table.CheckConstraint("chk_user_company_roles_joined_at", "joined_at IS NULL OR joined_at >= invited_at");
                    table.ForeignKey(
                        name: "FK_user_company_roles_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_company_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_actor_user_id",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_company_occurred_at",
                table: "audit_logs",
                columns: new[] { "company_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_login_history_ip_created_at",
                table: "login_history",
                columns: new[] { "ip_address", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_login_history_status_created_at",
                table: "login_history",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_login_history_user_id_created_at",
                table: "login_history",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_login_history_company_id",
                table: "login_history",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_otp_challenges_expires_at",
                table: "otp_challenges",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_otp_challenges_phone_purpose_created_at",
                table: "otp_challenges",
                columns: new[] { "phone", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_permissions_module",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "uq_permissions_key",
                table: "permissions",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_replaced_by_token_id",
                table: "refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "uq_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_granted_by",
                table: "role_permissions",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_role_permissions_role_permission",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_roles_company_id",
                table: "roles",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_roles_company_code",
                table: "roles",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_roles_system_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_company_roles_company_status",
                table: "user_company_roles",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_user_company_roles_user_id",
                table: "user_company_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_roles_role_id",
                table: "user_company_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_company_roles_user_company",
                table: "user_company_roles",
                columns: new[] { "user_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_locked_until",
                table: "users",
                column: "locked_until");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_by",
                table: "users",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_phone",
                table: "users",
                column: "phone",
                unique: true);

            // ---------------------------------------------------------------------------------------------------------
            // MANUAL MIGRATION SQL: RLS Policies, Partial Indexes, and Missing Constraints
            // ---------------------------------------------------------------------------------------------------------
            migrationBuilder.Sql(@"
-- 0. Prerequisites Validation
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

-- 1. Partial Indexes (Drop EF's generic indexes and recreate with WHERE clauses)
DROP INDEX IF EXISTS idx_permissions_module;
CREATE INDEX idx_permissions_module ON permissions(module) WHERE is_deprecated = false;

DROP INDEX IF EXISTS idx_refresh_tokens_expires_at;
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at) WHERE revoked_at IS NULL;

DROP INDEX IF EXISTS idx_refresh_tokens_user_id;
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id) WHERE revoked_at IS NULL;

DROP INDEX IF EXISTS idx_roles_company_id;
CREATE INDEX idx_roles_company_id ON roles(company_id) WHERE company_id IS NOT NULL AND deleted_at IS NULL;

DROP INDEX IF EXISTS uq_roles_company_code;
CREATE UNIQUE INDEX uq_roles_company_code ON roles(company_id, code) WHERE company_id IS NOT NULL AND deleted_at IS NULL;

DROP INDEX IF EXISTS uq_roles_system_code;
CREATE UNIQUE INDEX uq_roles_system_code ON roles(code) WHERE company_id IS NULL AND deleted_at IS NULL;

DROP INDEX IF EXISTS uq_user_company_roles_user_company;
CREATE UNIQUE INDEX uq_user_company_roles_user_company ON user_company_roles(user_id, company_id) WHERE deleted_at IS NULL;

DROP INDEX IF EXISTS idx_users_locked_until;
CREATE INDEX idx_users_locked_until ON users(locked_until) WHERE locked_until IS NOT NULL;

DROP INDEX IF EXISTS uq_users_email;
CREATE UNIQUE INDEX uq_users_email ON users(email) WHERE email IS NOT NULL AND deleted_at IS NULL;

DROP INDEX IF EXISTS uq_users_phone;
CREATE UNIQUE INDEX uq_users_phone ON users(phone) WHERE phone IS NOT NULL AND deleted_at IS NULL;

DROP INDEX IF EXISTS idx_user_company_roles_user_id;
CREATE INDEX idx_user_company_roles_user_id ON user_company_roles(user_id) WHERE deleted_at IS NULL;

DROP INDEX IF EXISTS idx_user_company_roles_company_status;
CREATE INDEX idx_user_company_roles_company_status ON user_company_roles(company_id, status) WHERE deleted_at IS NULL;

DROP INDEX IF EXISTS idx_audit_logs_actor_user_id;
CREATE INDEX idx_audit_logs_actor_user_id ON audit_logs(actor_user_id, occurred_at DESC) WHERE actor_user_id IS NOT NULL;

DROP INDEX IF EXISTS idx_audit_logs_company_occurred_at;
CREATE INDEX idx_audit_logs_company_occurred_at ON audit_logs(company_id, occurred_at DESC) WHERE company_id IS NOT NULL;

DROP INDEX IF EXISTS idx_audit_logs_entity;
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_name, entity_id, occurred_at DESC) WHERE entity_id IS NOT NULL;

DROP INDEX IF EXISTS idx_login_history_status_created_at;
CREATE INDEX idx_login_history_status_created_at ON login_history(status, created_at DESC) WHERE status != 'success';

DROP INDEX IF EXISTS idx_login_history_user_id_created_at;
CREATE INDEX idx_login_history_user_id_created_at ON login_history(user_id, created_at DESC) WHERE user_id IS NOT NULL;

DROP INDEX IF EXISTS idx_otp_challenges_expires_at;
CREATE INDEX idx_otp_challenges_expires_at ON otp_challenges(expires_at) WHERE consumed_at IS NULL;

-- 2. ROW LEVEL SECURITY (RLS) Policies

-- users
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE users FORCE ROW LEVEL SECURITY;
CREATE POLICY select_users_app ON users FOR SELECT TO propertyos_app USING (id = NULLIF(current_setting('app.current_user_id', true), '')::uuid);
CREATE POLICY update_users_app ON users FOR UPDATE TO propertyos_app USING (id = NULLIF(current_setting('app.current_user_id', true), '')::uuid);
CREATE POLICY all_users_auth ON users TO propertyos_auth USING (true);

-- user_company_roles
ALTER TABLE user_company_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_company_roles FORCE ROW LEVEL SECURITY;
CREATE POLICY all_user_company_roles ON user_company_roles USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

-- roles
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE roles FORCE ROW LEVEL SECURITY;
CREATE POLICY select_roles ON roles FOR SELECT USING (company_id IS NULL OR company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
CREATE POLICY modify_roles ON roles FOR ALL USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid AND is_system = false);

-- permissions
ALTER TABLE permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE permissions FORCE ROW LEVEL SECURITY;
CREATE POLICY select_permissions ON permissions FOR SELECT USING (true);

-- role_permissions
ALTER TABLE role_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE role_permissions FORCE ROW LEVEL SECURITY;
CREATE POLICY select_role_permissions ON role_permissions FOR SELECT USING (
  EXISTS (SELECT 1 FROM roles r WHERE r.id = role_permissions.role_id AND r.deleted_at IS NULL AND (r.company_id IS NULL OR r.company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid))
);
CREATE POLICY modify_role_permissions ON role_permissions FOR ALL USING (
  EXISTS (SELECT 1 FROM roles r WHERE r.id = role_permissions.role_id AND r.deleted_at IS NULL AND r.company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid AND r.is_system = false)
);

-- refresh_tokens
ALTER TABLE refresh_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE refresh_tokens FORCE ROW LEVEL SECURITY;
CREATE POLICY modify_refresh_tokens_app ON refresh_tokens TO propertyos_app USING (user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid);
CREATE POLICY all_refresh_tokens_auth ON refresh_tokens TO propertyos_auth USING (true);

-- login_history
ALTER TABLE login_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE login_history FORCE ROW LEVEL SECURITY;
CREATE POLICY select_login_history ON login_history FOR SELECT TO propertyos_app USING (user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid);
CREATE POLICY insert_login_history ON login_history FOR INSERT TO propertyos_auth WITH CHECK (true);

-- audit_logs
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_logs FORCE ROW LEVEL SECURITY;
CREATE POLICY audit_logs_select ON audit_logs FOR SELECT TO propertyos_app, propertyos_auth USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
CREATE POLICY audit_logs_insert ON audit_logs FOR INSERT TO propertyos_owner WITH CHECK (true);

-- otp_challenges
ALTER TABLE otp_challenges ENABLE ROW LEVEL SECURITY;
ALTER TABLE otp_challenges FORCE ROW LEVEL SECURITY;
CREATE POLICY all_otp_challenges ON otp_challenges TO propertyos_auth USING (true);

-- 3. SECURITY DEFINER function for audit logging
CREATE OR REPLACE FUNCTION insert_audit_log(
    p_entity_name       VARCHAR(100),
    p_entity_id         UUID,
    p_action            audit_action_enum,
    p_previous_values   JSONB,
    p_new_values        JSONB,
    p_occurred_at       TIMESTAMPTZ,
    p_actor_user_id     UUID,
    p_company_id        UUID,
    p_request_id        UUID,
    p_correlation_id    UUID,
    p_ip_address        INET,
    p_user_agent        TEXT,
    p_severity          audit_severity_enum,
    p_source            audit_source_enum,
    p_metadata          JSONB
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
BEGIN
    IF p_entity_name IS NULL OR p_occurred_at IS NULL THEN
        RAISE EXCEPTION 'insert_audit_log: entity_name and occurred_at are required';
    END IF;

    INSERT INTO audit_logs (
        entity_name, entity_id, action, previous_values, new_values,
        occurred_at, actor_user_id, company_id, request_id, correlation_id,
        ip_address, user_agent, severity, source, metadata
    ) VALUES (
        p_entity_name, p_entity_id, p_action, p_previous_values, p_new_values,
        p_occurred_at, p_actor_user_id, p_company_id, p_request_id, p_correlation_id,
        p_ip_address, p_user_agent, p_severity, p_source, COALESCE(p_metadata, '{}'::jsonb)
    );
END;
$$;

-- 4. GRANTs & REVOKEs
-- Roles MUST exist per prerequisite check
ALTER FUNCTION insert_audit_log OWNER TO propertyos_owner;

REVOKE EXECUTE ON FUNCTION insert_audit_log FROM PUBLIC;

GRANT EXECUTE ON FUNCTION insert_audit_log TO propertyos_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON companies, company_settings, subscription_plans, company_subscriptions TO propertyos_app;
REVOKE ALL ON audit_logs FROM propertyos_app;

GRANT EXECUTE ON FUNCTION insert_audit_log TO propertyos_auth;
GRANT SELECT, UPDATE ON users TO propertyos_auth;
GRANT SELECT, INSERT, UPDATE, DELETE ON refresh_tokens, otp_challenges TO propertyos_auth;
GRANT INSERT ON login_history TO propertyos_auth;
REVOKE ALL ON audit_logs FROM propertyos_auth;

GRANT INSERT ON audit_logs TO propertyos_owner;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "login_history");

            migrationBuilder.DropTable(
                name: "otp_challenges");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_company_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:billing_cycle_enum.billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:company_type_enum.company_type", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:late_fee_type_enum.late_fee_type", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:subscription_status_enum.subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
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
        }
    }
}

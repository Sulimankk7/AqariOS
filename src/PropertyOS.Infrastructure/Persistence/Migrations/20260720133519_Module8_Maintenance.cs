using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module8_Maintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:allocation_status_enum", "active,reversed")
                .Annotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .Annotation("Npgsql:Enum:cheque_status_enum", "issued,received,deposited,cleared,bounced,cancelled")
                .Annotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .Annotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
                .Annotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .Annotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .Annotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .Annotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .Annotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .Annotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:maintenance_category_enum", "electrical,plumbing,air_conditioning,elevator,cleaning,water,structural,doors_windows,internet,other")
                .Annotation("Npgsql:Enum:maintenance_priority_enum", "low,medium,high,emergency")
                .Annotation("Npgsql:Enum:maintenance_status_enum", "open,in_progress,waiting,resolved,closed,cancelled")
                .Annotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .Annotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .Annotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .Annotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .Annotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .Annotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .Annotation("Npgsql:Enum:payment_frequency_enum", "monthly,quarterly,semi_annual,annual")
                .Annotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom")
                .Annotation("Npgsql:Enum:payment_purpose_enum", "scheduled_installment,unallocated_receipt,adjustment")
                .Annotation("Npgsql:Enum:receipt_reset_policy_enum", "never,yearly,monthly")
                .Annotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .Annotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:allocation_status_enum", "active,reversed")
                .OldAnnotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .OldAnnotation("Npgsql:Enum:cheque_status_enum", "issued,received,deposited,cleared,bounced,cancelled")
                .OldAnnotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .OldAnnotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
                .OldAnnotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .OldAnnotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .OldAnnotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .OldAnnotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .OldAnnotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .OldAnnotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .OldAnnotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .OldAnnotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .OldAnnotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .OldAnnotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .OldAnnotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .OldAnnotation("Npgsql:Enum:payment_frequency_enum", "monthly,quarterly,semi_annual,annual")
                .OldAnnotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom")
                .OldAnnotation("Npgsql:Enum:payment_purpose_enum", "scheduled_installment,unallocated_receipt,adjustment")
                .OldAnnotation("Npgsql:Enum:receipt_reset_policy_enum", "never,yearly,monthly")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .OldAnnotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "maintenance_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<int>(type: "maintenance_category_enum", nullable: false),
                    priority = table.Column<int>(type: "maintenance_priority_enum", nullable: false),
                    status = table.Column<int>(type: "maintenance_status_enum", nullable: false, defaultValueSql: "'open'"),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    closed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_maintenance_requests", x => x.id);
                    table.CheckConstraint("chk_maintenance_requests_closed_date_not_before_request_date", "closed_date IS NULL OR closed_date >= request_date");
                    table.CheckConstraint("chk_maintenance_requests_closed_date_not_future", "closed_date IS NULL OR closed_date <= CURRENT_DATE");
                    table.CheckConstraint("chk_maintenance_requests_closed_date_status_consistency", "(status NOT IN ('closed', 'cancelled') AND closed_date IS NULL) OR (status IN ('closed', 'cancelled') AND closed_date IS NOT NULL)");
                    table.CheckConstraint("chk_maintenance_requests_request_date_not_future", "request_date <= CURRENT_DATE");
                    table.CheckConstraint("chk_maintenance_requests_title_not_blank", "length(btrim(title)) > 0");
                    table.ForeignKey(
                        name: "fk_maintenance_requests_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_request_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_request_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_request_id",
                        column: x => x.maintenance_request_id,
                        principalTable: "maintenance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_request_attachments_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_request_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment_text = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_maintenance_request_comments", x => x.id);
                    table.CheckConstraint("chk_maintenance_request_comments_text_not_blank", "length(btrim(comment_text)) > 0");
                    table.ForeignKey(
                        name: "fk_maintenance_request_comments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_request_comments_request_id",
                        column: x => x.maintenance_request_id,
                        principalTable: "maintenance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_request_comments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_request_comments_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_request_comments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<int>(type: "maintenance_status_enum", nullable: true),
                    new_status = table.Column<int>(type: "maintenance_status_enum", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_status_history", x => x.id);
                    table.CheckConstraint("chk_maintenance_status_history_no_noop", "previous_status IS NULL OR previous_status != new_status");
                    table.ForeignKey(
                        name: "fk_maintenance_status_history_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_status_history_requests_request_id",
                        column: x => x.maintenance_request_id,
                        principalTable: "maintenance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_status_history_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_request_attachments_request_id",
                table: "maintenance_request_attachments",
                column: "maintenance_request_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_company_id",
                table: "maintenance_request_attachments",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_created_by",
                table: "maintenance_request_attachments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_deleted_by",
                table: "maintenance_request_attachments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_updated_by",
                table: "maintenance_request_attachments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_attachments_uploaded_by",
                table: "maintenance_request_attachments",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "uq_maintenance_request_attachments_request_file",
                table: "maintenance_request_attachments",
                columns: new[] { "maintenance_request_id", "file_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_request_comments_request_created_at",
                table: "maintenance_request_comments",
                columns: new[] { "maintenance_request_id", "created_at" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_comments_company_id",
                table: "maintenance_request_comments",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_comments_created_by",
                table: "maintenance_request_comments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_comments_deleted_by",
                table: "maintenance_request_comments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_request_comments_updated_by",
                table: "maintenance_request_comments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_apartment_request_date",
                table: "maintenance_requests",
                columns: new[] { "apartment_id", "request_date" },
                descending: new[] { false, true },
                filter: "apartment_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_building_request_date",
                table: "maintenance_requests",
                columns: new[] { "building_id", "request_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_company_category",
                table: "maintenance_requests",
                columns: new[] { "company_id", "category" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_company_open_priority",
                table: "maintenance_requests",
                columns: new[] { "company_id", "priority", "request_date" },
                descending: new[] { false, false, true },
                filter: "status IN ('open', 'in_progress', 'waiting') AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_company_status_request_date",
                table: "maintenance_requests",
                columns: new[] { "company_id", "status", "request_date" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_tenant_request_date",
                table: "maintenance_requests",
                columns: new[] { "tenant_id", "request_date" },
                descending: new[] { false, true },
                filter: "tenant_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_requests_title_trgm",
                table: "maintenance_requests",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_created_by",
                table: "maintenance_requests",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_deleted_by",
                table: "maintenance_requests",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_requests_updated_by",
                table: "maintenance_requests",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_status_history_company_new_status",
                table: "maintenance_status_history",
                columns: new[] { "company_id", "new_status", "changed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_status_history_request_changed_at",
                table: "maintenance_status_history",
                columns: new[] { "maintenance_request_id", "changed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_status_history_changed_by",
                table: "maintenance_status_history",
                column: "changed_by");

            migrationBuilder.Sql(@"
                -- Enable Row Level Security (RLS) on all Module 8 tables
                ALTER TABLE maintenance_requests ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_requests FORCE ROW LEVEL SECURITY;
                CREATE POLICY maintenance_requests_tenant_isolation_policy ON maintenance_requests
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE maintenance_request_attachments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_request_attachments FORCE ROW LEVEL SECURITY;
                CREATE POLICY maintenance_request_attachments_tenant_isolation_policy ON maintenance_request_attachments
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE maintenance_request_comments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_request_comments FORCE ROW LEVEL SECURITY;
                CREATE POLICY maintenance_request_comments_tenant_isolation_policy ON maintenance_request_comments
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE maintenance_status_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE maintenance_status_history FORCE ROW LEVEL SECURITY;
                CREATE POLICY maintenance_status_history_tenant_isolation_policy ON maintenance_status_history
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_request_attachments");

            migrationBuilder.DropTable(
                name: "maintenance_request_comments");

            migrationBuilder.DropTable(
                name: "maintenance_status_history");

            migrationBuilder.DropTable(
                name: "maintenance_requests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:allocation_status_enum", "active,reversed")
                .Annotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .Annotation("Npgsql:Enum:cheque_status_enum", "issued,received,deposited,cleared,bounced,cancelled")
                .Annotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .Annotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
                .Annotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .Annotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .Annotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .Annotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .Annotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .Annotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .Annotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .Annotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .Annotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .Annotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .Annotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .Annotation("Npgsql:Enum:payment_frequency_enum", "monthly,quarterly,semi_annual,annual")
                .Annotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom")
                .Annotation("Npgsql:Enum:payment_purpose_enum", "scheduled_installment,unallocated_receipt,adjustment")
                .Annotation("Npgsql:Enum:receipt_reset_policy_enum", "never,yearly,monthly")
                .Annotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .Annotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:allocation_status_enum", "active,reversed")
                .OldAnnotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .OldAnnotation("Npgsql:Enum:cheque_status_enum", "issued,received,deposited,cleared,bounced,cancelled")
                .OldAnnotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .OldAnnotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
                .OldAnnotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .OldAnnotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .OldAnnotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .OldAnnotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .OldAnnotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .OldAnnotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:maintenance_category_enum", "electrical,plumbing,air_conditioning,elevator,cleaning,water,structural,doors_windows,internet,other")
                .OldAnnotation("Npgsql:Enum:maintenance_priority_enum", "low,medium,high,emergency")
                .OldAnnotation("Npgsql:Enum:maintenance_status_enum", "open,in_progress,waiting,resolved,closed,cancelled")
                .OldAnnotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .OldAnnotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .OldAnnotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .OldAnnotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .OldAnnotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .OldAnnotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .OldAnnotation("Npgsql:Enum:payment_frequency_enum", "monthly,quarterly,semi_annual,annual")
                .OldAnnotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom")
                .OldAnnotation("Npgsql:Enum:payment_purpose_enum", "scheduled_installment,unallocated_receipt,adjustment")
                .OldAnnotation("Npgsql:Enum:receipt_reset_policy_enum", "never,yearly,monthly")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .OldAnnotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}

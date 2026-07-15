using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module5_Phase2_LeaseContracts_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .Annotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .Annotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
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
                .Annotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .Annotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .OldAnnotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .OldAnnotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .OldAnnotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .OldAnnotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .OldAnnotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .OldAnnotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_apartments_company_id_id",
                table: "apartments",
                columns: new[] { "company_id", "id" });

            migrationBuilder.CreateTable(
                name: "lease_contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prior_contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    legal_regime = table.Column<int>(type: "legal_regime_enum", nullable: false, defaultValueSql: "'standard'"),
                    tenant_type = table.Column<int>(type: "tenant_type_enum", nullable: false, defaultValueSql: "'personal'"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    signed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    monthly_rent_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    security_deposit_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 0m),
                    payment_frequency = table.Column<int>(type: "payment_frequency_enum", nullable: false, defaultValueSql: "'monthly'"),
                    payment_due_day = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    status = table.Column<int>(type: "contract_status_enum", nullable: false, defaultValueSql: "'draft'"),
                    external_registration_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contract_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_lease_contracts", x => x.id);
                    table.UniqueConstraint("uq_lease_contracts_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_lease_contracts_dates", "end_date > start_date");
                    table.CheckConstraint("chk_lease_contracts_deposit_nonneg", "security_deposit_amount >= 0");
                    table.CheckConstraint("chk_lease_contracts_monthly_rent_positive", "monthly_rent_amount > 0");
                    table.CheckConstraint("chk_lease_contracts_no_self_reference", "prior_contract_id IS NULL OR prior_contract_id != id");
                    table.CheckConstraint("chk_lease_contracts_payment_due_day", "payment_due_day BETWEEN 1 AND 28");
                    table.CheckConstraint("chk_lease_contracts_signed_date", "signed_date IS NULL OR signed_date <= end_date");
                    table.ForeignKey(
                        name: "fk_lease_contracts_apartments_company_apartment",
                        columns: x => new { x.company_id, x.apartment_id },
                        principalTable: "apartments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lease_contracts_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lease_contracts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lease_contracts_self_prior_contract",
                        columns: x => new { x.company_id, x.prior_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lease_contracts_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lease_contracts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lease_contracts_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_lease_contracts_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contract_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<int>(type: "contract_document_type_enum", nullable: false, defaultValueSql: "'other'"),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_documents_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_documents_lease_contracts_company_contract",
                        columns: x => new { x.company_id, x.lease_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_documents_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_contract_documents_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contract_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<int>(type: "contract_status_enum", nullable: true),
                    new_status = table.Column<int>(type: "contract_status_enum", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_status_history", x => x.id);
                    table.CheckConstraint("chk_contract_status_history_no_noop", "previous_status IS NULL OR previous_status != new_status");
                    table.ForeignKey(
                        name: "fk_contract_status_history_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_status_history_lease_contracts_company_contract",
                        columns: x => new { x.company_id, x.lease_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_status_history_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contract_terminations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    termination_type = table.Column<int>(type: "termination_type_enum", nullable: false),
                    termination_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    outstanding_balance = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 0m),
                    deposit_returned_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 0m),
                    deposit_deduction_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 0m),
                    deposit_deduction_reason = table.Column<string>(type: "text", nullable: true),
                    final_utility_settlement_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_terminations", x => x.id);
                    table.CheckConstraint("chk_contract_terminations_balances_nonneg", "outstanding_balance >= 0 AND deposit_returned_amount >= 0 AND deposit_deduction_amount >= 0");
                    table.CheckConstraint("chk_contract_terminations_date_not_future", "termination_date <= CURRENT_DATE");
                    table.CheckConstraint("chk_contract_terminations_deduction_reason", "deposit_deduction_amount = 0 OR deposit_deduction_reason IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_contract_terminations_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_terminations_lease_contracts_company_contract",
                        columns: x => new { x.company_id, x.lease_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_terminations_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_contract_terminations_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_contract_terminations_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_contract_terminations_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parking_assignments_company_id_lease_contract_id",
                table: "parking_assignments",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "idx_contract_documents_lease_contract_id",
                table: "contract_documents",
                column: "lease_contract_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_contract_documents_type",
                table: "contract_documents",
                columns: new[] { "lease_contract_id", "document_type" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_company_id_lease_contract_id",
                table: "contract_documents",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_deleted_by",
                table: "contract_documents",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_uploaded_by",
                table: "contract_documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "uq_contract_documents_contract_file",
                table: "contract_documents",
                columns: new[] { "lease_contract_id", "file_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_contract_status_history_company_new_status",
                table: "contract_status_history",
                columns: new[] { "company_id", "new_status", "changed_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_contract_status_history_contract_changed_at",
                table: "contract_status_history",
                columns: new[] { "lease_contract_id", "changed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_contract_status_history_changed_by",
                table: "contract_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_contract_status_history_company_id_lease_contract_id",
                table: "contract_status_history",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_terminations_approved_by",
                table: "contract_terminations",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_contract_terminations_company_id_lease_contract_id",
                table: "contract_terminations",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_terminations_created_by",
                table: "contract_terminations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_contract_terminations_deleted_by",
                table: "contract_terminations",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_contract_terminations_updated_by",
                table: "contract_terminations",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_contract_terminations_lease_contract_id",
                table: "contract_terminations",
                column: "lease_contract_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_lease_contracts_apartment_history",
                table: "lease_contracts",
                columns: new[] { "apartment_id", "start_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_lease_contracts_company_status",
                table: "lease_contracts",
                columns: new[] { "company_id", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_lease_contracts_expiration",
                table: "lease_contracts",
                columns: new[] { "company_id", "end_date" },
                filter: "status = 'active' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_lease_contracts_tenant_history",
                table: "lease_contracts",
                columns: new[] { "tenant_id", "start_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_building_id",
                table: "lease_contracts",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_company_id_apartment_id",
                table: "lease_contracts",
                columns: new[] { "company_id", "apartment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_company_id_prior_contract_id",
                table: "lease_contracts",
                columns: new[] { "company_id", "prior_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_company_id_tenant_id",
                table: "lease_contracts",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_created_by",
                table: "lease_contracts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_deleted_by",
                table: "lease_contracts",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_lease_contracts_updated_by",
                table: "lease_contracts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_lease_contracts_company_contract_number",
                table: "lease_contracts",
                columns: new[] { "company_id", "contract_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_lease_contracts_one_active_per_apartment",
                table: "lease_contracts",
                column: "apartment_id",
                unique: true,
                filter: "status = 'active' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_lease_contracts_prior_contract_id",
                table: "lease_contracts",
                column: "prior_contract_id",
                unique: true,
                filter: "prior_contract_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_parking_assignments_lease_contracts_company_contract",
                table: "parking_assignments",
                columns: new[] { "company_id", "lease_contract_id" },
                principalTable: "lease_contracts",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            // -------------------------------------------------------------------
            // RAW SQL: Trigram Index
            // -------------------------------------------------------------------
            migrationBuilder.Sql("CREATE INDEX idx_lease_contracts_contract_number_trgm ON lease_contracts USING gin (contract_number gin_trgm_ops);");

            // -------------------------------------------------------------------
            // RAW SQL: Occupancy Trigger
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION trg_lease_contracts_occupancy_update()
                RETURNS TRIGGER
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    -- Recompute for OLD.apartment_id if it changed
                    IF (TG_OP = 'UPDATE' AND OLD.apartment_id IS DISTINCT FROM NEW.apartment_id) THEN
                        UPDATE apartments
                        SET occupancy_status = CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM lease_contracts
                                WHERE apartment_id = OLD.apartment_id
                                  AND status = 'active'
                                  AND deleted_at IS NULL
                            ) THEN 'occupied'::occupancy_status_enum
                            ELSE 'vacant'::occupancy_status_enum
                        END
                        WHERE id = OLD.apartment_id;
                    END IF;

                    -- Recompute for NEW.apartment_id
                    UPDATE apartments
                    SET occupancy_status = CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM lease_contracts
                            WHERE apartment_id = NEW.apartment_id
                              AND status = 'active'
                              AND deleted_at IS NULL
                        ) THEN 'occupied'::occupancy_status_enum
                        ELSE 'vacant'::occupancy_status_enum
                    END
                    WHERE id = NEW.apartment_id;

                    RETURN NULL;
                END;
                $$;

                CREATE TRIGGER trg_lease_contracts_occupancy
                AFTER INSERT OR UPDATE OF status, deleted_at, apartment_id
                ON lease_contracts
                FOR EACH ROW
                EXECUTE FUNCTION trg_lease_contracts_occupancy_update();
            ");

            // -------------------------------------------------------------------
            // RAW SQL: RLS Policies
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
                ALTER TABLE lease_contracts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE lease_contracts FORCE ROW LEVEL SECURITY;
                CREATE POLICY lease_contracts_tenant_isolation_policy ON lease_contracts
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE contract_terminations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE contract_terminations FORCE ROW LEVEL SECURITY;
                CREATE POLICY contract_terminations_tenant_isolation_policy ON contract_terminations
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE contract_status_history ENABLE ROW LEVEL SECURITY;
                ALTER TABLE contract_status_history FORCE ROW LEVEL SECURITY;
                CREATE POLICY contract_status_history_tenant_isolation_policy ON contract_status_history
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE contract_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE contract_documents FORCE ROW LEVEL SECURITY;
                CREATE POLICY contract_documents_tenant_isolation_policy ON contract_documents
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");

            // -------------------------------------------------------------------
            // RAW SQL: Ownership & Privileges
            // -------------------------------------------------------------------
            migrationBuilder.Sql(@"
                ALTER TABLE lease_contracts OWNER TO propertyos_owner;
                ALTER TABLE contract_terminations OWNER TO propertyos_owner;
                ALTER TABLE contract_status_history OWNER TO propertyos_owner;
                ALTER TABLE contract_documents OWNER TO propertyos_owner;

                GRANT SELECT, INSERT ON lease_contracts, contract_terminations, contract_status_history, contract_documents TO propertyos_app;
                
                GRANT UPDATE (status, signed_date, external_registration_ref, contract_document_id, notes, updated_at, updated_by, deleted_at, deleted_by) ON lease_contracts TO propertyos_app;
                GRANT UPDATE (reason, notes, deposit_returned_amount, deposit_deduction_amount, deposit_deduction_reason, final_utility_settlement_completed, updated_at, updated_by, deleted_at, deleted_by) ON contract_terminations TO propertyos_app;
                GRANT UPDATE (document_type, description, updated_at, deleted_at, deleted_by) ON contract_documents TO propertyos_app;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS contract_documents_tenant_isolation_policy ON contract_documents;
                DROP POLICY IF EXISTS contract_status_history_tenant_isolation_policy ON contract_status_history;
                DROP POLICY IF EXISTS contract_terminations_tenant_isolation_policy ON contract_terminations;
                DROP POLICY IF EXISTS lease_contracts_tenant_isolation_policy ON lease_contracts;
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_lease_contracts_occupancy ON lease_contracts;
                DROP FUNCTION IF EXISTS trg_lease_contracts_occupancy_update();
            ");

            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_lease_contracts_contract_number_trgm;");

            migrationBuilder.DropForeignKey(
                name: "fk_parking_assignments_lease_contracts_company_contract",
                table: "parking_assignments");

            migrationBuilder.DropTable(
                name: "contract_documents");

            migrationBuilder.DropTable(
                name: "contract_status_history");

            migrationBuilder.DropTable(
                name: "contract_terminations");

            migrationBuilder.DropTable(
                name: "lease_contracts");

            migrationBuilder.DropIndex(
                name: "IX_parking_assignments_company_id_lease_contract_id",
                table: "parking_assignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_apartments_company_id_id",
                table: "apartments");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .Annotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .Annotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .Annotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .Annotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .Annotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .Annotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .Annotation("Npgsql:Enum:occupancy_status_enum", "vacant,occupied,under_maintenance,listed")
                .Annotation("Npgsql:Enum:otp_purpose_enum", "login,phone_verification")
                .Annotation("Npgsql:Enum:ownership_status_enum", "company_owned,third_party_owned")
                .Annotation("Npgsql:Enum:parking_assignment_status_enum", "active,ended")
                .Annotation("Npgsql:Enum:parking_type_enum", "standard,covered,visitor,disabled_access")
                .Annotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:audit_action_enum", "create,update,delete,soft_delete,restore,login,logout,permission_change,export,status_change")
                .OldAnnotation("Npgsql:Enum:audit_severity_enum", "info,warning,critical")
                .OldAnnotation("Npgsql:Enum:audit_source_enum", "api,web,mobile,system_job,admin_console")
                .OldAnnotation("Npgsql:Enum:billing_cycle_enum", "monthly,yearly")
                .OldAnnotation("Npgsql:Enum:building_type_enum", "residential,commercial,mixed_use")
                .OldAnnotation("Npgsql:Enum:company_type_enum", "individual_owner,property_management_company,investment_company")
                .OldAnnotation("Npgsql:Enum:contract_document_type_enum", "signed_contract,national_id_copy,passport,income_proof,other")
                .OldAnnotation("Npgsql:Enum:contract_status_enum", "draft,pending_signature,active,expired,renewed,terminated,cancelled,superseded")
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
                .OldAnnotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .OldAnnotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .OldAnnotation("Npgsql:Enum:tenant_type_enum", "personal,corporate")
                .OldAnnotation("Npgsql:Enum:termination_type_enum", "normal_expiration,early_termination,mutual_agreement,tenant_request,owner_request,legal_eviction")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}

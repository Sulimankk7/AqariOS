using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module6_Module7_Financials_Consolidated : Migration
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

            migrationBuilder.CreateTable(
                name: "company_receipt_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValueSql: "''"),
                    current_number = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    padding_length = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)5),
                    reset_policy = table.Column<int>(type: "receipt_reset_policy_enum", nullable: false, defaultValueSql: "'never'"),
                    last_reset_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_receipt_sequences", x => x.id);
                    table.UniqueConstraint("uq_company_receipt_sequences_company_id", x => x.company_id);
                    table.CheckConstraint("chk_company_receipt_sequences_current_number_nonneg", "current_number >= 0");
                    table.CheckConstraint("chk_company_receipt_sequences_padding_length", "padding_length BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "fk_company_receipt_sequences_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<int>(type: "expense_category_enum", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_method = table.Column<int>(type: "expense_payment_method_enum", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.UniqueConstraint("uq_expenses_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_expenses_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_expenses_building_overhead", "(building_id IS NOT NULL) OR (building_id IS NULL)");
                    table.CheckConstraint("chk_expenses_expense_date_not_future", "expense_date <= CURRENT_DATE");
                    table.ForeignKey(
                        name: "fk_expenses_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expenses_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expenses_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rent_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_period_start = table.Column<DateOnly>(type: "date", nullable: true),
                    billing_period_end = table.Column<DateOnly>(type: "date", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    amount_due = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    payment_purpose = table.Column<int>(type: "payment_purpose_enum", nullable: false, defaultValueSql: "'scheduled_installment'"),
                    payment_method = table.Column<int>(type: "payment_method_enum", nullable: true),
                    payment_reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    due_date_status = table.Column<int>(type: "due_date_status_enum", nullable: false, defaultValueSql: "'pending'"),
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
                    table.PrimaryKey("PK_rent_payments", x => x.id);
                    table.UniqueConstraint("uq_rent_payments_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_rent_payments_amount_due_nonneg", "amount_due >= 0");
                    table.CheckConstraint("chk_rent_payments_amount_paid_nonneg", "amount_paid >= 0");
                    table.CheckConstraint("chk_rent_payments_period_dates", "billing_period_end IS NULL OR billing_period_start IS NULL OR billing_period_end > billing_period_start");
                    table.CheckConstraint("chk_rent_payments_scheduled_period_purpose", "(payment_purpose = 'scheduled_installment' AND billing_period_start IS NOT NULL AND billing_period_end IS NOT NULL AND due_date IS NOT NULL) OR (payment_purpose IN ('unallocated_receipt', 'adjustment') AND billing_period_start IS NULL AND billing_period_end IS NULL)");
                    table.ForeignKey(
                        name: "fk_rent_payments_apartments_company_apartment",
                        columns: x => new { x.company_id, x.apartment_id },
                        principalTable: "apartments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payments_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payments_lease_contracts_company_contract",
                        columns: x => new { x.company_id, x.lease_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payments_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payments_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rent_payments_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rent_payments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "expense_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    issued_at = table.Column<DateOnly>(type: "date", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
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
                    table.PrimaryKey("PK_expense_receipts", x => x.id);
                    table.UniqueConstraint("uq_expense_receipts_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_expense_receipts_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_expense_receipts_issued_at_not_future", "issued_at <= CURRENT_DATE");
                    table.ForeignKey(
                        name: "fk_expense_receipts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expense_receipts_expenses_company_expense",
                        columns: x => new { x.company_id, x.expense_id },
                        principalTable: "expenses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expense_receipts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expense_receipts_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expense_receipts_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_expense_receipts_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cheque_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rent_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cheque_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    bank_branch = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    status = table.Column<int>(type: "cheque_status_enum", nullable: false, defaultValueSql: "'issued'"),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    clearance_date = table.Column<DateOnly>(type: "date", nullable: true),
                    bounce_date = table.Column<DateOnly>(type: "date", nullable: true),
                    bounce_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bounce_fee_charged = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    replacement_cheque_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_cheque_details", x => x.id);
                    table.UniqueConstraint("uq_cheque_details_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_cheque_details_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_cheque_details_bounce_requires_deposit", "bounce_date IS NULL OR deposit_date IS NOT NULL");
                    table.CheckConstraint("chk_cheque_details_clearance_after_deposit", "clearance_date IS NULL OR clearance_date >= deposit_date");
                    table.CheckConstraint("chk_cheque_details_clearance_requires_deposit", "clearance_date IS NULL OR deposit_date IS NOT NULL");
                    table.CheckConstraint("chk_cheque_details_deposit_after_received", "deposit_date IS NULL OR deposit_date >= received_date");
                    table.CheckConstraint("chk_cheque_details_deposit_requires_received", "deposit_date IS NULL OR received_date IS NOT NULL");
                    table.CheckConstraint("chk_cheque_details_due_after_issue", "due_date >= issue_date");
                    table.CheckConstraint("chk_cheque_details_received_after_issue", "received_date IS NULL OR received_date >= issue_date");
                    table.CheckConstraint("chk_cheque_details_status_date_consistency", "(status = 'issued') OR (status = 'received' AND received_date IS NOT NULL) OR (status = 'deposited' AND deposit_date IS NOT NULL) OR (status = 'cleared' AND clearance_date IS NOT NULL) OR (status = 'bounced' AND bounce_date IS NOT NULL AND bounce_reason IS NOT NULL) OR (status = 'cancelled' AND cancellation_reason IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_cheque_details_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cheque_details_lease_contracts_company_contract",
                        columns: x => new { x.company_id, x.lease_contract_id },
                        principalTable: "lease_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cheque_details_rent_payments_company_payment",
                        columns: x => new { x.company_id, x.rent_payment_id },
                        principalTable: "rent_payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cheque_details_self_replacement_cheque",
                        columns: x => new { x.company_id, x.replacement_cheque_id },
                        principalTable: "cheque_details",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cheque_details_tenants_company_tenant",
                        columns: x => new { x.company_id, x.tenant_id },
                        principalTable: "tenants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cheque_details_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cheque_details_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cheque_details_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "efawateercom_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rent_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    request_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    response_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    transaction_status = table.Column<int>(type: "efawateercom_status_enum", nullable: false, defaultValueSql: "'pending'"),
                    response_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    response_message = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
                    raw_response = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_efawateercom_transactions", x => x.id);
                    table.UniqueConstraint("uq_efawateercom_transactions_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_efawateercom_transactions_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_efawateercom_transactions_response_time_after_request", "response_time IS NULL OR response_time >= request_time");
                    table.CheckConstraint("chk_efawateercom_transactions_status_consistency", "(transaction_status IN ('pending', 'sent')) OR (transaction_status IN ('success', 'failed', 'timeout', 'cancelled') AND response_time IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_efawateercom_transactions_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_efawateercom_transactions_rent_payments_company_payment",
                        columns: x => new { x.company_id, x.rent_payment_id },
                        principalTable: "rent_payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_efawateercom_transactions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_efawateercom_transactions_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_efawateercom_transactions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    allocation_status = table.Column<int>(type: "allocation_status_enum", nullable: false, defaultValueSql: "'active'"),
                    reversal_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_payment_allocations", x => x.id);
                    table.CheckConstraint("chk_payment_allocations_amount_positive", "allocated_amount > 0");
                    table.CheckConstraint("chk_payment_allocations_not_self_referencing", "receiving_payment_id != obligation_payment_id");
                    table.CheckConstraint("chk_payment_allocations_reversal_fields", "(allocation_status = 'active' AND reversal_reason IS NULL AND reversed_at IS NULL) OR (allocation_status = 'reversed' AND reversal_reason IS NOT NULL AND reversed_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_payment_allocations_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_rent_payments_obligation_payment",
                        columns: x => new { x.company_id, x.obligation_payment_id },
                        principalTable: "rent_payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_rent_payments_receiving_payment",
                        columns: x => new { x.company_id, x.receiving_payment_id },
                        principalTable: "rent_payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_allocations_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_allocations_users_reversed_by",
                        column: x => x.reversed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_allocations_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rent_payment_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rent_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", maxLength: 3, nullable: false, defaultValueSql: "'JOD'"),
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
                    table.PrimaryKey("PK_rent_payment_receipts", x => x.id);
                    table.UniqueConstraint("uq_rent_payment_receipts_company_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("chk_rent_payment_receipts_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_rent_payment_receipts_issue_date_not_future", "issue_date <= CURRENT_DATE");
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_rent_payments_company_payment",
                        columns: x => new { x.company_id, x.rent_payment_id },
                        principalTable: "rent_payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_users_issued_by",
                        column: x => x.issued_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_rent_payment_receipts_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_cheque_details_company_status_due_date",
                table: "cheque_details",
                columns: new[] { "company_id", "status", "due_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_cheque_details_lease_contract_id",
                table: "cheque_details",
                columns: new[] { "lease_contract_id", "due_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_cheque_details_tenant_id",
                table: "cheque_details",
                column: "tenant_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_company_id_lease_contract_id",
                table: "cheque_details",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_company_id_rent_payment_id",
                table: "cheque_details",
                columns: new[] { "company_id", "rent_payment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_company_id_replacement_cheque_id",
                table: "cheque_details",
                columns: new[] { "company_id", "replacement_cheque_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_company_id_tenant_id",
                table: "cheque_details",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_created_by",
                table: "cheque_details",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_deleted_by",
                table: "cheque_details",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_updated_by",
                table: "cheque_details",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_cheque_details_company_cheque_number",
                table: "cheque_details",
                columns: new[] { "company_id", "cheque_number", "bank_name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_cheque_details_rent_payment_id",
                table: "cheque_details",
                column: "rent_payment_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_efawateercom_transactions_company_status_request_time",
                table: "efawateercom_transactions",
                columns: new[] { "company_id", "transaction_status", "request_time" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_efawateercom_transactions_payment_reference",
                table: "efawateercom_transactions",
                column: "payment_reference",
                filter: "payment_reference IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_efawateercom_transactions_rent_payment_id",
                table: "efawateercom_transactions",
                column: "rent_payment_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_efawateercom_transactions_company_id_rent_payment_id",
                table: "efawateercom_transactions",
                columns: new[] { "company_id", "rent_payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_efawateercom_transactions_created_by",
                table: "efawateercom_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_efawateercom_transactions_deleted_by",
                table: "efawateercom_transactions",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_efawateercom_transactions_updated_by",
                table: "efawateercom_transactions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_efawateercom_transactions_external_transaction_id",
                table: "efawateercom_transactions",
                column: "external_transaction_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expense_receipts_company_issued_at",
                table: "expense_receipts",
                columns: new[] { "company_id", "issued_at" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expense_receipts_expense_id",
                table: "expense_receipts",
                column: "expense_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_company_id_expense_id",
                table: "expense_receipts",
                columns: new[] { "company_id", "expense_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_created_by",
                table: "expense_receipts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_deleted_by",
                table: "expense_receipts",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_updated_by",
                table: "expense_receipts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_expense_receipts_uploaded_by",
                table: "expense_receipts",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "uq_expense_receipts_company_receipt_number",
                table: "expense_receipts",
                columns: new[] { "company_id", "receipt_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_expense_receipts_expense_file",
                table: "expense_receipts",
                columns: new[] { "expense_id", "file_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_building_date",
                table: "expenses",
                columns: new[] { "building_id", "expense_date" },
                descending: new[] { false, true },
                filter: "building_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_company_category_date",
                table: "expenses",
                columns: new[] { "company_id", "category", "expense_date" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_company_date",
                table: "expenses",
                columns: new[] { "company_id", "expense_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_invoice_number",
                table: "expenses",
                column: "invoice_number",
                filter: "invoice_number IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_expenses_vendor_trgm",
                table: "expenses",
                column: "vendor_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_created_by",
                table: "expenses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_deleted_by",
                table: "expenses",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_updated_by",
                table: "expenses",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_payment_allocations_company_allocation_date",
                table: "payment_allocations",
                columns: new[] { "company_id", "allocation_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_payment_allocations_obligation_payment_id",
                table: "payment_allocations",
                column: "obligation_payment_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_payment_allocations_receiving_payment_id",
                table: "payment_allocations",
                column: "receiving_payment_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_payment_allocations_status",
                table: "payment_allocations",
                columns: new[] { "company_id", "allocation_status" },
                filter: "allocation_status = 'reversed' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_company_id_obligation_payment_id",
                table: "payment_allocations",
                columns: new[] { "company_id", "obligation_payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_company_id_receiving_payment_id",
                table: "payment_allocations",
                columns: new[] { "company_id", "receiving_payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_created_by",
                table: "payment_allocations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_deleted_by",
                table: "payment_allocations",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_reversed_by",
                table: "payment_allocations",
                column: "reversed_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_updated_by",
                table: "payment_allocations",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payment_receipts_company_issue_date",
                table: "rent_payment_receipts",
                columns: new[] { "company_id", "issue_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_receipts_company_id_rent_payment_id",
                table: "rent_payment_receipts",
                columns: new[] { "company_id", "rent_payment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_receipts_created_by",
                table: "rent_payment_receipts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_receipts_deleted_by",
                table: "rent_payment_receipts",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_receipts_issued_by",
                table: "rent_payment_receipts",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payment_receipts_updated_by",
                table: "rent_payment_receipts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_rent_payment_receipts_company_receipt_number",
                table: "rent_payment_receipts",
                columns: new[] { "company_id", "receipt_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_rent_payment_receipts_rent_payment_id",
                table: "rent_payment_receipts",
                column: "rent_payment_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_apartment_due_date",
                table: "rent_payments",
                columns: new[] { "apartment_id", "due_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_company_status_due_date",
                table: "rent_payments",
                columns: new[] { "company_id", "due_date_status", "due_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_contract_due_date",
                table: "rent_payments",
                columns: new[] { "lease_contract_id", "due_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_late_overdue",
                table: "rent_payments",
                columns: new[] { "company_id", "due_date" },
                filter: "due_date_status IN ('late', 'overdue_unpaid') AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_receipt_number",
                table: "rent_payments",
                column: "receipt_number",
                filter: "receipt_number IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_rent_payments_tenant_due_date",
                table: "rent_payments",
                columns: new[] { "tenant_id", "due_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_building_id",
                table: "rent_payments",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_company_id_apartment_id",
                table: "rent_payments",
                columns: new[] { "company_id", "apartment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_company_id_lease_contract_id",
                table: "rent_payments",
                columns: new[] { "company_id", "lease_contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_company_id_tenant_id",
                table: "rent_payments",
                columns: new[] { "company_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_created_by",
                table: "rent_payments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_deleted_by",
                table: "rent_payments",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_rent_payments_updated_by",
                table: "rent_payments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_rent_payments_contract_period",
                table: "rent_payments",
                columns: new[] { "lease_contract_id", "billing_period_start", "billing_period_end" },
                unique: true,
                filter: "deleted_at IS NULL AND payment_purpose = 'scheduled_installment'");

            migrationBuilder.Sql(@"
                -- RLS Policies for Module 7 tables
                ALTER TABLE payment_allocations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE payment_allocations FORCE ROW LEVEL SECURITY;
                CREATE POLICY payment_allocations_tenant_isolation_policy ON payment_allocations
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE expenses ENABLE ROW LEVEL SECURITY;
                ALTER TABLE expenses FORCE ROW LEVEL SECURITY;
                CREATE POLICY expenses_tenant_isolation_policy ON expenses
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE rent_payments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE rent_payments FORCE ROW LEVEL SECURITY;
                CREATE POLICY rent_payments_tenant_isolation_policy ON rent_payments
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE expense_receipts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE expense_receipts FORCE ROW LEVEL SECURITY;
                CREATE POLICY expense_receipts_tenant_isolation_policy ON expense_receipts
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE cheque_details ENABLE ROW LEVEL SECURITY;
                ALTER TABLE cheque_details FORCE ROW LEVEL SECURITY;
                CREATE POLICY cheque_details_tenant_isolation_policy ON cheque_details
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE company_receipt_sequences ENABLE ROW LEVEL SECURITY;
                ALTER TABLE company_receipt_sequences FORCE ROW LEVEL SECURITY;
                CREATE POLICY company_receipt_sequences_tenant_isolation_policy ON company_receipt_sequences
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE efawateercom_transactions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE efawateercom_transactions FORCE ROW LEVEL SECURITY;
                CREATE POLICY efawateercom_transactions_tenant_isolation_policy ON efawateercom_transactions
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE rent_payment_receipts ENABLE ROW LEVEL SECURITY;
                ALTER TABLE rent_payment_receipts FORCE ROW LEVEL SECURITY;
                CREATE POLICY rent_payment_receipts_tenant_isolation_policy ON rent_payment_receipts
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cheque_details");

            migrationBuilder.DropTable(
                name: "company_receipt_sequences");

            migrationBuilder.DropTable(
                name: "efawateercom_transactions");

            migrationBuilder.DropTable(
                name: "expense_receipts");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "rent_payment_receipts");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "rent_payments");

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
        }
    }
}

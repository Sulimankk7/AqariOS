using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module11_Notifications : Migration
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
                .Annotation("Npgsql:Enum:currency_code_enum", "jod,usd,eur,aed,sar")
                .Annotation("Npgsql:Enum:delivery_channel_enum", "email,sms,whats_app,in_app")
                .Annotation("Npgsql:Enum:delivery_status_enum", "pending,sent,delivered,failed")
                .Annotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .Annotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .Annotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .Annotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .Annotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .Annotation("Npgsql:Enum:listing_status_enum", "draft,published,rented,expired,archived")
                .Annotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .Annotation("Npgsql:Enum:maintenance_category_enum", "electrical,plumbing,air_conditioning,elevator,cleaning,water,structural,doors_windows,internet,other")
                .Annotation("Npgsql:Enum:maintenance_priority_enum", "low,medium,high,emergency")
                .Annotation("Npgsql:Enum:maintenance_status_enum", "open,in_progress,waiting,resolved,closed,cancelled")
                .Annotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .Annotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .Annotation("Npgsql:Enum:notification_priority_enum", "low,normal,high,critical")
                .Annotation("Npgsql:Enum:notification_status_enum", "pending,sent,failed,cancelled")
                .Annotation("Npgsql:Enum:notification_type_enum", "new_lease,lease_expiration,rent_due,rent_paid,late_payment,maintenance_request_created,maintenance_request_updated,marketplace_viewing_request,document_expiring,general_notification")
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
                .Annotation("Npgsql:Enum:viewing_request_status_enum", "pending,contacted,scheduled,completed,cancelled")
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
                .OldAnnotation("Npgsql:Enum:currency_code_enum", "jod,usd,eur,aed,sar")
                .OldAnnotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .OldAnnotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .OldAnnotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .OldAnnotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .OldAnnotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .OldAnnotation("Npgsql:Enum:listing_status_enum", "draft,published,rented,expired,archived")
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
                .OldAnnotation("Npgsql:Enum:viewing_request_status_enum", "pending,contacted,scheduled,completed,cancelled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    notification_type = table.Column<int>(type: "notification_type_enum", nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_notification_templates", x => x.id);
                    table.CheckConstraint("chk_notification_templates_body_not_blank", "length(btrim(body)) > 0");
                    table.CheckConstraint("chk_notification_templates_name_not_blank", "length(btrim(template_name)) > 0");
                    table.CheckConstraint("chk_notification_templates_subject_not_blank", "length(btrim(subject)) > 0");
                    table.ForeignKey(
                        name: "fk_notification_templates_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_templates_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_templates_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_templates_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<int>(type: "notification_type_enum", nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "notification_status_enum", nullable: false, defaultValueSql: "'pending'"),
                    priority = table.Column<int>(type: "notification_priority_enum", nullable: false, defaultValueSql: "'normal'"),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.CheckConstraint("chk_notifications_body_not_blank", "length(btrim(body)) > 0");
                    table.CheckConstraint("chk_notifications_read_at_after_created", "read_at IS NULL OR read_at >= created_at");
                    table.CheckConstraint("chk_notifications_read_at_not_future", "read_at IS NULL OR read_at <= now()");
                    table.CheckConstraint("chk_notifications_read_requires_sent", "read_at IS NULL OR status = 'sent'");
                    table.CheckConstraint("chk_notifications_subject_not_blank", "length(btrim(subject)) > 0");
                    table.ForeignKey(
                        name: "fk_notifications_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_notification_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "notification_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_channel = table.Column<int>(type: "delivery_channel_enum", nullable: false),
                    delivery_status = table.Column<int>(type: "delivery_status_enum", nullable: false, defaultValueSql: "'pending'"),
                    attempt_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.CheckConstraint("chk_notification_deliveries_attempt_count_nonneg", "attempt_count >= 0");
                    table.CheckConstraint("chk_notification_deliveries_delivered_after_sent", "delivered_at IS NULL OR sent_at IS NULL OR delivered_at >= sent_at");
                    table.CheckConstraint("chk_notification_deliveries_delivered_requires_delivered_at", "delivery_status != 'delivered' OR delivered_at IS NOT NULL");
                    table.CheckConstraint("chk_notification_deliveries_failed_requires_reason", "delivery_status != 'failed' OR failure_reason IS NOT NULL");
                    table.CheckConstraint("chk_notification_deliveries_sent_at_requires_status", "(delivery_status = 'pending' AND sent_at IS NULL) OR (delivery_status IN ('sent', 'delivered', 'failed') AND sent_at IS NOT NULL)");
                    table.CheckConstraint("chk_notification_deliveries_sent_requires_attempt", "delivery_status = 'pending' OR attempt_count > 0");
                    table.ForeignKey(
                        name: "fk_notification_deliveries_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_company_channel_created",
                table: "notification_deliveries",
                columns: new[] { "company_id", "delivery_channel", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_notification_deliveries_company_status_sent_at",
                table: "notification_deliveries",
                columns: new[] { "company_id", "delivery_status", "sent_at" },
                descending: new[] { false, false, true },
                filter: "delivery_status = 'failed'");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_created_by",
                table: "notification_deliveries",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_updated_by",
                table: "notification_deliveries",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_notification_deliveries_notification_channel",
                table: "notification_deliveries",
                columns: new[] { "notification_id", "delivery_channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_templates_company_type",
                table: "notification_templates",
                columns: new[] { "company_id", "notification_type" },
                filter: "deleted_at IS NULL AND is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_created_by",
                table: "notification_templates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_deleted_by",
                table: "notification_templates",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_updated_by",
                table: "notification_templates",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_company_name",
                table: "notification_templates",
                columns: new[] { "company_id", "template_name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_company_status_created",
                table: "notifications",
                columns: new[] { "company_id", "status", "created_at" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_company_type_created",
                table: "notifications",
                columns: new[] { "company_id", "notification_type", "created_at" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_recipient_unread",
                table: "notifications",
                columns: new[] { "recipient_user_id", "created_at" },
                descending: new[] { false, true },
                filter: "read_at IS NULL AND status = 'sent' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_subject_trgm",
                table: "notifications",
                column: "subject")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_by",
                table: "notifications",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_deleted_by",
                table: "notifications",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_template_id",
                table: "notifications",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_updated_by",
                table: "notifications",
                column: "updated_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "notification_templates");

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
                .Annotation("Npgsql:Enum:currency_code_enum", "jod,usd,eur,aed,sar")
                .Annotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .Annotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .Annotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .Annotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .Annotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .Annotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .Annotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .Annotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .Annotation("Npgsql:Enum:listing_status_enum", "draft,published,rented,expired,archived")
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
                .Annotation("Npgsql:Enum:viewing_request_status_enum", "pending,contacted,scheduled,completed,cancelled")
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
                .OldAnnotation("Npgsql:Enum:currency_code_enum", "jod,usd,eur,aed,sar")
                .OldAnnotation("Npgsql:Enum:delivery_channel_enum", "email,sms,whats_app,in_app")
                .OldAnnotation("Npgsql:Enum:delivery_status_enum", "pending,sent,delivered,failed")
                .OldAnnotation("Npgsql:Enum:due_date_status_enum", "pending,paid,partially_paid,late,overdue_unpaid,cancelled")
                .OldAnnotation("Npgsql:Enum:efawateercom_status_enum", "pending,sent,success,failed,timeout,cancelled")
                .OldAnnotation("Npgsql:Enum:expense_category_enum", "building,shared,emergency,utility_common_area,maintenance,cleaning,security,elevator,water_tank,generator,administrative,other")
                .OldAnnotation("Npgsql:Enum:expense_payment_method_enum", "cash,bank_transfer,cheque,other")
                .OldAnnotation("Npgsql:Enum:floor_type_enum", "basement,ground,regular,roof")
                .OldAnnotation("Npgsql:Enum:governorate_enum", "amman,zarqa,irbid,balqa,madaba,karak,tafilah,maan,aqaba,ajloun,jerash,mafraq")
                .OldAnnotation("Npgsql:Enum:late_fee_type_enum", "none,fixed,percentage")
                .OldAnnotation("Npgsql:Enum:legal_regime_enum", "standard,old_rent_law")
                .OldAnnotation("Npgsql:Enum:listing_status_enum", "draft,published,rented,expired,archived")
                .OldAnnotation("Npgsql:Enum:login_status_enum", "success,failed_password,failed_locked,failed_mfa,failed_not_found")
                .OldAnnotation("Npgsql:Enum:maintenance_category_enum", "electrical,plumbing,air_conditioning,elevator,cleaning,water,structural,doors_windows,internet,other")
                .OldAnnotation("Npgsql:Enum:maintenance_priority_enum", "low,medium,high,emergency")
                .OldAnnotation("Npgsql:Enum:maintenance_status_enum", "open,in_progress,waiting,resolved,closed,cancelled")
                .OldAnnotation("Npgsql:Enum:membership_status_enum", "invited_pending,active,suspended")
                .OldAnnotation("Npgsql:Enum:mfa_type_enum", "totp,sms")
                .OldAnnotation("Npgsql:Enum:notification_priority_enum", "low,normal,high,critical")
                .OldAnnotation("Npgsql:Enum:notification_status_enum", "pending,sent,failed,cancelled")
                .OldAnnotation("Npgsql:Enum:notification_type_enum", "new_lease,lease_expiration,rent_due,rent_paid,late_payment,maintenance_request_created,maintenance_request_updated,marketplace_viewing_request,document_expiring,general_notification")
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
                .OldAnnotation("Npgsql:Enum:viewing_request_status_enum", "pending,contacted,scheduled,completed,cancelled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}

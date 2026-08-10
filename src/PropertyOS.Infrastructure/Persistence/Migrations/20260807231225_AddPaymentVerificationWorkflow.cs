using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentVerificationWorkflow : Migration
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
                .Annotation("Npgsql:Enum:due_date_status_enum", "pending,pending_verification,paid,partially_paid,late,overdue_unpaid,cancelled")
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
                .Annotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom,cli_q")
                .Annotation("Npgsql:Enum:payment_purpose_enum", "scheduled_installment,unallocated_receipt,adjustment")
                .Annotation("Npgsql:Enum:receipt_reset_policy_enum", "never,yearly,monthly")
                .Annotation("Npgsql:Enum:revoke_reason_enum", "rotated,logout,theft_detected,admin_revoked,expired")
                .Annotation("Npgsql:Enum:subscription_status_enum", "trialing,active,past_due,suspended,cancelled,expired")
                .Annotation("Npgsql:Enum:submission_status_enum", "pending,approved,rejected")
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

            migrationBuilder.CreateTable(
                name: "payment_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rent_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<int>(type: "payment_method_enum", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    proof_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "submission_status_enum", nullable: false),
                    submitted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_payment_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_submissions_rent_payments_rent_payment_id",
                        column: x => x.rent_payment_id,
                        principalTable: "rent_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_submissions_company_id",
                table: "payment_submissions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_submissions_rent_payment_id",
                table: "payment_submissions",
                column: "rent_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_submissions_status",
                table: "payment_submissions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_submissions");

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
                .OldAnnotation("Npgsql:Enum:delivery_channel_enum", "email,sms,whats_app,in_app")
                .OldAnnotation("Npgsql:Enum:delivery_status_enum", "pending,sent,delivered,failed")
                .OldAnnotation("Npgsql:Enum:due_date_status_enum", "pending,pending_verification,paid,partially_paid,late,overdue_unpaid,cancelled")
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
                .OldAnnotation("Npgsql:Enum:payment_method_enum", "cash,bank_transfer,cheque,efawateercom,cli_q")
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

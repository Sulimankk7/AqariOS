using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module9_Marketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

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

            migrationBuilder.CreateTable(
                name: "marketplace_listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apartment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    listing_description = table.Column<string>(type: "text", nullable: false),
                    monthly_rent = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    security_deposit = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                    currency = table.Column<int>(type: "currency_code_enum", nullable: false, defaultValueSql: "'jod'"),
                    status = table.Column<int>(type: "listing_status_enum", nullable: false, defaultValueSql: "'draft'"),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contact_whatsapp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_marketplace_listings", x => x.id);
                    table.CheckConstraint("chk_marketplace_listings_contact_phone_not_blank", "length(btrim(contact_phone)) > 0");
                    table.CheckConstraint("chk_marketplace_listings_expiration_after_published", "expiration_date IS NULL OR published_date IS NULL OR expiration_date > published_date");
                    table.CheckConstraint("chk_marketplace_listings_monthly_rent_positive", "monthly_rent > 0");
                    table.CheckConstraint("chk_marketplace_listings_published_date_not_future", "published_date IS NULL OR published_date <= CURRENT_DATE");
                    table.CheckConstraint("chk_marketplace_listings_published_date_status_consistency", "(status = 'draft' AND published_date IS NULL) OR (status != 'draft' AND published_date IS NOT NULL)");
                    table.CheckConstraint("chk_marketplace_listings_security_deposit_nonneg", "security_deposit IS NULL OR security_deposit >= 0");
                    table.CheckConstraint("chk_marketplace_listings_title_not_blank", "length(btrim(listing_title)) > 0");
                    table.ForeignKey(
                        name: "fk_marketplace_listings_apartments_apartment_id",
                        column: x => x.apartment_id,
                        principalTable: "apartments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_marketplace_listings_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_marketplace_listings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listing_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<short>(type: "smallint", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_listing_images", x => x.id);
                    table.CheckConstraint("chk_listing_images_display_order_nonneg", "display_order >= 0");
                    table.ForeignKey(
                        name: "fk_listing_images_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_listing_images_listing_id",
                        column: x => x.listing_id,
                        principalTable: "marketplace_listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "viewing_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicant_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferred_viewing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    staff_notes = table.Column<string>(type: "text", nullable: true),
                    request_status = table.Column<int>(type: "viewing_request_status_enum", nullable: false, defaultValueSql: "'pending'"),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
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
                    table.PrimaryKey("PK_viewing_requests", x => x.id);
                    table.CheckConstraint("chk_viewing_requests_applicant_name_not_blank", "length(btrim(applicant_name)) > 0");
                    table.CheckConstraint("chk_viewing_requests_phone_not_blank", "length(btrim(phone_number)) > 0");
                    table.CheckConstraint("chk_viewing_requests_preferred_date_not_before_submission", "preferred_viewing_date IS NULL OR preferred_viewing_date >= (submitted_at AT TIME ZONE 'UTC')::date");
                    table.ForeignKey(
                        name: "fk_viewing_requests_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_viewing_requests_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "marketplace_listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_images_company_id",
                table: "listing_images",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_listing_images_listing_display_order",
                table: "listing_images",
                columns: new[] { "listing_id", "display_order" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_listing_images_one_cover",
                table: "listing_images",
                column: "listing_id",
                unique: true,
                filter: "is_cover = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_marketplace_listings_company_status_published",
                table: "marketplace_listings",
                columns: new[] { "company_id", "status", "published_date" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_marketplace_listings_public_browse",
                table: "marketplace_listings",
                columns: new[] { "is_featured", "published_date", "id" },
                descending: new[] { true, true, false },
                filter: "status = 'published' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_marketplace_listings_title_trgm",
                table: "marketplace_listings",
                column: "listing_title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_listings_building_id",
                table: "marketplace_listings",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "uq_marketplace_listings_one_active_per_apartment",
                table: "marketplace_listings",
                column: "apartment_id",
                unique: true,
                filter: "status = 'published' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_viewing_requests_company_status_submitted",
                table: "viewing_requests",
                columns: new[] { "company_id", "request_status", "submitted_at" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_viewing_requests_listing_submitted",
                table: "viewing_requests",
                columns: new[] { "listing_id", "submitted_at" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.Sql(@"
                -- 1. Enable RLS on marketplace_listings
                ALTER TABLE marketplace_listings ENABLE ROW LEVEL SECURITY;
                ALTER TABLE marketplace_listings FORCE ROW LEVEL SECURITY;

                -- Staff policy: Read/write scoped to their active company context
                CREATE POLICY marketplace_listings_staff_policy ON marketplace_listings
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                -- Public policy: Read-only access to published, active, non-expired, non-deleted listings
                CREATE POLICY marketplace_listings_public_select_policy ON marketplace_listings
                    FOR SELECT TO propertyos_app
                    USING (
                        status = 'published'::listing_status_enum
                        AND deleted_at IS NULL
                        AND (expiration_date IS NULL OR expiration_date >= CURRENT_DATE)
                    );

                -- 2. Enable RLS on listing_images
                ALTER TABLE listing_images ENABLE ROW LEVEL SECURITY;
                ALTER TABLE listing_images FORCE ROW LEVEL SECURITY;

                -- Staff policy
                CREATE POLICY listing_images_staff_policy ON listing_images
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                -- Public policy: Read-only access to images of published, active, non-expired, non-deleted listings
                CREATE POLICY listing_images_public_select_policy ON listing_images
                    FOR SELECT TO propertyos_app
                    USING (
                        deleted_at IS NULL
                        AND EXISTS (
                            SELECT 1 FROM marketplace_listings ml
                            WHERE ml.id = listing_images.listing_id
                              AND ml.status = 'published'::listing_status_enum
                              AND ml.deleted_at IS NULL
                              AND (ml.expiration_date IS NULL OR ml.expiration_date >= CURRENT_DATE)
                        )
                    );

                -- 3. Enable RLS on viewing_requests
                ALTER TABLE viewing_requests ENABLE ROW LEVEL SECURITY;
                ALTER TABLE viewing_requests FORCE ROW LEVEL SECURITY;

                -- Staff policy: Read/write scoped to their active company context
                CREATE POLICY viewing_requests_staff_policy ON viewing_requests
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                -- Public policy: Write-only (INSERT) access for any prospect to request a viewing
                CREATE POLICY viewing_requests_public_insert_policy ON viewing_requests
                    FOR INSERT TO propertyos_app
                    WITH CHECK (true);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_images");

            migrationBuilder.DropTable(
                name: "viewing_requests");

            migrationBuilder.DropTable(
                name: "marketplace_listings");

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
        }
    }
}

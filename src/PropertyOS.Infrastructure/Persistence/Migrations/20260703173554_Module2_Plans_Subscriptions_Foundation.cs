using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Module2_Plans_Subscriptions_Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'subscription_status_enum') THEN
        CREATE TYPE subscription_status_enum AS ENUM (
            'trialing', 'active', 'past_due', 'suspended', 'cancelled', 'expired'
        );
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'billing_cycle_enum') THEN
        CREATE TYPE billing_cycle_enum AS ENUM (
            'monthly', 'yearly'
        );
    END IF;
END $$;
");

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_ar = table.Column<string>(type: "text", nullable: true),
                    monthly_price = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    yearly_price = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "JOD"),
                    max_buildings = table.Column<int>(type: "integer", nullable: true),
                    max_users = table.Column<int>(type: "integer", nullable: true),
                    max_storage_mb = table.Column<int>(type: "integer", nullable: true),
                    feature_flags = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    supports_trial = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    trial_duration_days = table.Column<short>(type: "smallint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                    table.CheckConstraint("chk_subscription_plans_prices_positive", "monthly_price > 0 AND yearly_price > 0");
                    table.CheckConstraint("chk_subscription_plans_quotas_positive", "(max_buildings IS NULL OR max_buildings > 0) AND (max_users IS NULL OR max_users > 0) AND (max_storage_mb IS NULL OR max_storage_mb > 0)");
                    table.CheckConstraint("chk_subscription_plans_trial_duration", "trial_duration_days IS NOT NULL OR supports_trial = false");
                });

            migrationBuilder.CreateTable(
                name: "company_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "subscription_status_enum", nullable: false, defaultValueSql: "'trialing'::subscription_status_enum"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    trial_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    price_at_subscription = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    currency_at_subscription = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "JOD"),
                    billing_cycle = table.Column<int>(type: "billing_cycle_enum", nullable: false, defaultValueSql: "'monthly'::billing_cycle_enum"),
                    auto_renew = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    external_billing_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_subscriptions", x => x.id);
                    table.CheckConstraint("chk_company_subscriptions_dates", "end_date > start_date");
                    table.CheckConstraint("chk_company_subscriptions_suspension", "suspension_reason IS NOT NULL OR suspended_at IS NULL");
                    table.CheckConstraint("chk_company_subscriptions_trial_end", "trial_end_date IS NOT NULL OR status != 'trialing'");
                    table.ForeignKey(
                        name: "FK_company_subscriptions_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_company_subscriptions_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_company_subscriptions_company_status",
                table: "company_subscriptions",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_company_subscriptions_end_date",
                table: "company_subscriptions",
                column: "end_date",
                filter: "status IN ('trialing'::subscription_status_enum,'active'::subscription_status_enum,'past_due'::subscription_status_enum)");

            migrationBuilder.CreateIndex(
                name: "IX_company_subscriptions_plan_id",
                table: "company_subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_company_subscriptions_one_active",
                table: "company_subscriptions",
                column: "company_id",
                unique: true,
                filter: "status IN ('trialing'::subscription_status_enum,'active'::subscription_status_enum,'past_due'::subscription_status_enum)");

            migrationBuilder.CreateIndex(
                name: "idx_subscription_plans_active_sort",
                table: "subscription_plans",
                columns: new[] { "is_active", "sort_order" },
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plans_code",
                table: "subscription_plans",
                column: "code",
                unique: true);

            // Add updated_at triggers
            migrationBuilder.Sql(@"
CREATE TRIGGER set_subscription_plans_updated_at
    BEFORE UPDATE ON subscription_plans
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
");

            migrationBuilder.Sql(@"
CREATE TRIGGER set_company_subscriptions_updated_at
    BEFORE UPDATE ON company_subscriptions
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();
");

            // Row-Level Security — company_subscriptions
            migrationBuilder.Sql("ALTER TABLE company_subscriptions ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE company_subscriptions FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation_policy ON company_subscriptions
    AS PERMISSIVE FOR ALL
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_subscriptions");

            migrationBuilder.DropTable(
                name: "subscription_plans");


            migrationBuilder.Sql("DROP TYPE IF EXISTS billing_cycle_enum;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS subscription_status_enum;");
        }
    }
}

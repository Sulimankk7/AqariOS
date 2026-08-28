using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260827180000_NarrowPlatformSubscriptionRlsPolicies")]
public sealed class NarrowPlatformSubscriptionRlsPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS companies_platform_admin_update_policy ON companies;

            DROP POLICY IF EXISTS plan_change_requests_platform_all ON plan_change_requests;
            CREATE POLICY plan_change_requests_platform_select
                ON plan_change_requests FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY plan_change_requests_platform_update
                ON plan_change_requests FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            DROP POLICY IF EXISTS company_subscriptions_platform_admin_policy ON company_subscriptions;
            CREATE POLICY company_subscriptions_platform_select
                ON company_subscriptions FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY company_subscriptions_platform_insert
                ON company_subscriptions FOR INSERT TO propertyos_app
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY company_subscriptions_platform_update
                ON company_subscriptions FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS company_subscriptions_platform_update ON company_subscriptions;
            DROP POLICY IF EXISTS company_subscriptions_platform_insert ON company_subscriptions;
            DROP POLICY IF EXISTS company_subscriptions_platform_select ON company_subscriptions;
            CREATE POLICY company_subscriptions_platform_admin_policy
                ON company_subscriptions FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            DROP POLICY IF EXISTS plan_change_requests_platform_update ON plan_change_requests;
            DROP POLICY IF EXISTS plan_change_requests_platform_select ON plan_change_requests;
            CREATE POLICY plan_change_requests_platform_all
                ON plan_change_requests FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            CREATE POLICY companies_platform_admin_update_policy
                ON companies FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            """);
    }
}

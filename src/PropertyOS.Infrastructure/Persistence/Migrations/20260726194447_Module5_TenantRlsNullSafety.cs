using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The four Module 5 tenant-table policies used the non-null-safe
    /// current_setting('app.current_company_id') form: when no tenant context is set the
    /// comparison RAISES (unrecognized configuration parameter / invalid uuid "") instead
    /// of cleanly denying. Recreate them in the platform-standard fail-closed form
    /// NULLIF(current_setting(..., true), '')::uuid used by every other module's policies.
    /// </summary>
    public partial class Module5_TenantRlsNullSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenants;
                CREATE POLICY ""Tenant Isolation"" ON tenants FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_family_members;
                CREATE POLICY ""Tenant Isolation"" ON tenant_family_members FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_emergency_contacts;
                CREATE POLICY ""Tenant Isolation"" ON tenant_emergency_contacts FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_vehicles;
                CREATE POLICY ""Tenant Isolation"" ON tenant_vehicles FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenants;
                CREATE POLICY ""Tenant Isolation"" ON tenants FOR ALL TO propertyos_app
                    USING (company_id = current_setting('app.current_company_id')::uuid)
                    WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_family_members;
                CREATE POLICY ""Tenant Isolation"" ON tenant_family_members FOR ALL TO propertyos_app
                    USING (company_id = current_setting('app.current_company_id')::uuid)
                    WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_emergency_contacts;
                CREATE POLICY ""Tenant Isolation"" ON tenant_emergency_contacts FOR ALL TO propertyos_app
                    USING (company_id = current_setting('app.current_company_id')::uuid)
                    WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);

                DROP POLICY IF EXISTS ""Tenant Isolation"" ON tenant_vehicles;
                CREATE POLICY ""Tenant Isolation"" ON tenant_vehicles FOR ALL TO propertyos_app
                    USING (company_id = current_setting('app.current_company_id')::uuid)
                    WITH CHECK (company_id = current_setting('app.current_company_id')::uuid);
            ");
        }
    }
}

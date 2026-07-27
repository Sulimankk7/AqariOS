using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixCompanyRlsPlatformAdminBypass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE POLICY companies_platform_admin_select_policy
                ON companies
                FOR SELECT
                TO propertyos_app
                USING (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS companies_platform_admin_select_policy ON companies;
            ");
        }
    }
}

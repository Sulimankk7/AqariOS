using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260825120000_AddUtilityInvalidAccountSyncStatus")]
public partial class AddUtilityInvalidAccountSyncStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TYPE utility_sync_status_enum ADD VALUE IF NOT EXISTS 'invalid_account';",
            suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // PostgreSQL cannot safely remove one enum value in place. The label is
        // intentionally retained during downgrade, matching existing enum migrations.
    }
}

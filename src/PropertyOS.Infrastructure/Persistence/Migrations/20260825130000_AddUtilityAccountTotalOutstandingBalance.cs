using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260825130000_AddUtilityAccountTotalOutstandingBalance")]
public partial class AddUtilityAccountTotalOutstandingBalance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "total_outstanding_balance",
            table: "utility_accounts",
            type: "numeric(12,3)",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "chk_utility_accounts_total_outstanding_nonneg",
            table: "utility_accounts",
            sql: "total_outstanding_balance IS NULL OR total_outstanding_balance >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "chk_utility_accounts_total_outstanding_nonneg",
            table: "utility_accounts");

        migrationBuilder.DropColumn(
            name: "total_outstanding_balance",
            table: "utility_accounts");
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260830120000_AddActiveTenantPhoneUniqueness")]
public partial class AddActiveTenantPhoneUniqueness : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "uq_tenants_phone_active",
            table: "tenants",
            column: "phone",
            unique: true,
            filter: "phone IS NOT NULL AND deleted_at IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "uq_tenants_phone_active",
            table: "tenants");
    }
}

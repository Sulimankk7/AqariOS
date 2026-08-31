using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260831120000_AddRememberMeRefreshSessions")]
public partial class AddRememberMeRefreshSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "absolute_session_expires_at",
            table: "refresh_tokens",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_persistent",
            table: "refresh_tokens",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        // Existing sessions retain their historical persistent-cookie behavior,
        // while future inserts must explicitly persist the selected session mode.
        migrationBuilder.Sql(
            "ALTER TABLE refresh_tokens ALTER COLUMN is_persistent DROP DEFAULT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "absolute_session_expires_at",
            table: "refresh_tokens");

        migrationBuilder.DropColumn(
            name: "is_persistent",
            table: "refresh_tokens");
    }
}

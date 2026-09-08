using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260908103000_AddContactRequests")]
public sealed class AddContactRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contact_requests",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                company_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                phone_number = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                number_of_buildings = table.Column<int>(type: "integer", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contact_requests", x => x.id);
                table.CheckConstraint("chk_contact_requests_number_of_buildings", "number_of_buildings BETWEEN 1 AND 10000");
            });

        migrationBuilder.CreateIndex(
            name: "idx_contact_requests_status_created_at",
            table: "contact_requests",
            columns: new[] { "status", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "contact_requests");
    }
}

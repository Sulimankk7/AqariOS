using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260904120000_BackfillCompanyReceiptSequences")]
public sealed class BackfillCompanyReceiptSequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO company_receipt_sequences (company_id)
            SELECT id
            FROM companies
            ON CONFLICT (company_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data-only initialization is intentionally not reversed: sequence rows may
        // have issued receipt numbers after this migration is applied.
    }
}

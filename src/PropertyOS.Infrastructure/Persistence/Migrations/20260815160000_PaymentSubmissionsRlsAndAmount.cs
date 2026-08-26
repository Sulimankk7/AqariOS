using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PropertyOsDbContext))]
    [Migration("20260815160000_PaymentSubmissionsRlsAndAmount")]
    public partial class PaymentSubmissionsRlsAndAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add amount column as nullable numeric(12,3) to preserve legacy submissions without fabricating data
            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "payment_submissions",
                type: "numeric(12,3)",
                nullable: true);

            // 2. Add check constraint ensuring that any populated amount is strictly positive
            migrationBuilder.Sql(@"
                ALTER TABLE payment_submissions 
                ADD CONSTRAINT chk_payment_submissions_amount_positive 
                CHECK (amount IS NULL OR amount > 0);
            ");

            // 3. Enable and force Row Level Security (RLS) on payment_submissions matching AqariOS standard
            migrationBuilder.Sql(@"
                ALTER TABLE payment_submissions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE payment_submissions FORCE ROW LEVEL SECURITY;

                CREATE POLICY payment_submissions_tenant_isolation_policy ON payment_submissions
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Drop RLS policy and disable RLS
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS payment_submissions_tenant_isolation_policy ON payment_submissions;
                ALTER TABLE payment_submissions NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE payment_submissions DISABLE ROW LEVEL SECURITY;
                ALTER TABLE payment_submissions DROP CONSTRAINT IF EXISTS chk_payment_submissions_amount_positive;
            ");

            // 2. Drop amount column
            migrationBuilder.DropColumn(
                name: "amount",
                table: "payment_submissions");
        }
    }
}

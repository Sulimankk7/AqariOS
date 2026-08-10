using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds canonical 'cli_q' enum value to the PostgreSQL payment_method_enum type.
    /// Npgsql NpgsqlSnakeCaseNameTranslator translates C# enum CliQ to 'cli_q'.
    /// </summary>
    public partial class AddCliqToPaymentMethodEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE payment_method_enum ADD VALUE IF NOT EXISTS 'cli_q';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL does not support dropping enum values directly without recreating the type.
        }
    }
}

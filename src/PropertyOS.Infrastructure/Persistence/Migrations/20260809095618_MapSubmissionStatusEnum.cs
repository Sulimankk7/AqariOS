using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapSubmissionStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The database already has submission_status_enum from AddPaymentVerificationWorkflow.
            // This migration exists purely to synchronize the EF Core ModelSnapshot
            // because HasPostgresEnum was added to the DbContext later.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

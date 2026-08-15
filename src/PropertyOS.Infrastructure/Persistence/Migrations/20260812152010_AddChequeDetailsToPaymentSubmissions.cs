using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChequeDetailsToPaymentSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "payment_submissions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "cheque_due_date",
                table: "payment_submissions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "cheque_issue_date",
                table: "payment_submissions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cheque_number",
                table: "payment_submissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "payment_submissions");

            migrationBuilder.DropColumn(
                name: "cheque_due_date",
                table: "payment_submissions");

            migrationBuilder.DropColumn(
                name: "cheque_issue_date",
                table: "payment_submissions");

            migrationBuilder.DropColumn(
                name: "cheque_number",
                table: "payment_submissions");
        }
    }
}

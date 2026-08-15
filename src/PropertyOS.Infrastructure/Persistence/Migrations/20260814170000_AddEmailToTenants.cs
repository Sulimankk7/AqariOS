using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PropertyOsDbContext))]
    [Migration("20260814170000_AddEmailToTenants")]
    public partial class AddEmailToTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenants_company_email",
                table: "tenants",
                columns: new[] { "company_id", "email" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tenants_company_email",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "email",
                table: "tenants");
        }
    }
}

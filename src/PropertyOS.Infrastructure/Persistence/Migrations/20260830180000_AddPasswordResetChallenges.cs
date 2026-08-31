using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260830180000_AddPasswordResetChallenges")]
public partial class AddPasswordResetChallenges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TYPE revoke_reason_enum ADD VALUE IF NOT EXISTS 'password_reset';");

        migrationBuilder.CreateTable(
            name: "password_reset_challenges",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v7()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                credential_hash = table.Column<string>(type: "text", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                failed_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                max_attempts = table.Column<short>(type: "smallint", nullable: false),
                requested_ip = table.Column<System.Net.IPAddress>(type: "inet", nullable: false),
                verified_ip = table.Column<System.Net.IPAddress>(type: "inet", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_password_reset_challenges", x => x.id);
                table.CheckConstraint("chk_password_reset_challenges_attempts", "failed_attempts >= 0 AND max_attempts > 0 AND failed_attempts <= max_attempts");
                table.CheckConstraint("chk_password_reset_challenges_expiry", "expires_at > created_at");
                table.ForeignKey(
                    name: "FK_password_reset_challenges_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "idx_password_reset_challenges_expires_at",
            table: "password_reset_challenges",
            column: "expires_at");
        migrationBuilder.CreateIndex(
            name: "idx_password_reset_challenges_user_kind_created_at",
            table: "password_reset_challenges",
            columns: new[] { "user_id", "kind", "created_at" });
        migrationBuilder.CreateIndex(
            name: "uq_password_reset_challenges_credential_hash",
            table: "password_reset_challenges",
            column: "credential_hash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "password_reset_challenges");
        // PostgreSQL enum values cannot be removed safely in-place. The additive
        // password_reset revoke reason is intentionally retained on downgrade.
    }
}

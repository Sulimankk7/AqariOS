using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260910120000_PlatformAdministratorCreationSecurity")]
public sealed class PlatformAdministratorCreationSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE POLICY users_platform_admin_insert_policy
                ON users FOR INSERT TO propertyos_app
                WITH CHECK (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    AND email IS NOT NULL
                    AND phone IS NULL
                    AND password_hash IS NOT NULL
                    AND password_algorithm = 'argon2id'
                    AND preferred_language = 'ar'
                    AND email_verified_at IS NULL
                    AND phone_verified_at IS NULL
                    AND password_reset_token_hash IS NULL
                    AND password_reset_expires_at IS NULL
                    AND failed_login_attempts = 0
                    AND locked_until IS NULL
                    AND last_login_at IS NULL
                    AND last_login_ip IS NULL
                    AND mfa_enabled = false
                    AND mfa_secret_encrypted IS NULL
                    AND mfa_type IS NULL
                    AND is_active = true
                    AND deleted_at IS NULL
                    AND deleted_by IS NULL);

            GRANT SELECT, INSERT ON users TO propertyos_app;
            GRANT SELECT ON roles TO propertyos_app;

            ALTER TABLE user_system_roles ENABLE ROW LEVEL SECURITY;
            ALTER TABLE user_system_roles FORCE ROW LEVEL SECURITY;

            CREATE POLICY user_system_roles_read_policy
                ON user_system_roles FOR SELECT TO propertyos_app, propertyos_auth
                USING (true);

            CREATE POLICY user_system_roles_platform_admin_insert_policy
                ON user_system_roles FOR INSERT TO propertyos_app
                WITH CHECK (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    AND granted_by = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                    AND EXISTS (
                        SELECT 1
                        FROM roles
                        WHERE roles.id = user_system_roles.role_id
                          AND roles.code = 'SYSTEM_ADMIN'
                          AND roles.is_system = true
                          AND roles.company_id IS NULL
                          AND roles.deleted_at IS NULL));

            GRANT INSERT ON user_system_roles TO propertyos_app;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            REVOKE INSERT ON user_system_roles FROM propertyos_app;
            DROP POLICY IF EXISTS user_system_roles_platform_admin_insert_policy ON user_system_roles;
            DROP POLICY IF EXISTS user_system_roles_read_policy ON user_system_roles;
            ALTER TABLE user_system_roles NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE user_system_roles DISABLE ROW LEVEL SECURITY;

            REVOKE SELECT ON roles FROM propertyos_app;
            REVOKE SELECT, INSERT ON users FROM propertyos_app;
            DROP POLICY IF EXISTS users_platform_admin_insert_policy ON users;
            """);
    }
}

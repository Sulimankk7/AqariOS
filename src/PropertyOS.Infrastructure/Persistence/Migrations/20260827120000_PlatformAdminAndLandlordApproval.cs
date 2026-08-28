using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260827120000_PlatformAdminAndLandlordApproval")]
public sealed class PlatformAdminAndLandlordApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'registration_approval_status_enum') THEN
                    CREATE TYPE registration_approval_status_enum AS ENUM ('pending', 'approved', 'rejected');
                END IF;
            END $$;

            CREATE TABLE user_system_roles (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v7(),
                user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                role_id uuid NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
                granted_at timestamptz NOT NULL DEFAULT now(),
                granted_by uuid NULL REFERENCES users(id) ON DELETE SET NULL,
                CONSTRAINT uq_user_system_roles_user_role UNIQUE (user_id, role_id)
            );

            CREATE INDEX idx_user_system_roles_role_id ON user_system_roles(role_id);

            CREATE OR REPLACE FUNCTION enforce_global_system_role_assignment()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM roles
                    WHERE id = NEW.role_id
                      AND is_system = true
                      AND company_id IS NULL
                      AND deleted_at IS NULL
                ) THEN
                    RAISE EXCEPTION 'user_system_roles requires a live global system role'
                        USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_user_system_roles_global_role
            BEFORE INSERT OR UPDATE OF role_id ON user_system_roles
            FOR EACH ROW EXECUTE FUNCTION enforce_global_system_role_assignment();

            CREATE TABLE landlord_registrations (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v7(),
                user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
                company_id uuid NOT NULL REFERENCES companies(id) ON DELETE RESTRICT,
                membership_id uuid NOT NULL REFERENCES user_company_roles(id) ON DELETE RESTRICT,
                status registration_approval_status_enum NOT NULL,
                rejection_reason varchar(500) NULL,
                submitted_at timestamptz NOT NULL,
                reviewed_at timestamptz NULL,
                reviewed_by uuid NULL REFERENCES users(id) ON DELETE RESTRICT,
                created_at timestamptz NOT NULL DEFAULT now(),
                updated_at timestamptz NOT NULL DEFAULT now(),
                CONSTRAINT uq_landlord_registrations_user_id UNIQUE (user_id),
                CONSTRAINT uq_landlord_registrations_company_id UNIQUE (company_id),
                CONSTRAINT uq_landlord_registrations_membership_id UNIQUE (membership_id),
                CONSTRAINT chk_landlord_registrations_review CHECK (
                    (status = 'pending' AND reviewed_at IS NULL AND reviewed_by IS NULL AND rejection_reason IS NULL)
                    OR
                    (status = 'approved' AND rejection_reason IS NULL AND
                        ((reviewed_at IS NULL AND reviewed_by IS NULL) OR
                         (reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL)))
                    OR
                    (status = 'rejected' AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL AND rejection_reason IS NOT NULL)
                )
            );

            CREATE INDEX idx_landlord_registrations_status_submitted_at
                ON landlord_registrations(status, submitted_at);

            CREATE TRIGGER set_landlord_registrations_updated_at
                BEFORE UPDATE ON landlord_registrations
                FOR EACH ROW EXECUTE FUNCTION set_updated_at();

            INSERT INTO landlord_registrations (
                id, user_id, company_id, membership_id, status,
                submitted_at, created_at, updated_at)
            SELECT
                uuid_generate_v7(), c.created_by, c.id, ucr.id, 'approved',
                c.created_at, c.created_at, c.updated_at
            FROM companies c
            JOIN user_company_roles ucr
              ON ucr.company_id = c.id
             AND ucr.user_id = c.created_by
             AND ucr.deleted_at IS NULL
            JOIN roles r
              ON r.id = ucr.role_id
             AND r.code = 'COMPANY_ADMIN'
             AND r.deleted_at IS NULL
            WHERE c.created_by IS NOT NULL
              AND c.deleted_at IS NULL
            ON CONFLICT DO NOTHING;

            ALTER TABLE landlord_registrations ENABLE ROW LEVEL SECURITY;
            ALTER TABLE landlord_registrations FORCE ROW LEVEL SECURITY;

            CREATE POLICY landlord_registrations_public_insert
                ON landlord_registrations
                FOR INSERT TO propertyos_app
                WITH CHECK (
                    status = 'pending'
                    AND reviewed_at IS NULL
                    AND reviewed_by IS NULL
                    AND rejection_reason IS NULL
                );

            CREATE POLICY landlord_registrations_platform_select
                ON landlord_registrations
                FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            CREATE POLICY landlord_registrations_platform_update
                ON landlord_registrations
                FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            DROP POLICY IF EXISTS companies_platform_admin_select_policy ON companies;
            CREATE POLICY companies_platform_admin_select_policy
                ON companies FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY companies_platform_admin_update_policy
                ON companies FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            CREATE POLICY users_platform_admin_select_policy
                ON users FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY users_platform_admin_update_policy
                ON users FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            CREATE POLICY user_company_roles_platform_admin_select_policy
                ON user_company_roles FOR SELECT TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY user_company_roles_platform_admin_update_policy
                ON user_company_roles FOR UPDATE TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            GRANT SELECT, INSERT, UPDATE ON landlord_registrations TO propertyos_app;
            GRANT SELECT ON user_system_roles TO propertyos_app, propertyos_auth;

            INSERT INTO permissions (id, key, module, description_en, description_ar, is_deprecated, created_at)
            VALUES
                (uuid_generate_v7(), 'platform.landlord_registrations.read', 'PlatformAdministration', 'View landlord registration applications', 'عرض طلبات تسجيل الملاك', false, now()),
                (uuid_generate_v7(), 'platform.landlord_registrations.approve', 'PlatformAdministration', 'Approve landlord registration applications', 'الموافقة على طلبات تسجيل الملاك', false, now()),
                (uuid_generate_v7(), 'platform.landlord_registrations.reject', 'PlatformAdministration', 'Reject landlord registration applications', 'رفض طلبات تسجيل الملاك', false, now())
            ON CONFLICT (key) DO UPDATE SET
                module = EXCLUDED.module,
                description_en = EXCLUDED.description_en,
                description_ar = EXCLUDED.description_ar,
                is_deprecated = false;

            INSERT INTO roles (id, company_id, code, name_en, name_ar, is_system, created_at, updated_at)
            VALUES (uuid_generate_v7(), NULL, 'SYSTEM_ADMIN', 'System Administrator', 'مدير النظام', true, now(), now())
            ON CONFLICT (code) WHERE company_id IS NULL AND deleted_at IS NULL DO NOTHING;

            INSERT INTO role_permissions (id, role_id, permission_id, granted_at)
            SELECT uuid_generate_v7(), r.id, p.id, now()
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.code = 'SYSTEM_ADMIN'
              AND r.company_id IS NULL
              AND r.deleted_at IS NULL
              AND p.key IN (
                  'platform.landlord_registrations.read',
                  'platform.landlord_registrations.approve',
                  'platform.landlord_registrations.reject')
            ON CONFLICT (role_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS user_company_roles_platform_admin_update_policy ON user_company_roles;
            DROP POLICY IF EXISTS user_company_roles_platform_admin_select_policy ON user_company_roles;
            DROP POLICY IF EXISTS users_platform_admin_update_policy ON users;
            DROP POLICY IF EXISTS users_platform_admin_select_policy ON users;
            DROP POLICY IF EXISTS companies_platform_admin_update_policy ON companies;
            DROP POLICY IF EXISTS landlord_registrations_platform_update ON landlord_registrations;
            DROP POLICY IF EXISTS landlord_registrations_platform_select ON landlord_registrations;
            DROP POLICY IF EXISTS landlord_registrations_public_insert ON landlord_registrations;

            DELETE FROM role_permissions
            WHERE role_id IN (SELECT id FROM roles WHERE code = 'SYSTEM_ADMIN' AND company_id IS NULL)
               OR permission_id IN (
                    SELECT id FROM permissions
                    WHERE key IN (
                        'platform.landlord_registrations.read',
                        'platform.landlord_registrations.approve',
                        'platform.landlord_registrations.reject'));
            DELETE FROM user_system_roles
            WHERE role_id IN (SELECT id FROM roles WHERE code = 'SYSTEM_ADMIN' AND company_id IS NULL);
            DELETE FROM roles WHERE code = 'SYSTEM_ADMIN' AND company_id IS NULL;
            DELETE FROM permissions
            WHERE key IN (
                'platform.landlord_registrations.read',
                'platform.landlord_registrations.approve',
                'platform.landlord_registrations.reject');

            DROP TABLE IF EXISTS landlord_registrations;
            DROP TRIGGER IF EXISTS trg_user_system_roles_global_role ON user_system_roles;
            DROP FUNCTION IF EXISTS enforce_global_system_role_assignment();
            DROP TABLE IF EXISTS user_system_roles;
            DROP TYPE IF EXISTS registration_approval_status_enum;
            """);
    }
}

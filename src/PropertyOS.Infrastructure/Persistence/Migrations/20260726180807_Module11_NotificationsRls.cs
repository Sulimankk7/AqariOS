using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Module 11 RLS (doc §11.7) — the original Module11 migration shipped with NO policies.
    /// notification_templates and notification_deliveries get the standard company policy.
    /// notifications additionally gets the per-row recipient-ownership predicate: a user may
    /// only read/update/delete their own notifications ("Users may only access their own
    /// notifications" — the schema's one per-row, not merely per-tenant, restriction, and the
    /// direct PDPL-content protection). An UNSET app.current_user_id is trusted system context
    /// (background jobs / server-side commands with no acting user): TenantSessionInterceptor
    /// always sets the user id for authenticated requests, and session variables cannot be
    /// set by API callers. INSERT is company-checked only, because staff legitimately create
    /// notifications addressed to other recipients. The permission-gated
    /// 'notifications.view_all' admin bypass is deliberately deferred per the doc ("warranting
    /// its own dedicated design pass") — tracked in BACKEND_PROGRESS.md.
    /// </summary>
    public partial class Module11_NotificationsRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Runtime-role table privileges (RLS restricts rows; GRANT permits the tables).
                -- The original Module11 migration omitted these entirely.
                GRANT SELECT, INSERT, UPDATE, DELETE ON notifications, notification_deliveries, notification_templates TO propertyos_app;

                ALTER TABLE notification_templates ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notification_templates FORCE ROW LEVEL SECURITY;
                CREATE POLICY notification_templates_tenant_isolation_policy ON notification_templates
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE notification_deliveries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notification_deliveries FORCE ROW LEVEL SECURITY;
                CREATE POLICY notification_deliveries_tenant_isolation_policy ON notification_deliveries
                    FOR ALL TO propertyos_app
                    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
                ALTER TABLE notifications FORCE ROW LEVEL SECURITY;

                -- Staff create notifications addressed to OTHER users: INSERT checks tenant only.
                CREATE POLICY notifications_tenant_insert_policy ON notifications
                    FOR INSERT TO propertyos_app
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                -- Read/update/delete require tenant match AND (system context OR recipient ownership).
                CREATE POLICY notifications_recipient_select_policy ON notifications
                    FOR SELECT TO propertyos_app
                    USING (
                        company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                        AND (
                            NULLIF(current_setting('app.current_user_id', true), '') IS NULL
                            OR recipient_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                        )
                    );

                CREATE POLICY notifications_recipient_update_policy ON notifications
                    FOR UPDATE TO propertyos_app
                    USING (
                        company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                        AND (
                            NULLIF(current_setting('app.current_user_id', true), '') IS NULL
                            OR recipient_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);

                CREATE POLICY notifications_recipient_delete_policy ON notifications
                    FOR DELETE TO propertyos_app
                    USING (
                        company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                        AND (
                            NULLIF(current_setting('app.current_user_id', true), '') IS NULL
                            OR recipient_user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                        )
                    );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP POLICY IF EXISTS notifications_recipient_delete_policy ON notifications;
                DROP POLICY IF EXISTS notifications_recipient_update_policy ON notifications;
                DROP POLICY IF EXISTS notifications_recipient_select_policy ON notifications;
                DROP POLICY IF EXISTS notifications_tenant_insert_policy ON notifications;
                ALTER TABLE notifications NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE notifications DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS notification_deliveries_tenant_isolation_policy ON notification_deliveries;
                ALTER TABLE notification_deliveries NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE notification_deliveries DISABLE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS notification_templates_tenant_isolation_policy ON notification_templates;
                ALTER TABLE notification_templates NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE notification_templates DISABLE ROW LEVEL SECURITY;
            ");
        }
    }
}

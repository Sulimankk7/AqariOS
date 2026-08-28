using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260827170000_PlansSubscriptionsBusinessRules")]
public sealed class PlansSubscriptionsBusinessRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'plan_change_request_status_enum') THEN
                    CREATE TYPE plan_change_request_status_enum AS ENUM ('pending', 'approved', 'rejected', 'cancelled');
                END IF;
            END $$;

            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_trial_duration;
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_trial_duration
                CHECK ((supports_trial = true AND trial_duration_days > 0)
                    OR (supports_trial = false AND trial_duration_days IS NULL));
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_sort_order
                CHECK (sort_order >= 0);

            CREATE TABLE plan_change_requests (
                id uuid PRIMARY KEY DEFAULT uuid_generate_v7(),
                company_id uuid NOT NULL REFERENCES companies(id) ON DELETE RESTRICT,
                subscription_id uuid NOT NULL REFERENCES company_subscriptions(id) ON DELETE RESTRICT,
                current_plan_id uuid NOT NULL REFERENCES subscription_plans(id) ON DELETE RESTRICT,
                requested_plan_id uuid NOT NULL REFERENCES subscription_plans(id) ON DELETE RESTRICT,
                current_billing_cycle billing_cycle_enum NOT NULL,
                requested_billing_cycle billing_cycle_enum NOT NULL,
                requested_by uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
                requested_at timestamptz NOT NULL DEFAULT now(),
                status plan_change_request_status_enum NOT NULL DEFAULT 'pending',
                reviewer_id uuid NULL REFERENCES users(id) ON DELETE RESTRICT,
                reviewed_at timestamptz NULL,
                decision_note varchar(1000) NULL,
                rejection_reason varchar(500) NULL,
                CONSTRAINT chk_plan_change_requests_review_state CHECK (
                    (status = 'pending' AND reviewer_id IS NULL AND reviewed_at IS NULL AND rejection_reason IS NULL)
                    OR (status = 'cancelled' AND reviewer_id IS NULL AND reviewed_at IS NULL AND rejection_reason IS NULL)
                    OR (status = 'approved' AND reviewer_id IS NOT NULL AND reviewed_at IS NOT NULL AND rejection_reason IS NULL)
                    OR (status = 'rejected' AND reviewer_id IS NOT NULL AND reviewed_at IS NOT NULL
                        AND rejection_reason IS NOT NULL AND length(btrim(rejection_reason)) > 0)
                )
            );

            CREATE UNIQUE INDEX uq_plan_change_requests_one_pending_per_company
                ON plan_change_requests(company_id) WHERE status = 'pending';
            CREATE INDEX idx_plan_change_requests_company_requested_at
                ON plan_change_requests(company_id, requested_at);
            CREATE INDEX idx_plan_change_requests_status_requested_at
                ON plan_change_requests(status, requested_at);
            CREATE INDEX ix_plan_change_requests_subscription_id ON plan_change_requests(subscription_id);
            CREATE INDEX ix_plan_change_requests_current_plan_id ON plan_change_requests(current_plan_id);
            CREATE INDEX ix_plan_change_requests_requested_plan_id ON plan_change_requests(requested_plan_id);
            CREATE INDEX ix_plan_change_requests_requested_by ON plan_change_requests(requested_by);
            CREATE INDEX ix_plan_change_requests_reviewer_id ON plan_change_requests(reviewer_id);

            ALTER TABLE plan_change_requests ENABLE ROW LEVEL SECURITY;
            ALTER TABLE plan_change_requests FORCE ROW LEVEL SECURITY;

            CREATE POLICY plan_change_requests_company_select
                ON plan_change_requests FOR SELECT TO propertyos_app
                USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            CREATE POLICY plan_change_requests_company_insert
                ON plan_change_requests FOR INSERT TO propertyos_app
                WITH CHECK (
                    company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                    AND requested_by = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                    AND status = 'pending');
            CREATE POLICY plan_change_requests_company_update
                ON plan_change_requests FOR UPDATE TO propertyos_app
                USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            CREATE POLICY plan_change_requests_platform_all
                ON plan_change_requests FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            DROP POLICY IF EXISTS company_subscriptions_platform_admin_policy ON company_subscriptions;
            CREATE POLICY company_subscriptions_platform_admin_policy
                ON company_subscriptions FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            CREATE OR REPLACE FUNCTION enforce_plan_change_request_transition()
            RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE is_platform boolean := COALESCE(NULLIF(current_setting('app.is_platform_admin', true), '')::boolean, false);
            BEGIN
                IF ROW(NEW.company_id, NEW.subscription_id, NEW.current_plan_id, NEW.requested_plan_id,
                       NEW.current_billing_cycle, NEW.requested_billing_cycle, NEW.requested_by, NEW.requested_at)
                   IS DISTINCT FROM
                   ROW(OLD.company_id, OLD.subscription_id, OLD.current_plan_id, OLD.requested_plan_id,
                       OLD.current_billing_cycle, OLD.requested_billing_cycle, OLD.requested_by, OLD.requested_at) THEN
                    RAISE EXCEPTION 'Plan change request intent is immutable'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_plan_change_request_intent_immutable';
                END IF;

                IF OLD.status <> 'pending' THEN
                    RAISE EXCEPTION 'Only pending Plan change requests may transition'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_plan_change_request_pending_transition';
                END IF;

                IF is_platform THEN
                    IF NEW.status NOT IN ('approved', 'rejected') THEN
                        RAISE EXCEPTION 'Platform review must approve or reject a pending request'
                            USING ERRCODE = '23514', CONSTRAINT = 'chk_plan_change_request_platform_transition';
                    END IF;
                ELSE
                    IF NEW.status <> 'cancelled'
                       OR ROW(NEW.company_id, NEW.subscription_id, NEW.current_plan_id, NEW.requested_plan_id,
                              NEW.current_billing_cycle, NEW.requested_billing_cycle, NEW.requested_by, NEW.requested_at,
                              NEW.reviewer_id, NEW.reviewed_at, NEW.decision_note, NEW.rejection_reason)
                          IS DISTINCT FROM
                          ROW(OLD.company_id, OLD.subscription_id, OLD.current_plan_id, OLD.requested_plan_id,
                              OLD.current_billing_cycle, OLD.requested_billing_cycle, OLD.requested_by, OLD.requested_at,
                              OLD.reviewer_id, OLD.reviewed_at, OLD.decision_note, OLD.rejection_reason) THEN
                        RAISE EXCEPTION 'Company administrators may only cancel pending requests'
                            USING ERRCODE = '23514', CONSTRAINT = 'chk_plan_change_request_company_transition';
                    END IF;
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_plan_change_request_transition
                BEFORE UPDATE ON plan_change_requests
                FOR EACH ROW EXECUTE FUNCTION enforce_plan_change_request_transition();

            CREATE OR REPLACE FUNCTION enforce_used_subscription_plan_immutability()
            RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
            BEGIN
                IF EXISTS (SELECT 1 FROM company_subscriptions WHERE plan_id = OLD.id)
                   AND ROW(NEW.code, NEW.name_en, NEW.name_ar, NEW.description_en, NEW.description_ar,
                           NEW.monthly_price, NEW.yearly_price, NEW.currency, NEW.max_buildings,
                           NEW.max_users, NEW.max_storage_mb, NEW.feature_flags, NEW.supports_trial,
                           NEW.trial_duration_days, NEW.sort_order)
                       IS DISTINCT FROM
                       ROW(OLD.code, OLD.name_en, OLD.name_ar, OLD.description_en, OLD.description_ar,
                           OLD.monthly_price, OLD.yearly_price, OLD.currency, OLD.max_buildings,
                           OLD.max_users, OLD.max_storage_mb, OLD.feature_flags, OLD.supports_trial,
                           OLD.trial_duration_days, OLD.sort_order) THEN
                    RAISE EXCEPTION 'A used Plan commercial definition is immutable'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_subscription_plans_used_immutable';
                END IF;
                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_subscription_plans_used_immutable
                BEFORE UPDATE ON subscription_plans
                FOR EACH ROW EXECUTE FUNCTION enforce_used_subscription_plan_immutability();

            GRANT SELECT, INSERT, UPDATE ON plan_change_requests TO propertyos_app;

            INSERT INTO permissions (id, key, module, description_en, description_ar, is_deprecated, created_at)
            VALUES
                (uuid_generate_v7(), 'platform.plans.read', 'Subscriptions', 'View the platform Plan catalog', 'عرض كتالوج خطط المنصة', false, now()),
                (uuid_generate_v7(), 'platform.plans.create', 'Subscriptions', 'Create commercial Plans', 'إنشاء خطط تجارية', false, now()),
                (uuid_generate_v7(), 'platform.plans.lifecycle', 'Subscriptions', 'Activate and deactivate Plans', 'تفعيل وتعطيل الخطط', false, now()),
                (uuid_generate_v7(), 'platform.subscriptions.read', 'Subscriptions', 'View company subscriptions', 'عرض اشتراكات الشركات', false, now()),
                (uuid_generate_v7(), 'platform.subscriptions.manage', 'Subscriptions', 'Create company subscriptions', 'إنشاء اشتراكات الشركات', false, now()),
                (uuid_generate_v7(), 'platform.plan_change_requests.read', 'Subscriptions', 'View Plan change requests', 'عرض طلبات تغيير الخطط', false, now()),
                (uuid_generate_v7(), 'platform.plan_change_requests.review', 'Subscriptions', 'Approve and reject Plan change requests', 'الموافقة على طلبات تغيير الخطط ورفضها', false, now()),
                (uuid_generate_v7(), 'subscriptions.plans.view', 'Subscriptions', 'View available active Plans', 'عرض الخطط النشطة المتاحة', false, now()),
                (uuid_generate_v7(), 'subscriptions.own.read', 'Subscriptions', 'View the company''s own subscription', 'عرض اشتراك الشركة', false, now()),
                (uuid_generate_v7(), 'subscriptions.plan_change_requests.own.read', 'Subscriptions', 'View the company''s Plan change requests', 'عرض طلبات الشركة لتغيير الخطط', false, now()),
                (uuid_generate_v7(), 'subscriptions.plan_change_requests.own.create', 'Subscriptions', 'Create a Plan change request for the company', 'إنشاء طلب لتغيير خطة الشركة', false, now()),
                (uuid_generate_v7(), 'subscriptions.plan_change_requests.own.cancel', 'Subscriptions', 'Cancel a pending Plan change request for the company', 'إلغاء طلب معلق لتغيير خطة الشركة', false, now())
            ON CONFLICT (key) DO UPDATE SET
                module = EXCLUDED.module,
                description_en = EXCLUDED.description_en,
                description_ar = EXCLUDED.description_ar,
                is_deprecated = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_subscription_plans_used_immutable ON subscription_plans;
            DROP FUNCTION IF EXISTS enforce_used_subscription_plan_immutability();
            DROP TRIGGER IF EXISTS trg_plan_change_request_transition ON plan_change_requests;
            DROP FUNCTION IF EXISTS enforce_plan_change_request_transition();
            DROP POLICY IF EXISTS company_subscriptions_platform_admin_policy ON company_subscriptions;
            DROP TABLE IF EXISTS plan_change_requests;
            DROP TYPE IF EXISTS plan_change_request_status_enum;
            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_sort_order;
            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_trial_duration;
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_trial_duration
                CHECK (trial_duration_days IS NOT NULL OR supports_trial = false);

            DELETE FROM role_permissions WHERE permission_id IN (
                SELECT id FROM permissions WHERE key IN (
                    'platform.plans.read', 'platform.plans.create', 'platform.plans.lifecycle',
                    'platform.subscriptions.read', 'platform.subscriptions.manage',
                    'platform.plan_change_requests.read', 'platform.plan_change_requests.review',
                    'subscriptions.plans.view', 'subscriptions.own.read',
                    'subscriptions.plan_change_requests.own.read',
                    'subscriptions.plan_change_requests.own.create',
                    'subscriptions.plan_change_requests.own.cancel'));
            DELETE FROM permissions WHERE key IN (
                'platform.plans.read', 'platform.plans.create', 'platform.plans.lifecycle',
                'platform.subscriptions.read', 'platform.subscriptions.manage',
                'platform.plan_change_requests.read', 'platform.plan_change_requests.review',
                'subscriptions.plans.view', 'subscriptions.own.read',
                'subscriptions.plan_change_requests.own.read',
                'subscriptions.plan_change_requests.own.create',
                'subscriptions.plan_change_requests.own.cancel');
            """);
    }
}

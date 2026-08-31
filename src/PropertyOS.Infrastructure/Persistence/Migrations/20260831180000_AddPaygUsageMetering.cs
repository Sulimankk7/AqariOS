using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260831180000_AddPaygUsageMetering")]
public sealed class AddPaygUsageMetering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'subscription_pricing_model_enum') THEN
                    CREATE TYPE subscription_pricing_model_enum AS ENUM ('fixed', 'pay_as_you_go');
                END IF;
            END $$;

            ALTER TABLE subscription_plans
                ADD COLUMN pricing_model subscription_pricing_model_enum NOT NULL DEFAULT 'fixed',
                ADD COLUMN payg_monthly_unit_price numeric(12,3) NULL,
                ADD COLUMN payg_yearly_monthly_equivalent_unit_price numeric(12,3) NULL;

            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_prices_positive;
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_prices_positive CHECK (
                (pricing_model = 'fixed' AND monthly_price > 0 AND yearly_price > 0)
                OR (pricing_model = 'pay_as_you_go' AND monthly_price = 0 AND yearly_price = 0));
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_payg_prices CHECK (
                (pricing_model = 'fixed' AND payg_monthly_unit_price IS NULL AND payg_yearly_monthly_equivalent_unit_price IS NULL)
                OR (pricing_model = 'pay_as_you_go' AND payg_monthly_unit_price IS NOT NULL AND payg_monthly_unit_price > 0 AND payg_yearly_monthly_equivalent_unit_price IS NOT NULL AND payg_yearly_monthly_equivalent_unit_price > 0));

            CREATE TABLE payg_usage_periods (
                id uuid PRIMARY KEY,
                company_id uuid NOT NULL REFERENCES companies(id) ON DELETE RESTRICT,
                company_subscription_id uuid NOT NULL REFERENCES company_subscriptions(id) ON DELETE RESTRICT,
                period_start date NOT NULL,
                period_end date NOT NULL,
                billing_cycle billing_cycle_enum NOT NULL,
                monthly_equivalent_unit_price_snapshot numeric(12,3) NOT NULL,
                currency_snapshot char(3) NOT NULL,
                period_day_count smallint NOT NULL,
                active_lease_count integer NOT NULL DEFAULT 0,
                accumulated_lease_days integer NOT NULL DEFAULT 0,
                estimated_amount numeric(14,3) NOT NULL DEFAULT 0,
                projected_amount numeric(14,3) NOT NULL DEFAULT 0,
                calculated_through date NOT NULL,
                is_chargeable boolean NOT NULL,
                is_finalized boolean NOT NULL DEFAULT false,
                finalized_at timestamptz NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL,
                CONSTRAINT uq_payg_usage_periods_company_id UNIQUE (company_id, id),
                CONSTRAINT chk_payg_usage_period_dates CHECK (period_end > period_start AND calculated_through >= period_start AND calculated_through <= period_end),
                CONSTRAINT chk_payg_usage_period_price CHECK (monthly_equivalent_unit_price_snapshot > 0),
                CONSTRAINT chk_payg_usage_period_totals CHECK (period_day_count > 0 AND active_lease_count >= 0 AND accumulated_lease_days >= 0 AND estimated_amount >= 0 AND projected_amount >= 0),
                CONSTRAINT chk_payg_usage_period_finalized CHECK ((is_finalized = false AND finalized_at IS NULL) OR (is_finalized = true AND finalized_at IS NOT NULL AND calculated_through = period_end))
            );

            CREATE UNIQUE INDEX uq_payg_usage_period_subscription_dates
                ON payg_usage_periods(company_subscription_id, period_start, period_end);
            CREATE INDEX idx_payg_usage_period_company_start
                ON payg_usage_periods(company_id, period_start DESC);
            CREATE INDEX idx_payg_usage_period_unfinalized_end
                ON payg_usage_periods(is_finalized, period_end) WHERE is_finalized = false;

            CREATE TABLE payg_lease_usage (
                id uuid PRIMARY KEY,
                company_id uuid NOT NULL,
                usage_period_id uuid NOT NULL,
                lease_contract_id uuid NOT NULL,
                tenant_id uuid NOT NULL,
                usage_start date NOT NULL,
                usage_end date NOT NULL,
                billable_days smallint NOT NULL,
                calculated_amount numeric(14,3) NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL,
                CONSTRAINT fk_payg_lease_usage_period FOREIGN KEY (company_id, usage_period_id)
                    REFERENCES payg_usage_periods(company_id, id) ON DELETE CASCADE,
                CONSTRAINT fk_payg_lease_usage_lease FOREIGN KEY (company_id, lease_contract_id)
                    REFERENCES lease_contracts(company_id, id) ON DELETE RESTRICT,
                CONSTRAINT fk_payg_lease_usage_tenant FOREIGN KEY (company_id, tenant_id)
                    REFERENCES tenants(company_id, id) ON DELETE RESTRICT,
                CONSTRAINT chk_payg_lease_usage_dates CHECK (usage_end > usage_start AND billable_days = usage_end - usage_start),
                CONSTRAINT chk_payg_lease_usage_amount CHECK (calculated_amount >= 0)
            );

            CREATE UNIQUE INDEX uq_payg_lease_usage_period_lease
                ON payg_lease_usage(usage_period_id, lease_contract_id);
            CREATE INDEX idx_payg_lease_usage_company_period
                ON payg_lease_usage(company_id, usage_period_id);
            CREATE INDEX idx_payg_lease_usage_lease ON payg_lease_usage(lease_contract_id);
            CREATE INDEX idx_lease_contracts_payg_period_overlap
                ON lease_contracts(company_id, start_date, end_date);

            ALTER TABLE payg_usage_periods ENABLE ROW LEVEL SECURITY;
            ALTER TABLE payg_usage_periods FORCE ROW LEVEL SECURITY;
            ALTER TABLE payg_lease_usage ENABLE ROW LEVEL SECURITY;
            ALTER TABLE payg_lease_usage FORCE ROW LEVEL SECURITY;

            CREATE POLICY payg_usage_periods_company_policy ON payg_usage_periods
                FOR ALL TO propertyos_app
                USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            CREATE POLICY payg_usage_periods_platform_policy ON payg_usage_periods
                FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');
            CREATE POLICY payg_lease_usage_company_policy ON payg_lease_usage
                FOR ALL TO propertyos_app
                USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
                WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);
            CREATE POLICY payg_lease_usage_platform_policy ON payg_lease_usage
                FOR ALL TO propertyos_app
                USING (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true')
                WITH CHECK (NULLIF(current_setting('app.is_platform_admin', true), '') = 'true');

            GRANT SELECT, INSERT, UPDATE, DELETE ON payg_usage_periods, payg_lease_usage TO propertyos_app;

            CREATE FUNCTION prevent_finalized_payg_period_mutation()
            RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF OLD.is_finalized THEN
                    RAISE EXCEPTION 'Finalized PAYG usage is immutable'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_payg_usage_finalized_immutable';
                END IF;
                IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER trg_payg_period_finalized_immutable
                BEFORE UPDATE OR DELETE ON payg_usage_periods
                FOR EACH ROW EXECUTE FUNCTION prevent_finalized_payg_period_mutation();

            CREATE FUNCTION prevent_finalized_payg_detail_mutation()
            RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE target_period_id uuid := CASE WHEN TG_OP = 'DELETE' THEN OLD.usage_period_id ELSE NEW.usage_period_id END;
            BEGIN
                IF EXISTS (SELECT 1 FROM payg_usage_periods WHERE id = target_period_id AND is_finalized) THEN
                    RAISE EXCEPTION 'Finalized PAYG lease usage is immutable'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_payg_lease_usage_finalized_immutable';
                END IF;
                IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER trg_payg_detail_finalized_immutable
                BEFORE INSERT OR UPDATE OR DELETE ON payg_lease_usage
                FOR EACH ROW EXECUTE FUNCTION prevent_finalized_payg_detail_mutation();

            CREATE OR REPLACE FUNCTION enforce_used_subscription_plan_immutability()
            RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
            BEGIN
                IF EXISTS (SELECT 1 FROM company_subscriptions WHERE plan_id = OLD.id)
                   AND ROW(NEW.code, NEW.name_en, NEW.name_ar, NEW.description_en, NEW.description_ar,
                           NEW.monthly_price, NEW.yearly_price, NEW.currency, NEW.pricing_model,
                           NEW.payg_monthly_unit_price, NEW.payg_yearly_monthly_equivalent_unit_price,
                           NEW.max_buildings, NEW.max_users, NEW.max_storage_mb, NEW.feature_flags,
                           NEW.supports_trial, NEW.trial_duration_days, NEW.sort_order)
                       IS DISTINCT FROM
                       ROW(OLD.code, OLD.name_en, OLD.name_ar, OLD.description_en, OLD.description_ar,
                           OLD.monthly_price, OLD.yearly_price, OLD.currency, OLD.pricing_model,
                           OLD.payg_monthly_unit_price, OLD.payg_yearly_monthly_equivalent_unit_price,
                           OLD.max_buildings, OLD.max_users, OLD.max_storage_mb, OLD.feature_flags,
                           OLD.supports_trial, OLD.trial_duration_days, OLD.sort_order) THEN
                    RAISE EXCEPTION 'A used Plan commercial definition is immutable'
                        USING ERRCODE = '23514', CONSTRAINT = 'chk_subscription_plans_used_immutable';
                END IF;
                RETURN NEW;
            END;
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_payg_detail_finalized_immutable ON payg_lease_usage;
            DROP FUNCTION IF EXISTS prevent_finalized_payg_detail_mutation();
            DROP TRIGGER IF EXISTS trg_payg_period_finalized_immutable ON payg_usage_periods;
            DROP FUNCTION IF EXISTS prevent_finalized_payg_period_mutation();
            DROP TABLE IF EXISTS payg_lease_usage;
            DROP TABLE IF EXISTS payg_usage_periods;
            DROP INDEX IF EXISTS idx_lease_contracts_payg_period_overlap;
            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_payg_prices;
            ALTER TABLE subscription_plans DROP CONSTRAINT IF EXISTS chk_subscription_plans_prices_positive;
            ALTER TABLE subscription_plans DROP COLUMN IF EXISTS payg_yearly_monthly_equivalent_unit_price;
            ALTER TABLE subscription_plans DROP COLUMN IF EXISTS payg_monthly_unit_price;
            ALTER TABLE subscription_plans DROP COLUMN IF EXISTS pricing_model;
            ALTER TABLE subscription_plans ADD CONSTRAINT chk_subscription_plans_prices_positive
                CHECK (monthly_price > 0 AND yearly_price > 0);
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
            DROP TYPE IF EXISTS subscription_pricing_model_enum;
            """);
    }
}

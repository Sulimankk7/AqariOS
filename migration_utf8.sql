Build started...
Build succeeded.
START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'billing_cycle_enum') THEN
        CREATE SCHEMA billing_cycle_enum;
    END IF;
END $EF$;

CREATE TYPE billing_cycle_enum.billing_cycle_enum AS ENUM ('monthly', 'yearly');
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'company_type_enum') THEN
        CREATE SCHEMA company_type_enum;
    END IF;
END $EF$;

CREATE TYPE company_type_enum.company_type AS ENUM ('individual_owner', 'property_management_company', 'investment_company');
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'late_fee_type_enum') THEN
        CREATE SCHEMA late_fee_type_enum;
    END IF;
END $EF$;

CREATE TYPE late_fee_type_enum.late_fee_type AS ENUM ('none', 'fixed', 'percentage');
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'subscription_status_enum') THEN
        CREATE SCHEMA subscription_status_enum;
    END IF;
END $EF$;

CREATE TYPE subscription_status_enum.subscription_status_enum AS ENUM ('trialing', 'active', 'past_due', 'suspended', 'cancelled', 'expired');


DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'subscription_status_enum') THEN
        CREATE TYPE subscription_status_enum AS ENUM (
            'trialing', 'active', 'past_due', 'suspended', 'cancelled', 'expired'
        );
    END IF;
END $$;



DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'billing_cycle_enum') THEN
        CREATE TYPE billing_cycle_enum AS ENUM (
            'monthly', 'yearly'
        );
    END IF;
END $$;


CREATE TABLE subscription_plans (
    id uuid NOT NULL DEFAULT (uuid_generate_v7()),
    code character varying(50) NOT NULL,
    name_en character varying(100) NOT NULL,
    name_ar character varying(100) NOT NULL,
    description_en text,
    description_ar text,
    monthly_price numeric(12,3) NOT NULL,
    yearly_price numeric(12,3) NOT NULL,
    currency char(3) NOT NULL DEFAULT 'JOD',
    max_buildings integer,
    max_users integer,
    max_storage_mb integer,
    feature_flags jsonb NOT NULL DEFAULT '{}',
    supports_trial boolean NOT NULL DEFAULT FALSE,
    trial_duration_days smallint,
    is_active boolean NOT NULL DEFAULT TRUE,
    sort_order smallint NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_subscription_plans" PRIMARY KEY (id),
    CONSTRAINT chk_subscription_plans_prices_positive CHECK (monthly_price > 0 AND yearly_price > 0),
    CONSTRAINT chk_subscription_plans_quotas_positive CHECK ((max_buildings IS NULL OR max_buildings > 0) AND (max_users IS NULL OR max_users > 0) AND (max_storage_mb IS NULL OR max_storage_mb > 0)),
    CONSTRAINT chk_subscription_plans_trial_duration CHECK (trial_duration_days IS NOT NULL OR supports_trial = false)
);

CREATE TABLE company_subscriptions (
    id uuid NOT NULL DEFAULT (uuid_generate_v7()),
    company_id uuid NOT NULL,
    plan_id uuid NOT NULL,
    status subscription_status_enum NOT NULL DEFAULT ('trialing'::subscription_status_enum),
    start_date date NOT NULL,
    end_date date NOT NULL,
    trial_end_date date,
    price_at_subscription numeric(12,3) NOT NULL,
    currency_at_subscription char(3) NOT NULL DEFAULT 'JOD',
    billing_cycle billing_cycle_enum NOT NULL DEFAULT ('monthly'::billing_cycle_enum),
    auto_renew boolean NOT NULL DEFAULT TRUE,
    suspended_at timestamp with time zone,
    suspension_reason character varying(255),
    cancelled_at timestamp with time zone,
    cancellation_reason character varying(255),
    expired_at timestamp with time zone,
    external_billing_ref character varying(255),
    created_at timestamp with time zone NOT NULL DEFAULT (now()),
    updated_at timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_company_subscriptions" PRIMARY KEY (id),
    CONSTRAINT chk_company_subscriptions_dates CHECK (end_date > start_date),
    CONSTRAINT chk_company_subscriptions_suspension CHECK (suspension_reason IS NOT NULL OR suspended_at IS NULL),
    CONSTRAINT chk_company_subscriptions_trial_end CHECK (trial_end_date IS NOT NULL OR status != 'trialing'),
    CONSTRAINT "FK_company_subscriptions_companies_company_id" FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_company_subscriptions_subscription_plans_plan_id" FOREIGN KEY (plan_id) REFERENCES subscription_plans (id) ON DELETE RESTRICT
);

CREATE INDEX idx_company_subscriptions_company_status ON company_subscriptions (company_id, status);

CREATE INDEX idx_company_subscriptions_end_date ON company_subscriptions (end_date) WHERE status IN ('trialing'::subscription_status_enum,'active'::subscription_status_enum,'past_due'::subscription_status_enum);

CREATE INDEX "IX_company_subscriptions_plan_id" ON company_subscriptions (plan_id);

CREATE UNIQUE INDEX uq_company_subscriptions_one_active ON company_subscriptions (company_id) WHERE status IN ('trialing'::subscription_status_enum,'active'::subscription_status_enum,'past_due'::subscription_status_enum);

CREATE INDEX idx_subscription_plans_active_sort ON subscription_plans (is_active, sort_order) WHERE is_active = true;

CREATE UNIQUE INDEX uq_subscription_plans_code ON subscription_plans (code);


CREATE TRIGGER set_subscription_plans_updated_at
    BEFORE UPDATE ON subscription_plans
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();



CREATE TRIGGER set_company_subscriptions_updated_at
    BEFORE UPDATE ON company_subscriptions
    FOR EACH ROW
    EXECUTE FUNCTION set_updated_at();


ALTER TABLE company_subscriptions ENABLE ROW LEVEL SECURITY;

ALTER TABLE company_subscriptions FORCE ROW LEVEL SECURITY;


CREATE POLICY tenant_isolation_policy ON company_subscriptions
    AS PERMISSIVE FOR ALL
    USING (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid)
    WITH CHECK (company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid);


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260703173554_Module2_Plans_Subscriptions_Foundation', '9.0.0');

COMMIT;



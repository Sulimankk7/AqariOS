using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Canonical production schema for Module 12 — Utility Bills (Electricity & Water).
/// The schema follows the existing EF configurations and AqariOS tenant/RLS conventions.
/// </summary>
[DbContext(typeof(PropertyOsDbContext))]
[Migration("20260820120000_Module12_UtilityBills")]
public partial class Module12_UtilityBills : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TYPE utility_type_enum AS ENUM ('electricity', 'water');
            CREATE TYPE utility_sync_status_enum AS ENUM (
                'never_synced', 'syncing', 'synced', 'provider_error',
                'rate_limited', 'timeout', 'suspended'
            );
            CREATE TYPE utility_bill_status_enum AS ENUM ('unpaid', 'paid', 'unknown');

            ALTER TYPE notification_type_enum ADD VALUE IF NOT EXISTS 'utility_bill_electricity';
            ALTER TYPE notification_type_enum ADD VALUE IF NOT EXISTS 'utility_bill_water';
            """);

        migrationBuilder.Sql("""
            CREATE TABLE utility_accounts (
                id UUID NOT NULL DEFAULT uuid_generate_v7(),
                company_id UUID NOT NULL,
                lease_contract_id UUID NOT NULL,
                tenant_id UUID NOT NULL,
                apartment_id UUID NOT NULL,
                utility_type utility_type_enum NOT NULL,
                account_number VARCHAR(150) NOT NULL,
                meter_number VARCHAR(100) NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                last_known_bill_date DATE NULL,
                last_successful_sync_at TIMESTAMPTZ NULL,
                last_attempted_sync_at TIMESTAMPTZ NULL,
                next_check_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                sync_status utility_sync_status_enum NOT NULL DEFAULT 'never_synced',
                last_sync_error_detail TEXT NULL,
                consecutive_failure_count SMALLINT NOT NULL DEFAULT 0,
                average_billing_interval_days SMALLINT NULL,
                billing_interval_sample_count SMALLINT NOT NULL DEFAULT 0,
                estimated_next_bill_date DATE NULL,
                historical_bootstrap_completed BOOLEAN NOT NULL DEFAULT FALSE,
                claimed_by_job_run_id VARCHAR(100) NULL,
                claimed_at TIMESTAMPTZ NULL,
                deleted_at TIMESTAMPTZ NULL,
                deleted_by UUID NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                created_by UUID NULL,
                updated_by UUID NULL,

                CONSTRAINT pk_utility_accounts PRIMARY KEY (id),
                CONSTRAINT fk_utility_accounts_companies_company_id
                    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE RESTRICT,
                CONSTRAINT fk_utility_accounts_lease_contracts_lease_contract_id
                    FOREIGN KEY (lease_contract_id) REFERENCES lease_contracts(id) ON DELETE RESTRICT,
                CONSTRAINT fk_utility_accounts_tenants_tenant_id
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE RESTRICT,
                CONSTRAINT fk_utility_accounts_apartments_apartment_id
                    FOREIGN KEY (apartment_id) REFERENCES apartments(id) ON DELETE RESTRICT,
                CONSTRAINT fk_utility_accounts_users_created_by
                    FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE SET NULL,
                CONSTRAINT fk_utility_accounts_users_updated_by
                    FOREIGN KEY (updated_by) REFERENCES users(id) ON DELETE SET NULL,
                CONSTRAINT fk_utility_accounts_users_deleted_by
                    FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL,
                CONSTRAINT chk_utility_accounts_account_number_not_blank
                    CHECK (length(btrim(account_number)) > 0),
                CONSTRAINT chk_utility_accounts_interval_consistent
                    CHECK (
                        (billing_interval_sample_count = 0
                            AND average_billing_interval_days IS NULL
                            AND estimated_next_bill_date IS NULL)
                        OR
                        (billing_interval_sample_count > 0
                            AND average_billing_interval_days IS NOT NULL)
                    ),
                CONSTRAINT chk_utility_accounts_claim_fields_consistent
                    CHECK (
                        (claimed_at IS NULL AND claimed_by_job_run_id IS NULL)
                        OR
                        (claimed_at IS NOT NULL AND claimed_by_job_run_id IS NOT NULL)
                    )
            );

            CREATE TABLE utility_bills (
                id UUID NOT NULL DEFAULT uuid_generate_v7(),
                company_id UUID NOT NULL,
                utility_account_id UUID NOT NULL,
                utility_type utility_type_enum NOT NULL,
                provider_external_id VARCHAR(255) NOT NULL,
                bill_date DATE NOT NULL,
                due_date DATE NULL,
                amount NUMERIC(12,3) NOT NULL,
                currency CHAR(3) NOT NULL DEFAULT 'JOD',
                is_paid BOOLEAN NOT NULL DEFAULT FALSE,
                payment_status utility_bill_status_enum NOT NULL DEFAULT 'unknown',
                provider_reference TEXT NULL,
                discovered_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                is_from_historical_backfill BOOLEAN NOT NULL DEFAULT FALSE,
                notification_sent_at TIMESTAMPTZ NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),

                CONSTRAINT pk_utility_bills PRIMARY KEY (id),
                CONSTRAINT fk_utility_bills_companies_company_id
                    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE RESTRICT,
                CONSTRAINT fk_utility_bills_utility_accounts_utility_account_id
                    FOREIGN KEY (utility_account_id) REFERENCES utility_accounts(id) ON DELETE RESTRICT,
                CONSTRAINT chk_utility_bills_amount_positive CHECK (amount > 0),
                CONSTRAINT chk_utility_bills_currency_length CHECK (length(currency) = 3),
                CONSTRAINT chk_utility_bills_notification_after_discovered
                    CHECK (notification_sent_at IS NULL OR notification_sent_at >= discovered_at)
            );
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX uq_utility_accounts_lease_type
                ON utility_accounts (lease_contract_id, utility_type)
                WHERE deleted_at IS NULL;

            CREATE UNIQUE INDEX uq_utility_accounts_type_number
                ON utility_accounts (utility_type, account_number)
                WHERE deleted_at IS NULL;

            CREATE INDEX idx_utility_accounts_scheduler
                ON utility_accounts (utility_type, next_check_at)
                WHERE deleted_at IS NULL
                  AND is_active = TRUE
                  AND historical_bootstrap_completed = TRUE;

            CREATE INDEX idx_utility_accounts_bootstrap_pending
                ON utility_accounts (utility_type, created_at)
                WHERE deleted_at IS NULL
                  AND is_active = TRUE
                  AND historical_bootstrap_completed = FALSE;

            CREATE INDEX idx_utility_accounts_company_tenant
                ON utility_accounts (company_id, tenant_id)
                WHERE deleted_at IS NULL;

            CREATE INDEX idx_utility_accounts_lease
                ON utility_accounts (lease_contract_id)
                WHERE deleted_at IS NULL;

            CREATE UNIQUE INDEX uq_utility_bills_account_external_id
                ON utility_bills (utility_account_id, provider_external_id);

            CREATE INDEX idx_utility_bills_account_date
                ON utility_bills (utility_account_id, bill_date DESC);

            CREATE INDEX idx_utility_bills_company_date
                ON utility_bills (company_id, bill_date DESC);

            CREATE INDEX idx_utility_bills_notification_pending
                ON utility_bills (utility_account_id)
                WHERE notification_sent_at IS NULL
                  AND is_paid = FALSE
                  AND is_from_historical_backfill = FALSE;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER set_utility_accounts_updated_at
                BEFORE UPDATE ON utility_accounts
                FOR EACH ROW EXECUTE FUNCTION set_updated_at();

            CREATE TRIGGER set_utility_bills_updated_at
                BEFORE UPDATE ON utility_bills
                FOR EACH ROW EXECUTE FUNCTION set_updated_at();

            ALTER TABLE utility_accounts OWNER TO propertyos_owner;
            ALTER TABLE utility_bills OWNER TO propertyos_owner;

            ALTER TABLE utility_accounts ENABLE ROW LEVEL SECURITY;
            ALTER TABLE utility_accounts FORCE ROW LEVEL SECURITY;
            CREATE POLICY utility_accounts_tenant_isolation_policy ON utility_accounts
                FOR ALL TO propertyos_app
                USING (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    OR company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                )
                WITH CHECK (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    OR company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                );

            ALTER TABLE utility_bills ENABLE ROW LEVEL SECURITY;
            ALTER TABLE utility_bills FORCE ROW LEVEL SECURITY;
            CREATE POLICY utility_bills_tenant_isolation_policy ON utility_bills
                FOR ALL TO propertyos_app
                USING (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    OR company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                )
                WITH CHECK (
                    NULLIF(current_setting('app.is_platform_admin', true), '') = 'true'
                    OR company_id = NULLIF(current_setting('app.current_company_id', true), '')::uuid
                );

            GRANT SELECT, INSERT, UPDATE, DELETE ON utility_accounts, utility_bills TO propertyos_app;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS utility_bills;
            DROP TABLE IF EXISTS utility_accounts;
            DROP TYPE IF EXISTS utility_bill_status_enum;
            DROP TYPE IF EXISTS utility_sync_status_enum;
            DROP TYPE IF EXISTS utility_type_enum;
            """);

        // PostgreSQL does not support removing individual enum labels safely.
        // The two notification labels intentionally remain after a downgrade.
    }
}

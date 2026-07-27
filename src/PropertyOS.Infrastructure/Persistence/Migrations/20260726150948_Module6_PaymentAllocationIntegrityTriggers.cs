using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyOS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Database-level backstop for payment-allocation financial integrity (Module 6 doc §6.3).
    /// The application layer enforces these rules first (with FOR UPDATE row locks); this
    /// trigger guarantees that no writer — including direct SQL — can produce:
    ///   1. an obligation whose active allocations exceed its amount_due
    ///      (except 'adjustment' obligations, which absorb overpayment by design), or
    ///   2. a receiving payment allocated out beyond the funds it physically received.
    /// </summary>
    public partial class Module6_PaymentAllocationIntegrityTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION enforce_payment_allocation_limits()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $body$
                DECLARE
                    v_amount_due    numeric(12,3);
                    v_is_adjustment boolean;
                    v_total         numeric(12,3);
                BEGIN
                    -- Only rows that count toward active settlement totals need validation.
                    IF NEW.allocation_status <> 'active' OR NEW.deleted_at IS NOT NULL THEN
                        RETURN NEW;
                    END IF;

                    -- Obligation side: total active allocations must not exceed amount_due,
                    -- unless the obligation is an 'adjustment' row (overpayment absorber).
                    SELECT rp.amount_due, rp.payment_purpose = 'adjustment'
                      INTO v_amount_due, v_is_adjustment
                      FROM rent_payments rp
                     WHERE rp.id = NEW.obligation_payment_id;

                    IF FOUND AND NOT v_is_adjustment THEN
                        SELECT COALESCE(SUM(pa.allocated_amount), 0)
                          INTO v_total
                          FROM payment_allocations pa
                         WHERE pa.obligation_payment_id = NEW.obligation_payment_id
                           AND pa.allocation_status = 'active'
                           AND pa.deleted_at IS NULL
                           AND pa.id <> NEW.id;

                        IF v_total + NEW.allocated_amount > v_amount_due THEN
                            RAISE EXCEPTION
                                'Over-allocation: obligation % would carry % allocated against amount_due %',
                                NEW.obligation_payment_id, v_total + NEW.allocated_amount, v_amount_due
                                USING ERRCODE = '23514';
                        END IF;
                    END IF;

                    -- Receiving side: cannot allocate out more than was physically received.
                    SELECT rp.amount_due
                      INTO v_amount_due
                      FROM rent_payments rp
                     WHERE rp.id = NEW.receiving_payment_id;

                    IF FOUND THEN
                        SELECT COALESCE(SUM(pa.allocated_amount), 0)
                          INTO v_total
                          FROM payment_allocations pa
                         WHERE pa.receiving_payment_id = NEW.receiving_payment_id
                           AND pa.allocation_status = 'active'
                           AND pa.deleted_at IS NULL
                           AND pa.id <> NEW.id;

                        IF v_total + NEW.allocated_amount > v_amount_due THEN
                            RAISE EXCEPTION
                                'Over-allocation: receiving payment % would have % allocated out of received %',
                                NEW.receiving_payment_id, v_total + NEW.allocated_amount, v_amount_due
                                USING ERRCODE = '23514';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $body$;

                CREATE TRIGGER trg_payment_allocations_enforce_limits
                BEFORE INSERT OR UPDATE ON payment_allocations
                FOR EACH ROW
                EXECUTE FUNCTION enforce_payment_allocation_limits();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_payment_allocations_enforce_limits ON payment_allocations;
                DROP FUNCTION IF EXISTS enforce_payment_allocation_limits();
            ");
        }
    }
}

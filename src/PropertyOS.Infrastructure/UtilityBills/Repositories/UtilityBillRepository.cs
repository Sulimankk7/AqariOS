using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using PropertyOS.Application.UtilityBills;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.UtilityBills.Repositories;

internal sealed class UtilityBillRepository : IUtilityBillRepository
{
    private readonly PropertyOsDbContext _context;

    public UtilityBillRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Inserts a utility bill using ON CONFLICT DO NOTHING for idempotency.
    ///
    /// EF Core does not support ON CONFLICT DO NOTHING natively, so we use
    /// raw SQL for the upsert and detect whether a row was inserted by
    /// checking the rows-affected count.
    ///
    /// Returns true if the bill was newly inserted, false if it already existed.
    /// </summary>
    public async Task<bool> InsertIfNewAsync(UtilityBill bill, CancellationToken cancellationToken = default)
    {
        // Use raw SQL to get ON CONFLICT DO NOTHING semantics.
        // This is the database-level uniqueness protection against duplicates.
        var sql = """
            INSERT INTO utility_bills (
                id, company_id, utility_account_id, utility_type,
                provider_external_id, bill_date, due_date,
                amount, currency, is_paid, payment_status,
                provider_reference, is_from_historical_backfill,
                discovered_at, notification_sent_at,
                created_at, updated_at
            )
            VALUES (
                {0}, {1}, {2}, {3}::utility_type_enum,
                {4}, {5}, {6},
                {7}, {8}, {9}, {10}::utility_bill_status_enum,
                {11}, {12},
                {13}, NULL,
                {14}, {15}
            )
            ON CONFLICT (utility_account_id, provider_external_id) DO NOTHING
            """;

        var dueDateParameter = new NpgsqlParameter("due_date", NpgsqlDbType.Date)
        {
            Value = (object?)bill.DueDate ?? DBNull.Value
        };
        var providerReferenceParameter = new NpgsqlParameter("provider_reference", NpgsqlDbType.Text)
        {
            Value = (object?)bill.ProviderReference ?? DBNull.Value
        };

        int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            sql,
            parameters: new object[]
            {
                bill.Id,
                bill.CompanyId,
                bill.UtilityAccountId,
                bill.UtilityType.ToString().ToLowerInvariant(),
                bill.ProviderExternalId,
                bill.BillDate,
                dueDateParameter,
                bill.Amount,
                bill.Currency,
                bill.IsPaid,
                bill.PaymentStatus.ToString().ToLowerInvariant(),
                providerReferenceParameter,
                bill.IsFromHistoricalBackfill,
                bill.DiscoveredAt,
                bill.CreatedAt,
                bill.UpdatedAt
            },
            cancellationToken: cancellationToken);

        return rowsAffected > 0; // true = newly inserted; false = conflict (already existed)
    }

    public async Task<List<UtilityBill>> GetUnnotifiedUnpaidBillsAsync(
        Guid utilityAccountId,
        CancellationToken cancellationToken = default)
    {
        // Uses idx_utility_bills_notification_pending partial index
        return await _context.UtilityBills
            .Where(b => b.UtilityAccountId == utilityAccountId
                     && b.NotificationSentAt == null
                     && !b.IsPaid
                     && !b.IsFromHistoricalBackfill)
            .OrderBy(b => b.BillDate)
            .ToListAsync(cancellationToken);
    }
}

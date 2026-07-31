using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials;

public interface IRentPaymentRepository
{
    Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the given rent payments with row-level locks (SELECT ... FOR UPDATE), in
    /// deterministic ascending-Id order to prevent deadlocks. Every allocation writer
    /// (record / reverse / cheque bounce cascade) MUST acquire these locks before reading
    /// allocation sums — this is what makes the read-modify-write settlement math safe
    /// under concurrency. Soft-deleted rows are excluded.
    /// </summary>
    Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default);
    Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    /// <summary>
    /// All existing (non-deleted) scheduled-installment billing periods for a contract, in
    /// one round trip — the installment generator diffs against this set instead of probing
    /// per period (a 12-month contract previously cost 13 EXISTS queries).
    /// </summary>
    Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the IDs of non-deleted scheduled installments whose cached settlement
    /// status is still Pending although their due date has passed (DueDate &lt; asOfDate).
    /// Used by the overdue sweep job: keyset cursor over Id — returns up to batchSize IDs
    /// strictly greater than <paramref name="afterId"/> (all rows when null), ordered by Id.
    /// Poison-id skipping is the job's responsibility (client-side), not the repository's.
    /// </summary>
    Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The owning company's configured rent grace period in days
    /// (company_settings.rent_grace_period_days; platform default when no settings row exists).
    /// Consulted by every settlement-status derivation per Module 6 doc §6.1.
    /// </summary>
    Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default);

    Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly tracks a receipt created by RentPayment.IssueReceipt as Added. Required
    /// because receipts carry client-generated IDs: navigation-only discovery would track
    /// them as Modified (assumed existing) and the save would fail.
    /// </summary>
    Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default);

    Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default);
    Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default);

    // Read-side projections. Every method is explicitly tenant-scoped: plain queries run
    // outside a transaction, so RLS tenant context is not guaranteed — the companyId
    // predicate is the enforced boundary here (Architecture §7).
    Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default);
    Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default);

    // Rent Payment Receipt read-side
    Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default);
}

public record RentPaymentReceiptFilterOptions(
    Guid? LeaseContractId = null,
    Guid? TenantId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenIssueDate = null,
    int PageSize = 50
);

/// <summary>An existing scheduled-installment billing period (start/end pair).</summary>
public readonly record struct BillingPeriod(DateOnly Start, DateOnly End);

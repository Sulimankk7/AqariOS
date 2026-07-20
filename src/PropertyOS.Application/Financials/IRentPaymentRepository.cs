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
    Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default);
    Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);

    Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default);

    Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default);

    Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default);
    Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default);

    Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(CancellationToken cancellationToken = default);
    Task<List<ChequeDetailDto>> GetChequesByStatusAsync(ChequeStatus status, CancellationToken cancellationToken = default);
    Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, CancellationToken cancellationToken = default);

    // Rent Payment Receipt read-side
    Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, CancellationToken cancellationToken = default);
    Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, CancellationToken cancellationToken = default);
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

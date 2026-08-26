using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Queries.GetRentPaymentSettlementStatementPdf;

public class GetRentPaymentSettlementStatementPdfQueryHandler : IRequestHandler<GetRentPaymentSettlementStatementPdfQuery, SettlementStatementFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IReceiptPdfGenerator _pdfGenerator;
    private readonly IBusinessClock _clock;

    public GetRentPaymentSettlementStatementPdfQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IReceiptPdfGenerator pdfGenerator,
        IBusinessClock clock)
    {
        _context = context;
        _tenantContext = tenantContext;
        _pdfGenerator = pdfGenerator;
        _clock = clock;
    }

    public async Task<SettlementStatementFileDto> Handle(GetRentPaymentSettlementStatementPdfQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Company context is required.");

        var rentPayment = await _context.RentPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.RentPaymentId && p.CompanyId == companyId && p.DeletedAt == null, cancellationToken);

        if (rentPayment == null)
            throw new NotFoundException($"Rent payment with ID {request.RentPaymentId} was not found.");

        if (request.TenantId.HasValue && rentPayment.TenantId != request.TenantId.Value)
            throw new NotFoundException($"Rent payment with ID {request.RentPaymentId} was not found.");

        // Settlement Statement is strictly available ONLY when fully paid
        if (rentPayment.DueDateStatus != DueDateStatus.Paid || rentPayment.AmountPaid != rentPayment.AmountDue)
        {
            throw new BusinessRuleException(
                "Final settlement statement is only available for fully settled installments.",
                "SETTLEMENT_NOT_AVAILABLE");
        }

        // Fetch display context entities
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == rentPayment.TenantId, cancellationToken);
        var building = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == rentPayment.BuildingId, cancellationToken);
        var apartment = await _context.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == rentPayment.ApartmentId, cancellationToken);
        var contract = await _context.LeaseContracts.AsNoTracking().FirstOrDefaultAsync(lc => lc.Id == rentPayment.LeaseContractId, cancellationToken);

        // Fetch all active payment allocations for this obligation
        var allocations = await (
            from a in _context.PaymentAllocations.AsNoTracking()
            where a.ObligationPaymentId == rentPayment.Id
               && a.AllocationStatus == AllocationStatus.Active
               && a.DeletedAt == null
            join rcv in _context.RentPayments.AsNoTracking() on a.ReceivingPaymentId equals rcv.Id
            join r in _context.RentPaymentReceipts.AsNoTracking() on rcv.Id equals r.RentPaymentId into rs
            from r in rs.Where(x => x.DeletedAt == null).DefaultIfEmpty()
            orderby a.AllocationDate, a.CreatedAt
            select new
            {
                Allocation = a,
                Receiving = rcv,
                Receipt = r
            }
        ).ToListAsync(cancellationToken);

        var transactions = new List<SettlementTransactionItem>();
        int index = 1;

        foreach (var item in allocations)
        {
            transactions.Add(new SettlementTransactionItem(
                Index: index++,
                PaymentDate: item.Receipt != null ? item.Receipt.CreatedAt : item.Allocation.CreatedAt,
                Amount: item.Allocation.AllocatedAmount > 0 ? item.Allocation.AllocatedAmount : (item.Receipt?.Amount ?? 0m),
                PaymentMethod: item.Receiving.PaymentMethod?.ToString() ?? "BankTransfer",
                ReferenceNumber: item.Receiving.PaymentReferenceNumber,
                ReceiptNumber: item.Receipt?.ReceiptNumber ?? "REC-PENDING"
            ));
        }

        // Direct receipt fallback if no separate allocations exist
        if (transactions.Count == 0 && !string.IsNullOrWhiteSpace(rentPayment.ReceiptNumber))
        {
            transactions.Add(new SettlementTransactionItem(
                Index: 1,
                PaymentDate: rentPayment.UpdatedAt,
                Amount: rentPayment.AmountPaid,
                PaymentMethod: rentPayment.PaymentMethod?.ToString() ?? "Cash",
                ReferenceNumber: rentPayment.PaymentReferenceNumber,
                ReceiptNumber: rentPayment.ReceiptNumber
            ));
        }

        var statementNumber = $"SETTLE-{rentPayment.ReceiptNumber ?? rentPayment.Id.ToString()[..8].ToUpperInvariant()}";
        var statementModel = new SettlementStatementPdfModel(
            StatementNumber: statementNumber,
            StatementDate: _clock.UtcNow,
            TenantName: tenant?.Name ?? "Tenant",
            TenantPhone: tenant?.Phone,
            PropertyName: building?.Name ?? "Property",
            UnitNumber: apartment?.UnitNumber ?? "Unit",
            ContractNumber: contract?.ContractNumber ?? "Contract",
            BillingPeriod: rentPayment.BillingPeriodStart.HasValue && rentPayment.BillingPeriodEnd.HasValue
                ? $"{rentPayment.BillingPeriodStart.Value:dd/MM/yyyy} - {rentPayment.BillingPeriodEnd.Value:dd/MM/yyyy}"
                : null,
            DueDate: rentPayment.DueDate?.ToString("dd/MM/yyyy"),
            TotalAmountDue: rentPayment.AmountDue,
            TotalAmountPaid: rentPayment.AmountPaid,
            RemainingBalance: Math.Max(0, rentPayment.AmountDue - rentPayment.AmountPaid),
            Currency: string.IsNullOrWhiteSpace(rentPayment.Currency) ? "JOD" : rentPayment.Currency,
            Status: rentPayment.DueDateStatus.ToString(),
            Transactions: transactions
        );

        var pdfBytes = await _pdfGenerator.GenerateSettlementStatementPdfAsync(statementModel, cancellationToken);

        return new SettlementStatementFileDto(
            Filename: $"Settlement_{statementNumber}.pdf",
            Content: pdfBytes,
            MimeType: "application/pdf"
        );
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;

public class GenerateScheduledInstallmentsCommandHandler : IRequestHandler<GenerateScheduledInstallmentsCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public GenerateScheduledInstallmentsCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IRentPaymentRepository rentPaymentRepository,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(GenerateScheduledInstallmentsCommand request, CancellationToken cancellationToken)
    {
        var contract = await _leaseContractRepository.GetByIdAsync(request.LeaseContractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException($"LeaseContract with ID {request.LeaseContractId} was not found.");

        if (contract.Status != ContractStatus.Active)
            throw new InvalidOperationException("Installments can only be generated for active contracts.");

        int intervalMonths = contract.PaymentFrequency switch
        {
            PaymentFrequency.Monthly => 1,
            PaymentFrequency.Quarterly => 3,
            PaymentFrequency.SemiAnnual => 6,
            PaymentFrequency.Annual => 12,
            _ => throw new InvalidOperationException($"Unsupported payment frequency: {contract.PaymentFrequency}")
        };

        var currentStart = contract.StartDate;
        var termEnd = contract.EndDate.AddDays(1); // Exclusive end boundary

        var newInstallments = new List<RentPayment>();

        while (currentStart < contract.EndDate)
        {
            var periodStart = currentStart;
            var periodEnd = currentStart.AddMonths(intervalMonths);

            // Clamp the period end date to the contract term boundary if it overflows
            if (periodEnd > termEnd)
            {
                periodEnd = termEnd;
            }

            // Check if a scheduled installment already exists for this contract and period
            var exists = await _rentPaymentRepository.HasScheduledInstallmentAsync(
                contract.Id,
                periodStart,
                periodEnd,
                cancellationToken);

            if (!exists)
            {
                decimal amountDue;
                var expectedEnd = periodStart.AddMonths(intervalMonths);

                if (expectedEnd == periodEnd)
                {
                    amountDue = contract.MonthlyRentAmount * intervalMonths;
                }
                else
                {
                    // Pro-rate based on days in truncated period relative to standard full interval days
                    decimal actualDays = periodEnd.DayNumber - periodStart.DayNumber;
                    decimal expectedDays = expectedEnd.DayNumber - periodStart.DayNumber;
                    amountDue = contract.MonthlyRentAmount * intervalMonths * (actualDays / expectedDays);
                }

                // Compute due date (payment_due_day of the starting month)
                var dueDate = new DateOnly(periodStart.Year, periodStart.Month, contract.PaymentDueDay);

                var payment = RentPayment.Create(
                    companyId: contract.CompanyId,
                    leaseContractId: contract.Id,
                    tenantId: contract.TenantId,
                    buildingId: contract.BuildingId,
                    apartmentId: contract.ApartmentId,
                    purpose: PaymentPurpose.ScheduledInstallment,
                    amountDue: amountDue,
                    currency: contract.Currency,
                    billingPeriodStart: periodStart,
                    billingPeriodEnd: periodEnd,
                    dueDate: dueDate,
                    createdAt: DateTimeOffset.UtcNow,
                    createdBy: _currentUserContext.UserId
                );

                newInstallments.Add(payment);
            }

            currentStart = periodEnd;
        }

        if (newInstallments.Count > 0)
        {
            await _rentPaymentRepository.AddRangeAsync(newInstallments, cancellationToken);
        }

        return Unit.Value;
    }
}

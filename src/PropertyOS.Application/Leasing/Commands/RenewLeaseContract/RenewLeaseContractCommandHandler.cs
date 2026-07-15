using MediatR;

using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.RenewLeaseContract;

public class RenewLeaseContractCommandHandler : IRequestHandler<RenewLeaseContractCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public RenewLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(RenewLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var priorContract = await _leaseContractRepository.GetByIdAsync(request.PriorContractId, cancellationToken);
        if (priorContract == null)
            throw new KeyNotFoundException($"LeaseContract with ID {request.PriorContractId} was not found.");

        if (priorContract.Status != ContractStatus.Active && priorContract.Status != ContractStatus.Expired)
            throw new InvalidOperationException($"Cannot renew a contract in {priorContract.Status} status.");

        if (DateOnly.FromDateTime(request.StartDate) < priorContract.EndDate)
            throw new InvalidOperationException("Renewal start date must be on or after the prior contract's end date.");

        var contractNumber = request.ContractNumber;

        var newContract = LeaseContract.Create(
            companyId: priorContract.CompanyId,
            buildingId: priorContract.BuildingId,
            apartmentId: priorContract.ApartmentId,
            tenantId: priorContract.TenantId,
            contractNumber: contractNumber,
            startDate: DateOnly.FromDateTime(request.StartDate),
            endDate: DateOnly.FromDateTime(request.EndDate),
            monthlyRentAmount: request.MonthlyRentAmount,
            securityDepositAmount: request.SecurityDepositAmount,
            paymentFrequency: request.PaymentFrequency,
            paymentDueDay: request.PaymentDueDay,

            legalRegime: request.LegalRegime,
            tenantType: request.TenantType,
            notes: request.Notes,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        newContract.SetPriorContractId(priorContract.Id);

        await _leaseContractRepository.AddAsync(newContract, cancellationToken);
        
        return Unit.Value;
    }
}

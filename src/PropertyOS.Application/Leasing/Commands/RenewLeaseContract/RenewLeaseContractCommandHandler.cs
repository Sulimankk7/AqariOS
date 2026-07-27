using MediatR;

using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.RenewLeaseContract;

public class RenewLeaseContractCommandHandler : IRequestHandler<RenewLeaseContractCommand, Guid>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public RenewLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, null!, currentUserContext)
    {
    }

    public RenewLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(RenewLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var priorContract = await _leaseContractRepository.GetByIdAsync(request.PriorContractId, cancellationToken);
        if (priorContract == null)
            throw new NotFoundException($"LeaseContract with ID {request.PriorContractId} was not found.");

        if (_tenantContext != null && _tenantContext.CompanyId.HasValue && priorContract.CompanyId != _tenantContext.CompanyId.Value)
            throw new NotFoundException($"LeaseContract with ID {request.PriorContractId} was not found.");

        if (priorContract.Status != ContractStatus.Active && priorContract.Status != ContractStatus.Expired)
            throw new BusinessRuleException($"Cannot renew a contract in {priorContract.Status} status.", "LEASE_RENEW_INVALID_STATUS");

        if (await _leaseContractRepository.HasSuccessorContractAsync(priorContract.Id, cancellationToken))
            throw new ConflictException("The contract already has a renewal successor.");

        if (DateOnly.FromDateTime(request.StartDate) < priorContract.EndDate)
            throw new BusinessRuleException("Renewal start date must be on or after the prior contract's end date.", "LEASE_RENEW_START_BEFORE_PRIOR_END");

        if (await _leaseContractRepository.HasOverlappingNonTerminalContractAsync(priorContract.ApartmentId, request.StartDate, request.EndDate, cancellationToken))
            throw new ConflictException("An overlapping draft, pending, or active contract already exists for this apartment.");

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

        var history = ContractStatusHistory.Create(
            companyId: priorContract.CompanyId,
            leaseContractId: newContract.Id,
            newStatus: newContract.Status,
            changedAt: DateTimeOffset.UtcNow,
            previousStatus: null,
            changedBy: _currentUserContext.UserId,
            reason: "Initial renewal draft created"
        );
        await _leaseContractRepository.AddStatusHistoryAsync(history, cancellationToken);

        return newContract.Id;
    }
}

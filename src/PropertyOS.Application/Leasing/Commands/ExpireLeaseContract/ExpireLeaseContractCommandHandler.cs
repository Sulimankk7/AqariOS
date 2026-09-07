using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;

public class ExpireLeaseContractCommandHandler : IRequestHandler<ExpireLeaseContractCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _businessClock;
    private readonly ITenantContext? _tenantContext;
    private readonly IParkingAssignmentRepository? _parkingAssignments;

    public ExpireLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext,
        IBusinessClock businessClock)
        : this(leaseContractRepository, currentUserContext, businessClock, null, null)
    {
    }

    public ExpireLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext,
        IBusinessClock businessClock,
        ITenantContext? tenantContext,
        IParkingAssignmentRepository? parkingAssignments)
    {
        _leaseContractRepository = leaseContractRepository;
        _currentUserContext = currentUserContext;
        _businessClock = businessClock;
        _tenantContext = tenantContext;
        _parkingAssignments = parkingAssignments;
    }

    public async Task<Unit> Handle(ExpireLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = _tenantContext?.CompanyId is Guid companyId
            ? await _leaseContractRepository.GetByIdForUpdateAsync(request.ContractId, companyId, cancellationToken)
            : await _leaseContractRepository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        if (contract.Status != ContractStatus.Active)
            throw new BusinessRuleException($"Cannot expire a contract in {contract.Status} status. Only Active contracts can be expired.", "LEASE_EXPIRE_INVALID_STATUS");

        var effectiveAsOf = request.AsOf ?? _businessClock.UtcNow;
        var jordanBusinessDate = _businessClock.GetJordanBusinessDate(effectiveAsOf);

        if (jordanBusinessDate < contract.EndDate)
            throw new BusinessRuleException("Contract term has not yet ended.", "LEASE_EXPIRE_TERM_NOT_ENDED");

        contract.Expire(effectiveAsOf, _currentUserContext.UserId);

        var history = ContractStatusHistory.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            newStatus: ContractStatus.Expired,
            changedAt: effectiveAsOf,
            previousStatus: ContractStatus.Active,
            changedBy: _currentUserContext.UserId,
            reason: "Automated lease contract expiration sweep"
        );

        await _leaseContractRepository.AddStatusHistoryAsync(history, cancellationToken);

        if (_parkingAssignments != null)
            await _parkingAssignments.EndActiveByLeaseAsync(contract.Id, contract.CompanyId, contract.EndDate,
                effectiveAsOf, _currentUserContext.UserId, cancellationToken);

        return Unit.Value;
    }
}

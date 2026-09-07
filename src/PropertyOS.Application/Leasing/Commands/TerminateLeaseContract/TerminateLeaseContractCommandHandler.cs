using MediatR;

using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;

public class TerminateLeaseContractCommandHandler : IRequestHandler<TerminateLeaseContractCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext? _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IParkingAssignmentRepository? _parkingAssignments;

    public TerminateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, null, currentUserContext, null)
    {
    }

    public TerminateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ITenantContext? tenantContext,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, tenantContext, currentUserContext, null)
    {
    }

    public TerminateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ITenantContext? tenantContext,
        ICurrentUserContext currentUserContext,
        IParkingAssignmentRepository? parkingAssignments)
    {
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _parkingAssignments = parkingAssignments;
    }

    public async Task<Unit> Handle(TerminateLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = _tenantContext?.CompanyId is Guid companyId
            ? await _leaseContractRepository.GetByIdForUpdateAsync(request.ContractId, companyId, cancellationToken)
            : await _leaseContractRepository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        if (_tenantContext != null && _tenantContext.CompanyId.HasValue && contract.CompanyId != _tenantContext.CompanyId.Value)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        if (contract.Status != ContractStatus.Active)
            throw new BusinessRuleException($"Cannot terminate a contract in {contract.Status} status.", "LEASE_TERMINATE_INVALID_STATUS");

        var terminationDate = DateOnly.FromDateTime(request.TerminationDate);
        if (terminationDate < contract.StartDate)
            throw new BusinessRuleException("Termination date cannot be before the contract's start date.", "LEASE_TERMINATE_DATE_BEFORE_START");

        if (terminationDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessRuleException("Termination date cannot be in the future.", "LEASE_TERMINATE_DATE_IN_FUTURE");

        if (request.DepositDeductionAmount > 0 && string.IsNullOrWhiteSpace(request.DepositDeductionReason))
            throw new BusinessRuleException("Deposit deduction reason is required when deduction amount is greater than zero.", "LEASE_TERMINATE_DEDUCTION_REASON_REQUIRED");

        var oldStatus = contract.Status;
        contract.Terminate(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        var termination = ContractTermination.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            terminationType: request.TerminationType,
            terminationDate: DateOnly.FromDateTime(request.TerminationDate),
            outstandingBalance: request.OutstandingBalance,
            depositReturnedAmount: request.DepositReturnedAmount,
            depositDeductionAmount: request.DepositDeductionAmount,
            depositDeductionReason: request.DepositDeductionReason,
            finalUtilitySettlementCompleted: request.FinalUtilitySettlementCompleted,
            currency: contract.Currency,
            reason: request.Reason,
            notes: request.Notes,
            approvedBy: _currentUserContext.UserId,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        await _leaseContractRepository.AddTerminationAsync(termination, cancellationToken);

        var history = ContractStatusHistory.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            newStatus: contract.Status,
            changedAt: DateTimeOffset.UtcNow,
            previousStatus: oldStatus,
            changedBy: _currentUserContext.UserId,
            reason: request.Reason ?? "Contract terminated"
        );

        await _leaseContractRepository.AddStatusHistoryAsync(history, cancellationToken);

        if (_parkingAssignments != null)
            await _parkingAssignments.EndActiveByLeaseAsync(contract.Id, contract.CompanyId, terminationDate,
                contract.UpdatedAt, _currentUserContext.UserId, cancellationToken);

        // SaveChangesAsync is owned by TransactionBehavior

        return Unit.Value;
    }
}

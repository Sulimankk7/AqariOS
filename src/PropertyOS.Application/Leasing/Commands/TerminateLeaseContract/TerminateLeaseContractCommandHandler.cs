using MediatR;

using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;

public class TerminateLeaseContractCommandHandler : IRequestHandler<TerminateLeaseContractCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public TerminateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(TerminateLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _leaseContractRepository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        if (contract.Status != ContractStatus.Active)
            throw new InvalidOperationException($"Cannot terminate a contract in {contract.Status} status.");

        var terminationDate = DateOnly.FromDateTime(request.TerminationDate);
        if (terminationDate < contract.StartDate)
            throw new InvalidOperationException("Termination date cannot be before the contract's start date.");

        if (terminationDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidOperationException("Termination date cannot be in the future.");

        if (request.DepositDeductionAmount > 0 && string.IsNullOrWhiteSpace(request.DepositDeductionReason))
            throw new InvalidOperationException("Deposit deduction reason is required when deduction amount is greater than zero.");

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

        // SaveChangesAsync is owned by TransactionBehavior

        return Unit.Value;
    }
}

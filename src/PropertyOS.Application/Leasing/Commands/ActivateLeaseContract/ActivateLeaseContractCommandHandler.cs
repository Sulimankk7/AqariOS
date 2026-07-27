using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Marketplace;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Properties;


namespace PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;

public class ActivateLeaseContractCommandHandler : IRequestHandler<ActivateLeaseContractCommand, Unit>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IMarketplaceListingRepository _marketplaceListingRepository;
    private readonly ITenantContext? _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ActivateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, null!, null, currentUserContext)
    {
    }

    public ActivateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IMarketplaceListingRepository marketplaceListingRepository,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, marketplaceListingRepository, null, currentUserContext)
    {
    }

    public class NullMarketplaceListingRepository : IMarketplaceListingRepository
    {
        public Task<MarketplaceListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<MarketplaceListing?>(null);
        public Task<MarketplaceListing?> GetActiveListingByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default) => Task.FromResult<MarketplaceListing?>(null);
        public Task AddAsync(MarketplaceListing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Apartment?> GetApartmentByIdAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Apartment?>(null);
        public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    public ActivateLeaseContractCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IMarketplaceListingRepository marketplaceListingRepository,
        ITenantContext? tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository;
        _marketplaceListingRepository = marketplaceListingRepository ?? new NullMarketplaceListingRepository();
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ActivateLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var contract = await _leaseContractRepository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        if (_tenantContext != null && _tenantContext.CompanyId.HasValue && contract.CompanyId != _tenantContext.CompanyId.Value)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        // 1. Prevent invalid status transitions
        if (contract.Status != ContractStatus.Draft && contract.Status != ContractStatus.PendingSignature)
            throw new BusinessRuleException($"Cannot activate a contract in {contract.Status} status. Only Draft or PendingSignature are allowed.", "LEASE_ACTIVATE_INVALID_STATUS");

        // 2. Cannot activate a contract before its start date
        if (contract.StartDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessRuleException("Cannot activate a contract before its start date.", "LEASE_ACTIVATE_BEFORE_START_DATE");

        // 3. Workflow gate: signed contract document must exist
        var hasSignedDoc = await _leaseContractRepository.HasSignedContractDocumentAsync(contract.Id, cancellationToken);
        if (!hasSignedDoc)
            throw new BusinessRuleException("A signed contract document is required before activation.", "LEASE_ACTIVATE_SIGNED_DOCUMENT_REQUIRED");

        // 4. Overlap validation (excluding the current contract itself)
        var startDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
        if (await _leaseContractRepository.HasOverlappingNonTerminalContractAsync(contract.ApartmentId, startDate, endDate, contract.Id, cancellationToken))
            throw new ConflictException("An overlapping draft, pending, or active contract already exists for this apartment.");

        // 5. Active contract check & predecessor superseding in the same transaction
        if (contract.PriorContractId.HasValue)
        {
            var priorContract = await _leaseContractRepository.GetByIdAsync(contract.PriorContractId.Value, cancellationToken);
            if (priorContract == null)
                throw new NotFoundException($"Predecessor LeaseContract with ID {contract.PriorContractId.Value} was not found.");

            if (_tenantContext != null && _tenantContext.CompanyId.HasValue && priorContract.CompanyId != _tenantContext.CompanyId.Value)
                throw new NotFoundException($"Predecessor LeaseContract with ID {contract.PriorContractId.Value} was not found.");

            // Check if there is any other active contract on the apartment, excluding the predecessor contract
            if (await _leaseContractRepository.HasActiveContractForApartmentAsync(contract.ApartmentId, priorContract.Id, cancellationToken))
                throw new ConflictException("An active contract already exists for this apartment.");

            // Capture previousStatus BEFORE mutation
            var priorPreviousStatus = priorContract.Status;

            // Mutate predecessor status
            priorContract.SupersedeForRenewal(DateTimeOffset.UtcNow, _currentUserContext.UserId);

            // Record history for predecessor
            var priorHistory = ContractStatusHistory.Create(
                companyId: priorContract.CompanyId,
                leaseContractId: priorContract.Id,
                newStatus: ContractStatus.Superseded,
                changedAt: DateTimeOffset.UtcNow,
                previousStatus: priorPreviousStatus,
                changedBy: _currentUserContext.UserId,
                reason: "Superseded by renewal activation"
            );
            await _leaseContractRepository.AddStatusHistoryAsync(priorHistory, cancellationToken);
        }
        else
        {
            // Verify no active contract exists for the apartment
            if (await _leaseContractRepository.HasActiveContractForApartmentAsync(contract.ApartmentId, cancellationToken))
                throw new ConflictException("An active contract already exists for this apartment.");
        }

        // 6. Mutate and activate the current contract
        var previousStatus = contract.Status;
        contract.Activate(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        // 7. Record history for the activated contract
        var history = ContractStatusHistory.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            newStatus: ContractStatus.Active,
            changedAt: DateTimeOffset.UtcNow,
            previousStatus: previousStatus,
            changedBy: _currentUserContext.UserId,
            reason: "Activated contract"
        );
        await _leaseContractRepository.AddStatusHistoryAsync(history, cancellationToken);

        // 8. Marketplace Listing Reconciliation
        var activeListing = await _marketplaceListingRepository.GetActiveListingByApartmentIdAsync(contract.ApartmentId, cancellationToken);
        if (activeListing != null)
        {
            activeListing.TryMarkAsRented(DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }

        return Unit.Value;
    }
}

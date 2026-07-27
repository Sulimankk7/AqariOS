using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.Leasing.Commands.UpdateDraftLeaseContract;

public class UpdateDraftLeaseContractCommandHandler : IRequestHandler<UpdateDraftLeaseContractCommand, Unit>
{
    private readonly ILeasingReferenceRepository _referenceRepository;
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateDraftLeaseContractCommandHandler(
        ILeasingReferenceRepository referenceRepository,
        ILeaseContractRepository leaseContractRepository,
        ICurrentUserContext currentUserContext)
    {
        _referenceRepository = referenceRepository;
        _leaseContractRepository = leaseContractRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateDraftLeaseContractCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch target contract
        var contract = await _leaseContractRepository.GetByIdAsync(request.ContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"LeaseContract with ID {request.ContractId} was not found.");

        // 2. Enforce Draft status invariant
        if (contract.Status != ContractStatus.Draft)
            throw new BusinessRuleException($"Cannot update contract terms when contract is in {contract.Status} status. Only Draft contracts can be edited.", "LEASE_EDIT_NOT_DRAFT");

        // 3. Prevent edit if a signed contract document is already attached
        if (await _leaseContractRepository.HasSignedContractDocumentAsync(contract.Id, cancellationToken))
            throw new BusinessRuleException("Cannot edit contract terms after a signed contract document has been attached.", "LEASE_EDIT_SIGNED_DOCUMENT_ATTACHED");

        // 4. Renewal Draft Safety: Prevent changing ApartmentId/TenantId on renewal drafts & validate predecessor date boundary
        if (contract.PriorContractId.HasValue)
        {
            if (request.ApartmentId != contract.ApartmentId || request.TenantId != contract.TenantId)
                throw new BusinessRuleException("Cannot change apartment or tenant on a renewal contract draft.", "LEASE_EDIT_RENEWAL_IDENTITY_LOCKED");

            var priorContract = await _leaseContractRepository.GetByIdAsync(contract.PriorContractId.Value, cancellationToken);
            if (priorContract != null && DateOnly.FromDateTime(request.StartDate) < priorContract.EndDate)
                throw new BusinessRuleException("Renewal start date must be on or after the prior contract's end date.", "LEASE_RENEW_START_BEFORE_PRIOR_END");
        }

        // 5. Verify Apartment & BuildingId (reuse existing if ApartmentId unchanged)
        Guid buildingId;
        if (request.ApartmentId == contract.ApartmentId)
        {
            buildingId = contract.BuildingId;
        }
        else
        {
            var fetchedBuildingId = await _referenceRepository.GetApartmentBuildingIdAsync(request.ApartmentId, contract.CompanyId, cancellationToken);
            if (fetchedBuildingId == null)
                throw new NotFoundException($"Apartment with ID {request.ApartmentId} was not found.");
            buildingId = fetchedBuildingId.Value;
        }

        // 6. Verify Tenant exists (reuse existing if TenantId unchanged)
        if (request.TenantId != contract.TenantId)
        {
            var tenantExists = await _referenceRepository.TenantExistsAsync(request.TenantId, contract.CompanyId, cancellationToken);
            if (!tenantExists)
                throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        // 7. Non-terminal overlap revalidation with self-exclusion (only when ApartmentId or Dates change)
        var newStartDate = DateOnly.FromDateTime(request.StartDate);
        var newEndDate = DateOnly.FromDateTime(request.EndDate);
        if (request.ApartmentId != contract.ApartmentId || newStartDate != contract.StartDate || newEndDate != contract.EndDate)
        {
            if (await _leaseContractRepository.HasOverlappingNonTerminalContractAsync(request.ApartmentId, request.StartDate, request.EndDate, contract.Id, cancellationToken))
                throw new ConflictException("An overlapping draft, pending, or active contract already exists for this apartment.");
        }

        // 8. Apply Domain mutation
        contract.UpdateDraftTerms(
            apartmentId: request.ApartmentId,
            buildingId: buildingId,
            tenantId: request.TenantId,
            startDate: DateOnly.FromDateTime(request.StartDate),
            endDate: DateOnly.FromDateTime(request.EndDate),
            monthlyRentAmount: request.MonthlyRentAmount,
            securityDepositAmount: request.SecurityDepositAmount,
            paymentFrequency: request.PaymentFrequency,
            paymentDueDay: request.PaymentDueDay,
            legalRegime: request.LegalRegime,
            tenantType: request.TenantType,
            notes: request.Notes,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        // Note: Unit of work completion / SaveChanges is managed by TransactionBehavior
        return Unit.Value;
    }
}

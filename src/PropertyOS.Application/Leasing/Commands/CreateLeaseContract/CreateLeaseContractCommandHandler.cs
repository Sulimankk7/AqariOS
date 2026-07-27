using MediatR;

using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;


namespace PropertyOS.Application.Leasing.Commands.CreateLeaseContract;

public class CreateLeaseContractCommandHandler : IRequestHandler<CreateLeaseContractCommand, Guid>
{
    private readonly ILeasingReferenceRepository _referenceRepository;
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateLeaseContractCommandHandler(
        ILeasingReferenceRepository referenceRepository,
        ILeaseContractRepository leaseContractRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _referenceRepository = referenceRepository;
        _leaseContractRepository = leaseContractRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateLeaseContractCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Verify Apartment and get BuildingId
        var buildingId = await _referenceRepository.GetApartmentBuildingIdAsync(request.ApartmentId, companyId, cancellationToken);
        if (buildingId == null)
            throw new NotFoundException($"Apartment with ID {request.ApartmentId} was not found.");

        // 2. Verify Tenant exists
        var tenantExists = await _referenceRepository.TenantExistsAsync(request.TenantId, companyId, cancellationToken);
        if (!tenantExists)
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");

        // 3. Overlap validation
        if (await _leaseContractRepository.HasOverlappingNonTerminalContractAsync(request.ApartmentId, request.StartDate, request.EndDate, cancellationToken))
            throw new ConflictException("An overlapping draft, pending, or active contract already exists for this apartment.");

        // 4. Generate contract number is now passed via command
        var contractNumber = request.ContractNumber;

        // 5. Create Contract entity (initial status: draft)
        var contract = LeaseContract.Create(
            companyId: companyId,
            buildingId: buildingId.Value,
            apartmentId: request.ApartmentId,
            tenantId: request.TenantId,
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

        await _leaseContractRepository.AddAsync(contract, cancellationToken);

        var history = ContractStatusHistory.Create(
            companyId: companyId,
            leaseContractId: contract.Id,
            newStatus: contract.Status,
            changedAt: DateTimeOffset.UtcNow,
            previousStatus: null,
            changedBy: _currentUserContext.UserId,
            reason: "Initial contract creation"
        );
        await _leaseContractRepository.AddStatusHistoryAsync(history, cancellationToken);

        // Note: SaveChanges is owned by TransactionBehavior

        return contract.Id;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenant;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Fetch; cross-tenant access is masked as NotFound.
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");

        // 2. Normalize phone to E.164 (validator already guarantees parseability).
        var phone = TenantPhoneNumber.Normalize(request.Phone, request.PhoneCountryCode);

        if (await _tenantRepository.ExistsByPhoneAsync(phone, excludeTenantId: tenant.Id, cancellationToken))
            throw new ConflictException("An active tenant with this phone number already exists.");

        // 3. National-ID uniqueness within the company, excluding self.
        var nationalId = request.NationalId.Trim();
        if (await _tenantRepository.ExistsByNationalIdAsync(companyId, nationalId, excludeTenantId: tenant.Id, cancellationToken))
            throw new ConflictException($"A tenant with national ID '{nationalId}' already exists.");

        // 4. Apply domain mutation.
        tenant.UpdateDetails(
            name: request.Name,
            nationalId: nationalId,
            phone: phone,
            occupation: request.Occupation,
            employer: request.Employer,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId,
            email: request.Email,
            phoneCountryCode: null
        );

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}

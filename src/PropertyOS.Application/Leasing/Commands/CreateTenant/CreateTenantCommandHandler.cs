using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Normalize phone to E.164 (validator already guarantees parseability).
        var phone = TenantPhoneNumber.Normalize(request.Phone, request.PhoneCountryCode);

        if (await _tenantRepository.ExistsByPhoneAsync(phone, excludeTenantId: null, cancellationToken))
            throw new ConflictException("An active tenant with this phone number already exists.");

        // 2. Pre-check national-ID uniqueness within the company (friendly 409). The partial
        //    unique index uq_tenants_company_national_id remains the race-condition backstop.
        var nationalId = request.NationalId.Trim();
        if (await _tenantRepository.ExistsByNationalIdAsync(companyId, nationalId, excludeTenantId: null, cancellationToken))
            throw new ConflictException($"A tenant with national ID '{nationalId}' already exists.");

        // 3. Create the aggregate. UserId stays null: the account/invitation flow is deferred.
        var tenant = Tenant.Create(
            companyId: companyId,
            name: request.Name,
            nationalId: nationalId,
            phone: phone,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId,
            email: request.Email,
            occupation: request.Occupation,
            employer: request.Employer,
            userId: null,
            phoneCountryCode: null
        );

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        // Note: SaveChanges is owned by TransactionBehavior
        return tenant.Id;
    }
}

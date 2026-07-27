using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Companies.Commands.UpdateCompanySettings;

public class UpdateCompanySettingsCommandHandler : IRequestHandler<UpdateCompanySettingsCommand, Unit>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateCompanySettingsCommandHandler(
        ICompanyRepository companyRepository,
        ITenantContext tenantContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(UpdateCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        // Tenant scoping: a company may only update its own settings. Cross-tenant
        // requests are masked as 404; null CompanyId is fail-closed.
        if (!_tenantContext.IsPlatformAdmin && _tenantContext.CompanyId != request.CompanyId)
            throw new NotFoundException($"Settings for Company with ID {request.CompanyId} were not found.");

        var company = await _companyRepository.GetWithSettingsByIdAsync(request.CompanyId, cancellationToken);
        if (company == null || company.Settings == null)
            throw new NotFoundException($"Settings for Company with ID {request.CompanyId} were not found.");

        var now = DateTimeOffset.UtcNow;

        company.Settings.UpdateLateFeePolicy(request.LateFeeType, request.LateFeeValue, now);
        company.Settings.UpdateFiscalYearStartMonth(request.FiscalYearStartMonth, now);
        company.Settings.UpdateRentGracePeriodDays(request.RentGracePeriodDays, now);

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}

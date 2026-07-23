using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetCompanySettings;

public class GetCompanySettingsQueryHandler : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;

    public GetCompanySettingsQueryHandler(ICompanyRepository companyRepository, ITenantContext tenantContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CompanySettingsDto?> Handle(GetCompanySettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _companyRepository.GetSettingsByIdAsync(request.CompanyId, cancellationToken);
        if (settings == null)
            throw new KeyNotFoundException($"Company settings for ID {request.CompanyId} were not found.");
            
        return settings;
    }
}

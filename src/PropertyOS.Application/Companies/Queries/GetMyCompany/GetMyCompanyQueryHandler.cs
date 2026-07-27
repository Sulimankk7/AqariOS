using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetMyCompany;

public class GetMyCompanyQueryHandler : IRequestHandler<GetMyCompanyQuery, CompanyDetailDto?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;

    public GetMyCompanyQueryHandler(ICompanyRepository companyRepository, ITenantContext tenantContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CompanyDetailDto?> Handle(GetMyCompanyQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var company = await _companyRepository.GetDetailByIdAsync(companyId, cancellationToken);
        if (company == null)
            throw new NotFoundException($"Company with ID {companyId} was not found.");

        return company;
    }
}

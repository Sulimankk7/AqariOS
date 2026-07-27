using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies.Queries.Common;

namespace PropertyOS.Application.Companies.Queries.GetCompanyById;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDetailDto?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;

    public GetCompanyByIdQueryHandler(ICompanyRepository companyRepository, ITenantContext tenantContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CompanyDetailDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        // Tenant scoping: a company may only read its own record. Cross-tenant requests
        // are masked as 404 so the existence of other tenants is never disclosed.
        // Null CompanyId is fail-closed (denied) unless the caller is the platform admin.
        if (!_tenantContext.IsPlatformAdmin && _tenantContext.CompanyId != request.Id)
            throw new NotFoundException($"Company with ID {request.Id} was not found.");

        var company = await _companyRepository.GetDetailByIdAsync(request.Id, cancellationToken);
        if (company == null)
            throw new NotFoundException($"Company with ID {request.Id} was not found.");

        return company;
    }
}

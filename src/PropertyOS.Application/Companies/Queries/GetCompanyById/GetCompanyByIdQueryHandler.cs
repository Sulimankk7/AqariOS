using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        var company = await _companyRepository.GetDetailByIdAsync(request.Id, cancellationToken);
        if (company == null)
            throw new KeyNotFoundException($"Company with ID {request.Id} was not found.");
            
        return company;
    }
}

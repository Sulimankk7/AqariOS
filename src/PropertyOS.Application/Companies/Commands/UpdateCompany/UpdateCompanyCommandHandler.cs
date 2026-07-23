using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Unit>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (company == null)
            throw new KeyNotFoundException($"Company with ID {request.Id} was not found.");

        company.UpdateProfile(
            request.LegalName,
            request.DisplayName,
            request.PrimaryPhone,
            request.PrimaryEmail,
            DateTimeOffset.UtcNow,
            _currentUserContext.UserId
        );

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}

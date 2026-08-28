using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPlatformCompanies;

internal sealed class GetPlatformCompaniesQueryHandler : IRequestHandler<GetPlatformCompaniesQuery, IReadOnlyList<PlatformCompanyListItemDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformCompaniesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlatformCompanyListItemDto>> Handle(GetPlatformCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Companies
            .AsNoTracking()
            .Where(c => !c.DeletedAt.HasValue && c.IsActive)
            .OrderBy(c => c.LegalName)
            .Select(c => new PlatformCompanyListItemDto(c.Id, c.LegalName))
            .ToListAsync(cancellationToken);
    }
}

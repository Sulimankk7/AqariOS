using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Companies.Queries.Common;
using PropertyOS.Domain.Companies;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Companies.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public CompanyRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<CompanyDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .ProjectToType<CompanyDetailDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CompanySettingsDto?> GetSettingsByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CompanySettings
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .ProjectToType<CompanySettingsDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Company?> GetWithSettingsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Companies
            .Include(c => c.Settings)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}

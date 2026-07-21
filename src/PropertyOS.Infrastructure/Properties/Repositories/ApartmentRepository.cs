using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Properties.Repositories;

public class ApartmentRepository : IApartmentRepository
{
    private readonly PropertyOsDbContext _context;

    public ApartmentRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public Task<Apartment?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, cancellationToken);
    }

    public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Buildings
            .AnyAsync(b => b.Id == buildingId && b.CompanyId == companyId, cancellationToken);
    }
}

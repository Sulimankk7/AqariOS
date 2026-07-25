using System;
using System.Collections.Generic;
using System.Linq;
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
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Apartment?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, cancellationToken);
    }

    public Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Apartment?> GetWithHierarchyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(Apartment apartment, CancellationToken cancellationToken = default)
    {
        if (apartment == null) throw new ArgumentNullException(nameof(apartment));
        await _context.Apartments.AddAsync(apartment, cancellationToken);
    }

    public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Buildings
            .AnyAsync(b => b.Id == buildingId && b.CompanyId == companyId, cancellationToken);
    }

    public Task<bool> UnitNumberExistsInBuildingAsync(Guid buildingId, string unitNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unitNumber)) return Task.FromResult(false);

        var normalizedNumber = unitNumber.Trim().ToLower();
        return _context.Apartments
            .AnyAsync(a => a.BuildingId == buildingId && a.UnitNumber.ToLower() == normalizedNumber, cancellationToken);
    }

    public Task<List<Apartment>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .AsNoTracking()
            .Where(a => a.BuildingId == buildingId)
            .OrderBy(a => a.UnitNumber)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Apartment>> ListByFloorIdAsync(Guid floorId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .AsNoTracking()
            .Where(a => a.FloorId == floorId)
            .OrderBy(a => a.UnitNumber)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Apartment>> ListByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.UnitNumber)
            .ToListAsync(cancellationToken);
    }
}

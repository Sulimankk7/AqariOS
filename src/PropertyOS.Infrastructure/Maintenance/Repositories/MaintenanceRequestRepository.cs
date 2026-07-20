using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Maintenance;
using PropertyOS.Domain.Maintenance;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Maintenance.Repositories;

public class MaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private readonly PropertyOsDbContext _context;

    public MaintenanceRequestRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load aggregate with active attachments and comments included to enforce aggregate boundaries
        return _context.MaintenanceRequests
            .Include(r => r.Attachments)
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceRequests.AddAsync(request, cancellationToken);
    }

    public async Task AddStatusHistoryAsync(MaintenanceStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceStatusHistory.AddAsync(history, cancellationToken);
    }

    public Task<bool> BuildingExistsAsync(Guid buildingId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Buildings
            .AnyAsync(b => b.Id == buildingId && b.CompanyId == companyId, cancellationToken);
    }

    public Task<bool> ApartmentExistsAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Apartments
            .AnyAsync(a => a.Id == apartmentId && a.CompanyId == companyId, cancellationToken);
    }

    public Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _context.Tenants
            .AnyAsync(t => t.Id == tenantId && t.CompanyId == companyId, cancellationToken);
    }

    public Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // NOTE: Attachment file existence validation (via file_storage table check)
        // is intentionally deferred/skipped because the File storage module does not yet exist.
        // Once implemented, this can query: return _context.FileStorage.AnyAsync(f => f.Id == fileId, cancellationToken);
        // For now, to allow seamless test seeding and execution, we return true.
        return Task.FromResult(true);
    }
}

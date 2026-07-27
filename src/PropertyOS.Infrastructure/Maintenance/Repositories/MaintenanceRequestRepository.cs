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

    public async Task AddAttachmentAsync(MaintenanceRequestAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceRequestAttachments.AddAsync(attachment, cancellationToken);
    }

    public async Task AddCommentAsync(MaintenanceRequestComment comment, CancellationToken cancellationToken = default)
    {
        await _context.MaintenanceRequestComments.AddAsync(comment, cancellationToken);
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

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown. File existence validation is intentionally deferred until the
    /// File storage module (Module 10) is implemented. Do not call this method from
    /// any Module 8 handler. When the File module exists, replace this body with a
    /// real query against the file_storage table.
    /// </exception>
    public Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "FileExistsAsync is not implemented. " +
            "File existence validation is deferred until the File storage module (Module 10) is available. " +
            "Do not call this method from Module 8 handlers.");
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Maintenance;
using PropertyOS.Application.Maintenance.Queries.Common;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Maintenance.Repositories;

public class MaintenanceQueries : IMaintenanceQueries
{
    private readonly PropertyOsDbContext _context;

    public MaintenanceQueries(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceRequestDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new MaintenanceRequestDetailDto(
                r.Id,
                r.CompanyId,
                r.BuildingId,
                r.ApartmentId,
                r.TenantId,
                r.Title,
                r.Description,
                r.Category,
                r.Priority,
                r.Status,
                r.RequestDate,
                r.ClosedDate,
                r.InternalNotes,
                _context.MaintenanceRequestAttachments.Count(a => a.MaintenanceRequestId == r.Id && a.DeletedAt == null),
                _context.MaintenanceRequestComments.Count(c => c.MaintenanceRequestId == r.Id && c.DeletedAt == null),
                r.CreatedBy,
                r.CreatedAt,
                r.UpdatedBy,
                r.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MaintenanceRequestSummaryDto>> GetRequestsAsync(
        MaintenanceRequestFilterOptions filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MaintenanceRequests
            .AsNoTracking();

        // ── Apply filters (combined via AND) ─────────────────────────────────

        if (filter.BuildingId.HasValue)
            query = query.Where(r => r.BuildingId == filter.BuildingId.Value);

        if (filter.ApartmentId.HasValue)
            query = query.Where(r => r.ApartmentId == filter.ApartmentId.Value);

        if (filter.TenantId.HasValue)
            query = query.Where(r => r.TenantId == filter.TenantId.Value);

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(r => r.Priority == filter.Priority.Value);

        if (filter.Category.HasValue)
            query = query.Where(r => r.Category == filter.Category.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(r => r.RequestDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(r => r.RequestDate <= filter.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            // Trigram ILIKE search — maps to the GIN idx_maintenance_requests_title_trgm index in Postgres
            var search = filter.SearchText.Trim();
            query = query.Where(r => EF.Functions.ILike(r.Title, $"%{search}%"));
        }

        // ── Keyset Pagination (spec §8.7) ────────────────────────────────────
        // Composite sorting: request_date DESC, id ASC.
        // Formula: (r.request_date < lastDate) OR (r.request_date == lastDate AND r.id > lastId)
        if (filter.LastSeenRequestDate.HasValue && filter.LastSeenId.HasValue)
        {
            var lastDate = filter.LastSeenRequestDate.Value;
            var lastId = filter.LastSeenId.Value;

            query = query.Where(r =>
                r.RequestDate < lastDate ||
                (r.RequestDate == lastDate && r.Id.CompareTo(lastId) > 0));
        }

        // Apply sort: request_date DESC, id ASC
        query = query.OrderByDescending(r => r.RequestDate).ThenBy(r => r.Id);

        // Limit results to page size
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        query = query.Take(pageSize);

        // Project directly to DTO to avoid loading whole entities into memory
        return await query
            .Select(r => new MaintenanceRequestSummaryDto(
                r.Id,
                r.CompanyId,
                r.BuildingId,
                r.ApartmentId,
                r.TenantId,
                r.Title,
                r.Category,
                r.Priority,
                r.Status,
                r.RequestDate,
                r.ClosedDate,
                r.CreatedBy,
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MaintenanceAttachmentDto>> GetAttachmentsAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _context.MaintenanceRequestAttachments
            .AsNoTracking()
            .Where(a => a.MaintenanceRequestId == requestId)
            .Select(a => new MaintenanceAttachmentDto(
                a.Id,
                a.FileId ?? Guid.Empty, // Handle nullable FileId
                a.Description,
                a.UploadedBy,
                a.CreatedBy,
                a.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MaintenanceCommentDto>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _context.MaintenanceRequestComments
            .AsNoTracking()
            .Where(c => c.MaintenanceRequestId == requestId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new MaintenanceCommentDto(
                c.Id,
                c.CommentText,
                c.CreatedBy,
                c.CreatedAt,
                c.UpdatedBy,
                c.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MaintenanceStatusHistoryDto>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _context.MaintenanceStatusHistory
            .AsNoTracking()
            .Where(h => h.MaintenanceRequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new MaintenanceStatusHistoryDto(
                h.Id,
                h.PreviousStatus,
                h.NewStatus,
                h.ChangedBy,
                h.ChangedAt,
                h.Reason
            ))
            .ToListAsync(cancellationToken);
    }
}

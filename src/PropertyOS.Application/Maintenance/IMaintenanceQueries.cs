using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Maintenance.Queries.Common;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance;

/// <summary>
/// Read-side query service for maintenance requests.
/// All methods return projections (DTOs) — never tracked domain entities.
///
/// <para>CQRS separation: this interface is injected only into query handlers.</para>
/// </summary>
public interface IMaintenanceQueries
{
    Task<MaintenanceRequestDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<MaintenanceRequestSummaryDto>> GetRequestsAsync(
        MaintenanceRequestFilterOptions filter,
        CancellationToken cancellationToken = default);

    Task<List<MaintenanceAttachmentDto>> GetAttachmentsAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<List<MaintenanceCommentDto>> GetCommentsAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<List<MaintenanceStatusHistoryDto>> GetStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter options for <see cref="IMaintenanceQueries.GetRequestsAsync"/>.
/// All filters are optional; combined via AND. Supports keyset pagination.
/// </summary>
public record MaintenanceRequestFilterOptions(
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    Guid? TenantId = null,
    MaintenanceStatus? Status = null,
    MaintenancePriority? Priority = null,
    MaintenanceCategory? Category = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    /// <summary>
    /// Substring match on <c>title</c> using PostgreSQL trigram ILIKE.
    /// Leverages <c>idx_maintenance_requests_title_trgm</c> GIN index.
    /// </summary>
    string? SearchText = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenRequestDate = null,
    int PageSize = 50);

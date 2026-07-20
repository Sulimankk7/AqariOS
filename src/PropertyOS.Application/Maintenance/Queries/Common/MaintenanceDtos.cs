using System;
using System.Collections.Generic;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Queries.Common;

/// <summary>
/// Lightweight list projection of a <c>maintenance_request</c> row.
/// Used by dashboard tiles, worklists, and paginated indexes.
/// </summary>
public record MaintenanceRequestSummaryDto(
    Guid Id,
    Guid CompanyId,
    Guid BuildingId,
    Guid? ApartmentId,
    Guid? TenantId,
    string Title,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    MaintenanceStatus Status,
    DateOnly RequestDate,
    DateOnly? ClosedDate,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt);

/// <summary>
/// Full detail projection including attachment and comment counts.
/// Used by the request detail page.
/// </summary>
public record MaintenanceRequestDetailDto(
    Guid Id,
    Guid CompanyId,
    Guid BuildingId,
    Guid? ApartmentId,
    Guid? TenantId,
    string Title,
    string Description,
    MaintenanceCategory Category,
    MaintenancePriority Priority,
    MaintenanceStatus Status,
    DateOnly RequestDate,
    DateOnly? ClosedDate,
    string? InternalNotes,
    int AttachmentCount,
    int CommentCount,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    Guid? UpdatedBy,
    DateTimeOffset UpdatedAt);

/// <summary>Projection of a single attachment row.</summary>
public record MaintenanceAttachmentDto(
    Guid Id,
    Guid FileId,
    string? Description,
    Guid? UploadedBy,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt);

/// <summary>Projection of a single comment row.</summary>
public record MaintenanceCommentDto(
    Guid Id,
    string CommentText,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    Guid? UpdatedBy,
    DateTimeOffset UpdatedAt);

/// <summary>Projection of a single status-history row.</summary>
public record MaintenanceStatusHistoryDto(
    Guid Id,
    MaintenanceStatus? PreviousStatus,
    MaintenanceStatus NewStatus,
    Guid? ChangedBy,
    DateTimeOffset ChangedAt,
    string? Reason);

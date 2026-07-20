using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Maintenance.Queries.Common;
using PropertyOS.Domain.Maintenance.Enums;

namespace PropertyOS.Application.Maintenance.Queries.GetMaintenanceRequests;

public record GetMaintenanceRequestsQuery(
    Guid? BuildingId = null,
    Guid? ApartmentId = null,
    Guid? TenantId = null,
    MaintenanceStatus? Status = null,
    MaintenancePriority? Priority = null,
    MaintenanceCategory? Category = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? SearchText = null,
    Guid? LastSeenId = null,
    DateOnly? LastSeenRequestDate = null,
    int PageSize = 50
) : IRequest<List<MaintenanceRequestSummaryDto>>;

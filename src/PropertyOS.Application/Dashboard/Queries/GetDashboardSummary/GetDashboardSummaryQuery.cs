using MediatR;
using PropertyOS.Application.Dashboard.DTOs;

namespace PropertyOS.Application.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// CQRS Query for retrieving aggregated dashboard KPI summary metrics.
/// </summary>
public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;

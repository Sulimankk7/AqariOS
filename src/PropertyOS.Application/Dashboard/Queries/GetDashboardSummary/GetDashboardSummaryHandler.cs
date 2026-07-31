using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Dashboard.DTOs;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties.Enums;
namespace PropertyOS.Application.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// CQRS Query Handler for computing dashboard KPI summary metrics directly from EF Core via high-performance SQL aggregate queries.
/// </summary>
public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetDashboardSummaryHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
        {
            throw new UnauthorizedAccessException("Tenant company identity is missing or invalid.");
        }

        var companyId = _tenantContext.CompanyId.Value;
        var nowUtc = DateTime.UtcNow;
        var todayDateOnly = DateOnly.FromDateTime(nowUtc);
        var thirtyDaysFromNowDateOnly = todayDateOnly.AddDays(30);
        var startOfMonthOffset = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var startOfMonthDateOnly = new DateOnly(nowUtc.Year, nowUtc.Month, 1);

        // Execute single high-performance SQL query projecting all aggregates in one database round trip.
        // We project into an intermediate anonymous type to query identical count expressions exactly once.
        var summaryTemp = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new
            {
                TotalBuildings = _dbContext.Buildings.Count(b => b.CompanyId == companyId && b.IsActive),
                TotalApartments = _dbContext.Apartments.Count(a => a.CompanyId == companyId && a.IsActive),
                OccupiedApartments = _dbContext.Apartments.Count(a => a.CompanyId == companyId && a.IsActive && a.OccupancyStatus == OccupancyStatus.Occupied),
                ActiveLeases = _dbContext.LeaseContracts.Count(l => l.CompanyId == companyId && l.Status == ContractStatus.Active),
                ExpiringIn30Days = _dbContext.LeaseContracts.Count(l => l.CompanyId == companyId && l.Status == ContractStatus.Active && l.EndDate >= todayDateOnly && l.EndDate <= thirtyDaysFromNowDateOnly),
                NewLeasesThisMonth = _dbContext.LeaseContracts.Count(l => l.CompanyId == companyId && l.StartDate >= startOfMonthDateOnly),
                CollectedThisMonth = _dbContext.RentPayments
                    .Where(p => p.CompanyId == companyId && p.CreatedAt >= startOfMonthOffset)
                    .Sum(p => (decimal?)p.AmountPaid) ?? 0m,
                OutstandingAmount = _dbContext.RentPayments
                    .Where(p => p.CompanyId == companyId && p.DueDate < todayDateOnly && p.AmountPaid < p.AmountDue)
                    .Sum(p => (decimal?)(p.AmountDue - p.AmountPaid)) ?? 0m,
                OverduePayments = _dbContext.RentPayments
                    .Count(p => p.CompanyId == companyId && (
                        p.DueDateStatus == DueDateStatus.Late || 
                        p.DueDateStatus == DueDateStatus.OverdueUnpaid || 
                        (p.DueDate < todayDateOnly && p.AmountPaid < p.AmountDue))),
                ExpensesThisMonth = _dbContext.Expenses
                    .Where(e => e.CompanyId == companyId && e.ExpenseDate >= startOfMonthDateOnly)
                    .Sum(e => (decimal?)e.Amount) ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (summaryTemp == null)
        {
            return new DashboardSummaryDto();
        }

        return new DashboardSummaryDto
        {
            Property = new PropertySummaryDto
            {
                TotalBuildings = summaryTemp.TotalBuildings,
                TotalApartments = summaryTemp.TotalApartments,
                OccupiedApartments = summaryTemp.OccupiedApartments,
                VacantApartments = summaryTemp.TotalApartments - summaryTemp.OccupiedApartments,
                OccupancyRate = summaryTemp.TotalApartments > 0 
                    ? Math.Round((double)summaryTemp.OccupiedApartments / summaryTemp.TotalApartments * 100, 2)
                    : 0.0
            },
            Leasing = new LeasingSummaryDto
            {
                ActiveLeases = summaryTemp.ActiveLeases,
                ExpiringIn30Days = summaryTemp.ExpiringIn30Days,
                NewLeasesThisMonth = summaryTemp.NewLeasesThisMonth
            },
            Payments = new PaymentSummaryDto
            {
                CollectedThisMonth = summaryTemp.CollectedThisMonth,
                OutstandingAmount = summaryTemp.OutstandingAmount,
                OverduePayments = summaryTemp.OverduePayments
            },
            Financials = new FinancialSummaryDto
            {
                ExpensesThisMonth = summaryTemp.ExpensesThisMonth
            }
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Subscriptions.Services;

/// <summary>
/// Infrastructure service handling business logic and persistence operations for Subscriptions.
/// Strict Clean Architecture: EF Core entities never escape this layer or return through public interfaces.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly PropertyOsDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of SubscriptionService with EF Core DbContext.
    /// </summary>
    /// <param name="dbContext">The application database context.</param>
    public SubscriptionService(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<SubscriptionPlanDto>> GetActivePlansAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Code = p.Code,
                NameEn = p.NameEn,
                NameAr = p.NameAr,
                DescriptionEn = p.DescriptionEn,
                DescriptionAr = p.DescriptionAr,
                MonthlyPrice = p.MonthlyPrice,
                YearlyPrice = p.YearlyPrice,
                Currency = p.Currency,
                MaxBuildings = p.MaxBuildings,
                MaxUsers = p.MaxUsers,
                MaxStorageMb = p.MaxStorageMb,
                FeatureFlags = p.FeatureFlags,
                SupportsTrial = p.SupportsTrial,
                TrialDurationDays = p.TrialDurationDays,
                IsActive = p.IsActive,
                SortOrder = p.SortOrder
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<SubscriptionPlanDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto?> GetUserSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var companyId = await ResolveCompanyIdForUserAsync(userId, cancellationToken);
        if (!companyId.HasValue)
        {
            return null;
        }

        var subscription = await _dbContext.CompanySubscriptions
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserSubscriptionDto
            {
                Id = s.Id,
                CompanyId = s.CompanyId,
                PlanId = s.PlanId,
                PlanCode = s.Plan.Code,
                PlanNameEn = s.Plan.NameEn,
                PlanNameAr = s.Plan.NameAr,
                Status = s.Status.ToString(),
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                TrialEndDate = s.TrialEndDate,
                PriceAtSubscription = s.PriceAtSubscription,
                CurrencyAtSubscription = s.CurrencyAtSubscription,
                BillingCycle = s.BillingCycle.ToString(),
                AutoRenew = s.AutoRenew,
                SuspendedAt = s.SuspendedAt,
                SuspensionReason = s.SuspensionReason,
                CancelledAt = s.CancelledAt,
                CancellationReason = s.CancellationReason,
                ExpiredAt = s.ExpiredAt,
                ExternalBillingRef = s.ExternalBillingRef,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return subscription;
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto> SubscribeAsync(
        Guid userId,
        CreateSubscriptionRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        await Task.CompletedTask;
        throw new BusinessRuleException(
            "Tenant self-service subscription creation is retired. Subscriptions must be created by platform administration.",
            "SUBSCRIPTION_DIRECT_CREATE_RETIRED");
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto> ChangePlanAsync(
        Guid userId,
        ChangePlanRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        await Task.CompletedTask;
        throw new BusinessRuleException(
            "Direct tenant plan changes are retired. Company administrators must submit a plan change request.",
            "SUBSCRIPTION_DIRECT_PLAN_CHANGE_RETIRED");
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto> CancelSubscriptionAsync(
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new BusinessRuleException(
            "Tenant self-service subscription cancellation is retired until a business rule explicitly defines it.",
            "SUBSCRIPTION_DIRECT_CANCEL_RETIRED");
    }

    private async Task<Guid?> ResolveCompanyIdForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Query UserCompanyRoles for active membership
        var companyId = await _dbContext.UserCompanyRoles
            .AsNoTracking()
            .Where(ucr => ucr.UserId == userId && ucr.DeletedAt == null)
            .Select(ucr => (Guid?)ucr.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (companyId.HasValue)
        {
            return companyId;
        }

        // Check if company has CreatedBy == userId
        var companyByOwner = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.CreatedBy == userId && c.DeletedAt == null)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return companyByOwner;
    }

}

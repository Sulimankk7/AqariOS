using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using PropertyOS.Domain.Identity.Enums;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;
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

        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException($"Subscription plan with ID '{dto.PlanId}' was not found or is inactive.");
        }

        var companyId = await GetOrCreateCompanyForUserAsync(userId, cancellationToken);

        // Check for existing active subscription
        var existingActive = await _dbContext.CompanySubscriptions
            .AsNoTracking()
            .AnyAsync(s => s.CompanyId == companyId &&
                           (s.Status == SubscriptionStatusEnum.Active ||
                            s.Status == SubscriptionStatusEnum.Trialing ||
                            s.Status == SubscriptionStatusEnum.PastDue), cancellationToken);

        if (existingActive)
        {
            throw new ConflictException("User or company already has an active subscription. Use change-plan endpoint to modify current subscription.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = dto.BillingCycle == BillingCycleEnum.Yearly
            ? today.AddYears(1)
            : today.AddMonths(1);

        var price = dto.BillingCycle == BillingCycleEnum.Yearly
            ? plan.YearlyPrice
            : plan.MonthlyPrice;

        DateOnly? trialEndDate = null;
        var initialStatus = SubscriptionStatusEnum.Active;

        if (plan.SupportsTrial && plan.TrialDurationDays.HasValue && plan.TrialDurationDays.Value > 0)
        {
            trialEndDate = today.AddDays(plan.TrialDurationDays.Value);
            initialStatus = SubscriptionStatusEnum.Trialing;
        }

        var subscription = new CompanySubscription
        {
            CompanyId = companyId,
            PlanId = plan.Id,
            Status = initialStatus,
            StartDate = today,
            EndDate = endDate,
            TrialEndDate = trialEndDate,
            PriceAtSubscription = price,
            CurrencyAtSubscription = plan.Currency,
            BillingCycle = dto.BillingCycle,
            AutoRenew = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.CompanySubscriptions.Add(subscription);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("uq_company_subscriptions_one_active") == true)
        {
            throw new ConflictException("A subscription for this company was created concurrently by another request.");
        }

        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            CompanyId = subscription.CompanyId,
            PlanId = subscription.PlanId,
            PlanCode = plan.Code,
            PlanNameEn = plan.NameEn,
            PlanNameAr = plan.NameAr,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            PriceAtSubscription = subscription.PriceAtSubscription,
            CurrencyAtSubscription = subscription.CurrencyAtSubscription,
            BillingCycle = subscription.BillingCycle.ToString(),
            AutoRenew = subscription.AutoRenew,
            SuspendedAt = subscription.SuspendedAt,
            SuspensionReason = subscription.SuspensionReason,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            ExpiredAt = subscription.ExpiredAt,
            ExternalBillingRef = subscription.ExternalBillingRef,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto> ChangePlanAsync(
        Guid userId,
        ChangePlanRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var newPlan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.NewPlanId && p.IsActive, cancellationToken);

        if (newPlan == null)
        {
            throw new NotFoundException($"Subscription plan with ID '{dto.NewPlanId}' was not found or is inactive.");
        }

        var companyId = await ResolveCompanyIdForUserAsync(userId, cancellationToken);
        if (!companyId.HasValue)
        {
            throw new NotFoundException("No active subscription found for user because no associated company exists.");
        }

        var subscription = await _dbContext.CompanySubscriptions
            .FirstOrDefaultAsync(s => s.CompanyId == companyId.Value &&
                                      (s.Status == SubscriptionStatusEnum.Active ||
                                       s.Status == SubscriptionStatusEnum.Trialing ||
                                       s.Status == SubscriptionStatusEnum.PastDue), cancellationToken);

        if (subscription == null)
        {
            throw new NotFoundException("No active or trialing subscription was found to modify.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var price = dto.NewBillingCycle == BillingCycleEnum.Yearly
            ? newPlan.YearlyPrice
            : newPlan.MonthlyPrice;

        subscription.PlanId = newPlan.Id;
        subscription.BillingCycle = dto.NewBillingCycle;
        subscription.PriceAtSubscription = price;
        subscription.CurrencyAtSubscription = newPlan.Currency;
        subscription.EndDate = dto.NewBillingCycle == BillingCycleEnum.Yearly
            ? today.AddYears(1)
            : today.AddMonths(1);
        subscription.Status = SubscriptionStatusEnum.Active;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The subscription was updated concurrently by another operation. Please retry.");
        }

        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            CompanyId = subscription.CompanyId,
            PlanId = subscription.PlanId,
            PlanCode = newPlan.Code,
            PlanNameEn = newPlan.NameEn,
            PlanNameAr = newPlan.NameAr,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            PriceAtSubscription = subscription.PriceAtSubscription,
            CurrencyAtSubscription = subscription.CurrencyAtSubscription,
            BillingCycle = subscription.BillingCycle.ToString(),
            AutoRenew = subscription.AutoRenew,
            SuspendedAt = subscription.SuspendedAt,
            SuspensionReason = subscription.SuspensionReason,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            ExpiredAt = subscription.ExpiredAt,
            ExternalBillingRef = subscription.ExternalBillingRef,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionDto> CancelSubscriptionAsync(
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var companyId = await ResolveCompanyIdForUserAsync(userId, cancellationToken);
        if (!companyId.HasValue)
        {
            throw new NotFoundException("No active subscription found for user because no associated company exists.");
        }

        var subscription = await _dbContext.CompanySubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId.Value &&
                                      (s.Status == SubscriptionStatusEnum.Active ||
                                       s.Status == SubscriptionStatusEnum.Trialing ||
                                       s.Status == SubscriptionStatusEnum.PastDue), cancellationToken);

        if (subscription == null)
        {
            throw new NotFoundException("No active subscription was found to cancel.");
        }

        subscription.Status = SubscriptionStatusEnum.Cancelled;
        subscription.CancelledAt = DateTimeOffset.UtcNow;
        subscription.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by user" : reason.Trim();
        subscription.AutoRenew = false;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The subscription status was modified concurrently by another operation. Please retry.");
        }

        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            CompanyId = subscription.CompanyId,
            PlanId = subscription.PlanId,
            PlanCode = subscription.Plan.Code,
            PlanNameEn = subscription.Plan.NameEn,
            PlanNameAr = subscription.Plan.NameAr,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            PriceAtSubscription = subscription.PriceAtSubscription,
            CurrencyAtSubscription = subscription.CurrencyAtSubscription,
            BillingCycle = subscription.BillingCycle.ToString(),
            AutoRenew = subscription.AutoRenew,
            SuspendedAt = subscription.SuspendedAt,
            SuspensionReason = subscription.SuspensionReason,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            ExpiredAt = subscription.ExpiredAt,
            ExternalBillingRef = subscription.ExternalBillingRef,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
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

    private async Task<Guid> GetOrCreateCompanyForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existingCompanyId = await ResolveCompanyIdForUserAsync(userId, cancellationToken);
        if (existingCompanyId.HasValue)
        {
            return existingCompanyId.Value;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var displayName = user?.FullName ?? $"User-{userId.ToString()[..8]}";
        var company = Company.Create(
            legalName: displayName,
            displayName: displayName,
            primaryPhone: user?.Phone ?? "+962790000000",
            companyType: CompanyType.IndividualOwner,
            countryCode: "JO",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userId,
            primaryEmail: user?.Email);

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}

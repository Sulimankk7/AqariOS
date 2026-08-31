using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Subscriptions.Repositories;

internal sealed class SubscriptionPersistence : ISubscriptionPersistence
{
    private readonly PropertyOsDbContext _db;

    public SubscriptionPersistence(PropertyOsDbContext db) => _db = db;

    public async Task<PlanPageDto> GetPlatformPlansAsync(int page, int pageSize, bool? isActive, string? search, CancellationToken cancellationToken = default)
    {
        var query = _db.SubscriptionPlans.AsNoTracking().AsQueryable();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, pattern) ||
                                     EF.Functions.ILike(x.NameEn, pattern) ||
                                     EF.Functions.ILike(x.NameAr, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.IsActive).ThenBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SubscriptionPlanDto
            {
                Id = x.Id, Code = x.Code, NameEn = x.NameEn, NameAr = x.NameAr,
                DescriptionEn = x.DescriptionEn, DescriptionAr = x.DescriptionAr,
                MonthlyPrice = x.MonthlyPrice, YearlyPrice = x.YearlyPrice, Currency = x.Currency,
                PricingModel = x.PricingModel, PaygMonthlyUnitPrice = x.PaygMonthlyUnitPrice,
                PaygYearlyMonthlyEquivalentUnitPrice = x.PaygYearlyMonthlyEquivalentUnitPrice,
                MaxBuildings = x.MaxBuildings, MaxUsers = x.MaxUsers, MaxStorageMb = x.MaxStorageMb,
                FeatureFlags = x.FeatureFlags, SupportsTrial = x.SupportsTrial,
                TrialDurationDays = x.TrialDurationDays, IsActive = x.IsActive, SortOrder = x.SortOrder
            }).ToListAsync(cancellationToken);
        return new PlanPageDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<SubscriptionPageDto> GetPlatformSubscriptionsAsync(int page, int pageSize, string? search,
        SubscriptionStatusEnum? status, BillingCycleEnum? billingCycle, Guid? planId, string sortBy, bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CompanySubscriptions.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (billingCycle.HasValue) query = query.Where(x => x.BillingCycle == billingCycle.Value);
        if (planId.HasValue) query = query.Where(x => x.PlanId == planId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Company.DisplayName, pattern) ||
                                     EF.Functions.ILike(x.Company.LegalName, pattern) ||
                                     EF.Functions.ILike(x.Plan.Code, pattern) ||
                                     EF.Functions.ILike(x.Plan.NameEn, pattern) ||
                                     EF.Functions.ILike(x.Plan.NameAr, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        query = ApplySort(query, sortBy, descending);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SubscriptionAdministrationDto
        {
            Id = x.Id, CompanyId = x.CompanyId, CompanyName = x.Company.DisplayName,
            PlanId = x.PlanId, PlanCode = x.Plan.Code, PlanNameEn = x.Plan.NameEn, PlanNameAr = x.Plan.NameAr,
            Status = x.Status, BillingCycle = x.BillingCycle, StartDate = x.StartDate, EndDate = x.EndDate,
            TrialEndDate = x.TrialEndDate, PriceAtSubscription = x.PriceAtSubscription,
            CurrencyAtSubscription = x.CurrencyAtSubscription, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt
        }).ToListAsync(cancellationToken);
        return new SubscriptionPageDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PlanChangeRequestPageDto> GetPlatformPlanChangeRequestsAsync(int page, int pageSize,
        PlanChangeRequestStatus? status, Guid? companyId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _db.PlanChangeRequests.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (companyId.HasValue) query = query.Where(x => x.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Company.DisplayName, pattern) ||
                                     EF.Functions.ILike(x.Company.LegalName, pattern) ||
                                     EF.Functions.ILike(x.CurrentPlan.NameEn, pattern) ||
                                     EF.Functions.ILike(x.RequestedPlan.NameEn, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.RequestedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new PlanChangeRequestDto
            {
                Id = x.Id, CompanyId = x.CompanyId, CompanyName = x.Company.DisplayName, SubscriptionId = x.SubscriptionId,
                CurrentPlanId = x.CurrentPlanId, CurrentPlanNameEn = x.CurrentPlan.NameEn, CurrentPlanNameAr = x.CurrentPlan.NameAr,
                RequestedPlanId = x.RequestedPlanId, RequestedPlanNameEn = x.RequestedPlan.NameEn, RequestedPlanNameAr = x.RequestedPlan.NameAr,
                CurrentBillingCycle = x.CurrentBillingCycle, RequestedBillingCycle = x.RequestedBillingCycle,
                RequestedBy = x.RequestedBy, RequesterName = x.Requester.FullName, RequestedAt = x.RequestedAt, Status = x.Status,
                ReviewerId = x.ReviewerId, ReviewerName = x.Reviewer == null ? null : x.Reviewer.FullName,
                ReviewedAt = x.ReviewedAt, DecisionNote = x.DecisionNote, RejectionReason = x.RejectionReason
            }).ToListAsync(cancellationToken);
        return new PlanChangeRequestPageDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<Company?> GetCompanyForUpdateAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        _db.Database.IsRelational()
            ? _db.Companies.FromSqlInterpolated($"SELECT *, xmin FROM companies WHERE id = {companyId} AND deleted_at IS NULL FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : _db.Companies.SingleOrDefaultAsync(x => x.Id == companyId, cancellationToken);

    public Task<SubscriptionPlan?> GetPlanForUpdateAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _db.Database.IsRelational()
            ? _db.SubscriptionPlans.FromSqlInterpolated($"SELECT *, xmin FROM subscription_plans WHERE id = {planId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : _db.SubscriptionPlans.SingleOrDefaultAsync(x => x.Id == planId, cancellationToken);

    public Task<CompanySubscription?> GetCurrentSubscriptionForUpdateAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        _db.Database.IsRelational()
            ? _db.CompanySubscriptions.FromSqlInterpolated($"SELECT *, xmin FROM company_subscriptions WHERE company_id = {companyId} AND status IN ('trialing', 'active', 'past_due') ORDER BY created_at DESC FOR UPDATE").FirstOrDefaultAsync(cancellationToken)
            : _db.CompanySubscriptions.Where(x => x.CompanyId == companyId && CurrentStatuses.Contains(x.Status)).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<CompanySubscription?> GetSubscriptionForUpdateAsync(Guid subscriptionId, Guid companyId, CancellationToken cancellationToken = default) =>
        _db.Database.IsRelational()
            ? _db.CompanySubscriptions.FromSqlInterpolated($"SELECT *, xmin FROM company_subscriptions WHERE id = {subscriptionId} AND company_id = {companyId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : _db.CompanySubscriptions.SingleOrDefaultAsync(x => x.Id == subscriptionId && x.CompanyId == companyId, cancellationToken);

    public Task<PlanChangeRequest?> GetPlanChangeRequestForUpdateAsync(Guid requestId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (_db.Database.IsRelational())
            return companyId.HasValue
                ? _db.PlanChangeRequests.FromSqlInterpolated($"SELECT *, xmin FROM plan_change_requests WHERE id = {requestId} AND company_id = {companyId.Value} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                : _db.PlanChangeRequests.FromSqlInterpolated($"SELECT *, xmin FROM plan_change_requests WHERE id = {requestId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

        return companyId.HasValue
            ? _db.PlanChangeRequests.SingleOrDefaultAsync(x => x.Id == requestId && x.CompanyId == companyId.Value, cancellationToken)
            : _db.PlanChangeRequests.SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    private static readonly SubscriptionStatusEnum[] CurrentStatuses =
        [SubscriptionStatusEnum.Trialing, SubscriptionStatusEnum.Active, SubscriptionStatusEnum.PastDue];

    private static IQueryable<CompanySubscription> ApplySort(IQueryable<CompanySubscription> query, string sortBy, bool descending) =>
        (sortBy.ToLowerInvariant(), descending) switch
        {
            ("startdate", false) => query.OrderBy(x => x.StartDate).ThenBy(x => x.Id),
            ("startdate", true) => query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Id),
            ("enddate", false) => query.OrderBy(x => x.EndDate).ThenBy(x => x.Id),
            ("enddate", true) => query.OrderByDescending(x => x.EndDate).ThenBy(x => x.Id),
            ("company", false) => query.OrderBy(x => x.Company.DisplayName).ThenBy(x => x.Id),
            ("company", true) => query.OrderByDescending(x => x.Company.DisplayName).ThenBy(x => x.Id),
            ("plan", false) => query.OrderBy(x => x.Plan.NameEn).ThenBy(x => x.Id),
            ("plan", true) => query.OrderByDescending(x => x.Plan.NameEn).ThenBy(x => x.Id),
            ("status", false) => query.OrderBy(x => x.Status).ThenBy(x => x.Id),
            ("status", true) => query.OrderByDescending(x => x.Status).ThenBy(x => x.Id),
            (_, false) => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
        };
}

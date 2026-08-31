using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.DTOs.Subscriptions;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.UseCases;

public sealed record GetCurrentSubscriptionQuery : ITransactionalRequest<UserSubscriptionDto>;

public sealed class GetCurrentSubscriptionQueryHandler : IRequestHandler<GetCurrentSubscriptionQuery, UserSubscriptionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetCurrentSubscriptionQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<UserSubscriptionDto> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var companyId = RequireCompany(_tenant);
        return await _db.CompanySubscriptions.AsNoTracking()
            .Where(x => x.CompanyId == companyId && SubscriptionStates.Current.Contains(x.Status))
            .OrderByDescending(x => x.CreatedAt)
            .Select(SubscriptionProjectionExtensions.UserSubscription)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No current subscription was found for the authenticated company.");
    }

    internal static Guid RequireCompany(ITenantContext tenant)
    {
        if (tenant.IsPlatformAdmin || tenant.CompanyId is null)
            throw new UnauthorizedAccessException("Company administrator scope is required.");
        return tenant.CompanyId.Value;
    }
}

public sealed record GetPlatformSubscriptionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    SubscriptionStatusEnum? Status = null,
    BillingCycleEnum? BillingCycle = null,
    Guid? PlanId = null,
    string SortBy = "createdAt",
    bool Descending = true) : ITransactionalRequest<SubscriptionPageDto>;

public sealed class GetPlatformSubscriptionsQueryValidator : AbstractValidator<GetPlatformSubscriptionsQuery>
{
    private static readonly string[] SortFields = ["createdAt", "startDate", "endDate", "company", "plan", "status"];

    public GetPlatformSubscriptionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.PlanId).NotEqual(Guid.Empty).When(x => x.PlanId.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.BillingCycle).IsInEnum().When(x => x.BillingCycle.HasValue);
        RuleFor(x => x.SortBy).Must(x => SortFields.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported subscription sort field.");
    }
}

public sealed class GetPlatformSubscriptionsQueryHandler : IRequestHandler<GetPlatformSubscriptionsQuery, SubscriptionPageDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;

    public GetPlatformSubscriptionsQueryHandler(ISubscriptionPersistence persistence, ITenantContext tenant)
    {
        _persistence = persistence;
        _tenant = tenant;
    }

    public async Task<SubscriptionPageDto> Handle(GetPlatformSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _persistence.GetPlatformSubscriptionsAsync(
            request.Page, request.PageSize, request.Search, request.Status, request.BillingCycle,
            request.PlanId, request.SortBy, request.Descending, cancellationToken);
    }
}

public sealed record GetPlatformSubscriptionByIdQuery(Guid SubscriptionId)
    : ITransactionalRequest<SubscriptionAdministrationDto>;

public sealed class GetPlatformSubscriptionByIdQueryValidator : AbstractValidator<GetPlatformSubscriptionByIdQuery>
{
    public GetPlatformSubscriptionByIdQueryValidator() => RuleFor(x => x.SubscriptionId).NotEmpty();
}

public sealed class GetPlatformSubscriptionByIdQueryHandler
    : IRequestHandler<GetPlatformSubscriptionByIdQuery, SubscriptionAdministrationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPlatformSubscriptionByIdQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SubscriptionAdministrationDto> Handle(GetPlatformSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _db.CompanySubscriptions.AsNoTracking().Where(x => x.Id == request.SubscriptionId)
            .Select(SubscriptionProjectionExtensions.Administration).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Company subscription was not found.");
    }
}

public sealed record CreatePlatformSubscriptionCommand(
    Guid CompanyId,
    Guid PlanId,
    BillingCycleEnum BillingCycle,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? TrialEndDate) : ICommand<SubscriptionAdministrationDto>;

public sealed class CreatePlatformSubscriptionCommandValidator : AbstractValidator<CreatePlatformSubscriptionCommand>
{
    public CreatePlatformSubscriptionCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
        RuleFor(x => x.TrialEndDate)
            .Must((command, value) => !value.HasValue || value.Value > command.StartDate)
            .WithMessage("Trial end date must be after the start date.");
        RuleFor(x => x.TrialEndDate)
            .Must((command, value) => !value.HasValue || value.Value <= command.EndDate)
            .WithMessage("Trial end date must not be after the subscription end date.");
    }
}

public sealed class CreatePlatformSubscriptionCommandHandler
    : IRequestHandler<CreatePlatformSubscriptionCommand, SubscriptionAdministrationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;
    private readonly IBusinessClock _clock;

    public CreatePlatformSubscriptionCommandHandler(
        IApplicationDbContext db, ISubscriptionPersistence persistence, ITenantContext tenant, IBusinessClock clock)
    {
        _db = db;
        _persistence = persistence;
        _tenant = tenant;
        _clock = clock;
    }

    public async Task<SubscriptionAdministrationDto> Handle(CreatePlatformSubscriptionCommand request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        var company = await _persistence.GetCompanyForUpdateAsync(request.CompanyId, cancellationToken);
        if (company is null)
            throw new NotFoundException("Company was not found.");
        if (!company.IsActive)
            throw new BusinessRuleException("An inactive company is not eligible for a new subscription.", "COMPANY_NOT_ELIGIBLE");

        var plan = await _persistence.GetPlanForUpdateAsync(request.PlanId, cancellationToken);
        if (plan is null)
            throw new NotFoundException("Subscription plan was not found.");
        if (!plan.IsActive)
            throw new BusinessRuleException("An inactive plan cannot be used for a new subscription.", "PLAN_INACTIVE");

        if (await _db.CompanySubscriptions.AsNoTracking()
            .AnyAsync(x => x.CompanyId == request.CompanyId && SubscriptionStates.Current.Contains(x.Status), cancellationToken))
            throw new ConflictException(
                "The company already has a current subscription.",
                "CURRENT_SUBSCRIPTION_ALREADY_EXISTS");

        if (request.TrialEndDate.HasValue)
        {
            if (!plan.SupportsTrial || !plan.TrialDurationDays.HasValue)
                throw new BusinessRuleException("The selected plan does not support a trial.", "PLAN_TRIAL_NOT_SUPPORTED");
            if (request.TrialEndDate.Value > request.StartDate.AddDays(plan.TrialDurationDays.Value))
                throw new BusinessRuleException("Trial end date exceeds the selected plan's configured trial duration.", "PLAN_TRIAL_DURATION_EXCEEDED");
        }

        var status = request.TrialEndDate.HasValue ? SubscriptionStatusEnum.Trialing : SubscriptionStatusEnum.Active;
        var price = plan.PricingModel == SubscriptionPricingModel.Fixed
            ? request.BillingCycle == BillingCycleEnum.Yearly ? plan.YearlyPrice : plan.MonthlyPrice
            : 0m;
        var now = _clock.UtcNow;
        var subscription = new CompanySubscription
        {
            Id = Guid.CreateVersion7(), CompanyId = request.CompanyId, PlanId = plan.Id,
            Status = status, StartDate = request.StartDate, EndDate = request.EndDate,
            TrialEndDate = request.TrialEndDate, PriceAtSubscription = price,
            CurrencyAtSubscription = plan.Currency, BillingCycle = request.BillingCycle,
            CreatedAt = now, UpdatedAt = now
        };
        _db.CompanySubscriptions.Add(subscription);
        return SubscriptionProjectionExtensions.MapAdministration(subscription, company.DisplayName, plan);
    }
}

internal static class SubscriptionStates
{
    public static readonly SubscriptionStatusEnum[] Current =
        [SubscriptionStatusEnum.Trialing, SubscriptionStatusEnum.Active, SubscriptionStatusEnum.PastDue];
}

internal static class SubscriptionProjectionExtensions
{
    public static readonly System.Linq.Expressions.Expression<Func<CompanySubscription, UserSubscriptionDto>> UserSubscription = x => new UserSubscriptionDto
    {
        Id = x.Id, CompanyId = x.CompanyId, PlanId = x.PlanId, PlanCode = x.Plan.Code,
        PlanNameEn = x.Plan.NameEn, PlanNameAr = x.Plan.NameAr, Status = x.Status.ToString(),
        StartDate = x.StartDate, EndDate = x.EndDate, TrialEndDate = x.TrialEndDate,
        PriceAtSubscription = x.PriceAtSubscription, CurrencyAtSubscription = x.CurrencyAtSubscription,
        BillingCycle = x.BillingCycle.ToString(), AutoRenew = x.AutoRenew, SuspendedAt = x.SuspendedAt,
        SuspensionReason = x.SuspensionReason, CancelledAt = x.CancelledAt,
        CancellationReason = x.CancellationReason, ExpiredAt = x.ExpiredAt,
        ExternalBillingRef = x.ExternalBillingRef, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt
    };

    public static readonly System.Linq.Expressions.Expression<Func<CompanySubscription, SubscriptionAdministrationDto>> Administration = x => new SubscriptionAdministrationDto
    {
        Id = x.Id, CompanyId = x.CompanyId, CompanyName = x.Company.DisplayName,
        PlanId = x.PlanId, PlanCode = x.Plan.Code, PlanNameEn = x.Plan.NameEn, PlanNameAr = x.Plan.NameAr,
        Status = x.Status, BillingCycle = x.BillingCycle, StartDate = x.StartDate, EndDate = x.EndDate,
        TrialEndDate = x.TrialEndDate, PriceAtSubscription = x.PriceAtSubscription,
        CurrencyAtSubscription = x.CurrencyAtSubscription, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt
    };

    public static SubscriptionAdministrationDto MapAdministration(CompanySubscription x, string companyName, SubscriptionPlan plan) => new()
    {
        Id = x.Id, CompanyId = x.CompanyId, CompanyName = companyName, PlanId = x.PlanId,
        PlanCode = plan.Code, PlanNameEn = plan.NameEn, PlanNameAr = plan.NameAr,
        Status = x.Status, BillingCycle = x.BillingCycle, StartDate = x.StartDate, EndDate = x.EndDate,
        TrialEndDate = x.TrialEndDate, PriceAtSubscription = x.PriceAtSubscription,
        CurrencyAtSubscription = x.CurrencyAtSubscription, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt
    };
}

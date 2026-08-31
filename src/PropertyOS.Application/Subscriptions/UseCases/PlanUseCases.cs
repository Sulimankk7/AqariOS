using System.Text.Json;
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

public sealed record GetAvailablePlansQuery(int Page = 1, int PageSize = 20) : IRequest<PlanPageDto>;

public sealed class GetAvailablePlansQueryValidator : AbstractValidator<GetAvailablePlansQuery>
{
    public GetAvailablePlansQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetAvailablePlansQueryHandler : IRequestHandler<GetAvailablePlansQuery, PlanPageDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAvailablePlansQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PlanPageDto> Handle(GetAvailablePlansQuery request, CancellationToken cancellationToken)
    {
        if (_tenant.CompanyId is null || _tenant.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Company administrator scope is required.");

        var query = _db.SubscriptionPlans.AsNoTracking().Where(x => x.IsActive);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(SubscriptionProjection.Plan)
            .ToListAsync(cancellationToken);

        return new PlanPageDto { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = count };
    }
}

public sealed record GetPlatformPlansQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null,
    string? Search = null) : ITransactionalRequest<PlanPageDto>;

public sealed class GetPlatformPlansQueryValidator : AbstractValidator<GetPlatformPlansQuery>
{
    public GetPlatformPlansQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class GetPlatformPlansQueryHandler : IRequestHandler<GetPlatformPlansQuery, PlanPageDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;

    public GetPlatformPlansQueryHandler(ISubscriptionPersistence persistence, ITenantContext tenant)
    {
        _persistence = persistence;
        _tenant = tenant;
    }

    public async Task<PlanPageDto> Handle(GetPlatformPlansQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _persistence.GetPlatformPlansAsync(
            request.Page, request.PageSize, request.IsActive, request.Search, cancellationToken);
    }
}

public sealed record GetPlatformPlanByIdQuery(Guid PlanId) : ITransactionalRequest<SubscriptionPlanDto>;

public sealed class GetPlatformPlanByIdQueryValidator : AbstractValidator<GetPlatformPlanByIdQuery>
{
    public GetPlatformPlanByIdQueryValidator() => RuleFor(x => x.PlanId).NotEmpty();
}

public sealed class GetPlatformPlanByIdQueryHandler : IRequestHandler<GetPlatformPlanByIdQuery, SubscriptionPlanDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPlatformPlanByIdQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SubscriptionPlanDto> Handle(GetPlatformPlanByIdQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _db.SubscriptionPlans.AsNoTracking().Where(x => x.Id == request.PlanId)
            .Select(SubscriptionProjection.Plan).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Subscription plan was not found.");
    }
}

public sealed record CreatePlanCommand(
    string Code,
    string NameEn,
    string NameAr,
    string? DescriptionEn,
    string? DescriptionAr,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    string Currency,
    int? MaxBuildings,
    int? MaxUsers,
    int? MaxStorageMb,
    string FeatureFlags,
    bool SupportsTrial,
    short? TrialDurationDays,
    short SortOrder,
    SubscriptionPricingModel PricingModel = SubscriptionPricingModel.Fixed,
    decimal? PaygMonthlyUnitPrice = null,
    decimal? PaygYearlyMonthlyEquivalentUnitPrice = null) : ICommand<SubscriptionPlanDto>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.PricingModel).IsInEnum();
        RuleFor(x => x.MonthlyPrice).GreaterThan(0).When(x => x.PricingModel == SubscriptionPricingModel.Fixed);
        RuleFor(x => x.YearlyPrice).GreaterThan(0).When(x => x.PricingModel == SubscriptionPricingModel.Fixed);
        RuleFor(x => x.MonthlyPrice).Equal(0).When(x => x.PricingModel == SubscriptionPricingModel.PayAsYouGo);
        RuleFor(x => x.YearlyPrice).Equal(0).When(x => x.PricingModel == SubscriptionPricingModel.PayAsYouGo);
        RuleFor(x => x.PaygMonthlyUnitPrice).NotNull().GreaterThan(0)
            .When(x => x.PricingModel == SubscriptionPricingModel.PayAsYouGo);
        RuleFor(x => x.PaygYearlyMonthlyEquivalentUnitPrice).NotNull().GreaterThan(0)
            .When(x => x.PricingModel == SubscriptionPricingModel.PayAsYouGo);
        RuleFor(x => x.PaygMonthlyUnitPrice).Null().When(x => x.PricingModel == SubscriptionPricingModel.Fixed);
        RuleFor(x => x.PaygYearlyMonthlyEquivalentUnitPrice).Null().When(x => x.PricingModel == SubscriptionPricingModel.Fixed);
        RuleFor(x => x.Currency).NotEmpty().Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a three-letter uppercase ISO currency code.");
        RuleFor(x => x.MaxBuildings).GreaterThan(0).When(x => x.MaxBuildings.HasValue);
        RuleFor(x => x.MaxUsers).GreaterThan(0).When(x => x.MaxUsers.HasValue);
        RuleFor(x => x.MaxStorageMb).GreaterThan(0).When(x => x.MaxStorageMb.HasValue);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
        RuleFor(x => x.TrialDurationDays).NotNull().GreaterThan((short)0).When(x => x.SupportsTrial);
        RuleFor(x => x.TrialDurationDays).Null().When(x => !x.SupportsTrial);
        RuleFor(x => x.FeatureFlags).NotEmpty().Must(FeatureFlagJson.IsValid)
            .WithMessage("FeatureFlags must be a JSON object whose values are booleans or enum strings.");
    }
}

public sealed class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, SubscriptionPlanDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBusinessClock _clock;

    public CreatePlanCommandHandler(IApplicationDbContext db, ITenantContext tenant, IBusinessClock clock)
    {
        _db = db;
        _tenant = tenant;
        _clock = clock;
    }

    public async Task<SubscriptionPlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        var code = request.Code.Trim();
        if (await _db.SubscriptionPlans.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException(
                "A subscription plan with this code already exists.",
                "PLAN_CODE_ALREADY_EXISTS");

        var now = _clock.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.CreateVersion7(),
            Code = code,
            NameEn = request.NameEn.Trim(),
            NameAr = request.NameAr.Trim(),
            DescriptionEn = Normalize(request.DescriptionEn),
            DescriptionAr = Normalize(request.DescriptionAr),
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            PricingModel = request.PricingModel,
            PaygMonthlyUnitPrice = request.PaygMonthlyUnitPrice,
            PaygYearlyMonthlyEquivalentUnitPrice = request.PaygYearlyMonthlyEquivalentUnitPrice,
            Currency = request.Currency,
            MaxBuildings = request.MaxBuildings,
            MaxUsers = request.MaxUsers,
            MaxStorageMb = request.MaxStorageMb,
            FeatureFlags = request.FeatureFlags,
            SupportsTrial = request.SupportsTrial,
            TrialDurationDays = request.TrialDurationDays,
            IsActive = true,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.SubscriptionPlans.Add(plan);
        return SubscriptionProjection.Map(plan);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record SetPlanActiveCommand(Guid PlanId, bool IsActive) : ICommand<SubscriptionPlanDto>;

public sealed class SetPlanActiveCommandValidator : AbstractValidator<SetPlanActiveCommand>
{
    public SetPlanActiveCommandValidator() => RuleFor(x => x.PlanId).NotEmpty();
}

public sealed class SetPlanActiveCommandHandler : IRequestHandler<SetPlanActiveCommand, SubscriptionPlanDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;
    private readonly IBusinessClock _clock;

    public SetPlanActiveCommandHandler(ISubscriptionPersistence persistence, ITenantContext tenant, IBusinessClock clock)
    {
        _persistence = persistence;
        _tenant = tenant;
        _clock = clock;
    }

    public async Task<SubscriptionPlanDto> Handle(SetPlanActiveCommand request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        var plan = await _persistence.GetPlanForUpdateAsync(request.PlanId, cancellationToken);

        if (plan is null)
            throw new NotFoundException("Subscription plan was not found.");

        if (request.IsActive) plan.Activate(_clock.UtcNow);
        else plan.Deactivate(_clock.UtcNow);
        return SubscriptionProjection.Map(plan);
    }
}

internal static class FeatureFlagJson
{
    public static bool IsValid(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name) || property.Name.Length > 100)
                    return false;
                if (property.Value.ValueKind is not JsonValueKind.True and not JsonValueKind.False and not JsonValueKind.String)
                    return false;
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

internal static class SubscriptionProjection
{
    public static readonly System.Linq.Expressions.Expression<Func<SubscriptionPlan, SubscriptionPlanDto>> Plan = x => new SubscriptionPlanDto
    {
        Id = x.Id, Code = x.Code, NameEn = x.NameEn, NameAr = x.NameAr,
        DescriptionEn = x.DescriptionEn, DescriptionAr = x.DescriptionAr,
        MonthlyPrice = x.MonthlyPrice, YearlyPrice = x.YearlyPrice, Currency = x.Currency,
        PricingModel = x.PricingModel, PaygMonthlyUnitPrice = x.PaygMonthlyUnitPrice,
        PaygYearlyMonthlyEquivalentUnitPrice = x.PaygYearlyMonthlyEquivalentUnitPrice,
        MaxBuildings = x.MaxBuildings, MaxUsers = x.MaxUsers, MaxStorageMb = x.MaxStorageMb,
        FeatureFlags = x.FeatureFlags, SupportsTrial = x.SupportsTrial,
        TrialDurationDays = x.TrialDurationDays, IsActive = x.IsActive, SortOrder = x.SortOrder
    };

    public static SubscriptionPlanDto Map(SubscriptionPlan x) => new()
    {
        Id = x.Id, Code = x.Code, NameEn = x.NameEn, NameAr = x.NameAr,
        DescriptionEn = x.DescriptionEn, DescriptionAr = x.DescriptionAr,
        MonthlyPrice = x.MonthlyPrice, YearlyPrice = x.YearlyPrice, Currency = x.Currency,
        PricingModel = x.PricingModel, PaygMonthlyUnitPrice = x.PaygMonthlyUnitPrice,
        PaygYearlyMonthlyEquivalentUnitPrice = x.PaygYearlyMonthlyEquivalentUnitPrice,
        MaxBuildings = x.MaxBuildings, MaxUsers = x.MaxUsers, MaxStorageMb = x.MaxStorageMb,
        FeatureFlags = x.FeatureFlags, SupportsTrial = x.SupportsTrial,
        TrialDurationDays = x.TrialDurationDays, IsActive = x.IsActive, SortOrder = x.SortOrder
    };
}

internal static class SubscriptionAuthorization
{
    public static void EnsurePlatform(ITenantContext tenant)
    {
        if (!tenant.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.UseCases;

public sealed record CreatePlanChangeRequestCommand(Guid RequestedPlanId, BillingCycleEnum RequestedBillingCycle)
    : ICommand<PlanChangeRequestDto>;

public sealed class CreatePlanChangeRequestCommandValidator : AbstractValidator<CreatePlanChangeRequestCommand>
{
    public CreatePlanChangeRequestCommandValidator()
    {
        RuleFor(x => x.RequestedPlanId).NotEmpty();
        RuleFor(x => x.RequestedBillingCycle).IsInEnum();
    }
}

public sealed class CreatePlanChangeRequestCommandHandler
    : IRequestHandler<CreatePlanChangeRequestCommand, PlanChangeRequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;

    public CreatePlanChangeRequestCommandHandler(
        IApplicationDbContext db,
        ISubscriptionPersistence persistence,
        ITenantContext tenant,
        ICurrentUserContext currentUser,
        IBusinessClock clock)
    {
        _db = db;
        _persistence = persistence;
        _tenant = tenant;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PlanChangeRequestDto> Handle(CreatePlanChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var companyId = GetCurrentSubscriptionQueryHandler.RequireCompany(_tenant);
        var requesterId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated company administrator is required.");

        var subscription = await _persistence.GetCurrentSubscriptionForUpdateAsync(companyId, cancellationToken)
            ?? throw new NotFoundException("No current subscription was found for the authenticated company.");

        var requestedPlan = await _persistence.GetPlanForUpdateAsync(request.RequestedPlanId, cancellationToken)
            ?? throw new NotFoundException("Requested subscription plan was not found.");
        if (!requestedPlan.IsActive)
            throw new BusinessRuleException("An inactive plan cannot be requested.", "PLAN_INACTIVE");

        if (subscription.PlanId == request.RequestedPlanId && subscription.BillingCycle == request.RequestedBillingCycle)
            throw new BusinessRuleException(
                "The requested plan and billing cycle are already assigned to the subscription.",
                "PLAN_CHANGE_SAME_AS_CURRENT");

        if (await _db.PlanChangeRequests.AsNoTracking()
            .AnyAsync(x => x.CompanyId == companyId && x.Status == PlanChangeRequestStatus.Pending, cancellationToken))
            throw new ConflictException(
                "The company already has a pending plan change request.",
                "PENDING_PLAN_CHANGE_ALREADY_EXISTS");

        var changeRequest = PlanChangeRequest.Create(
            companyId, subscription.Id, subscription.PlanId, requestedPlan.Id,
            subscription.BillingCycle, request.RequestedBillingCycle,
            requesterId, _clock.UtcNow);
        _db.PlanChangeRequests.Add(changeRequest);

        var currentPlan = subscription.PlanId == requestedPlan.Id
            ? requestedPlan
            : await _db.SubscriptionPlans.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == subscription.PlanId, cancellationToken)
                ?? throw new BusinessRuleException(
                    "The current subscription plan no longer exists.",
                    "PLAN_CHANGE_CURRENT_PLAN_MISSING");
        return PlanChangeRequestProjection.Map(changeRequest, currentPlan, requestedPlan);
    }
}

public sealed record GetMyPlanChangeRequestsQuery(int Page = 1, int PageSize = 20)
    : ITransactionalRequest<PlanChangeRequestPageDto>;

public sealed class GetMyPlanChangeRequestsQueryValidator : AbstractValidator<GetMyPlanChangeRequestsQuery>
{
    public GetMyPlanChangeRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetMyPlanChangeRequestsQueryHandler
    : IRequestHandler<GetMyPlanChangeRequestsQuery, PlanChangeRequestPageDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetMyPlanChangeRequestsQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PlanChangeRequestPageDto> Handle(GetMyPlanChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        var companyId = GetCurrentSubscriptionQueryHandler.RequireCompany(_tenant);
        var query = _db.PlanChangeRequests.AsNoTracking().Where(x => x.CompanyId == companyId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.RequestedAt).ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(PlanChangeRequestProjection.Own)
            .ToListAsync(cancellationToken);
        return new PlanChangeRequestPageDto { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }
}

public sealed record CancelPlanChangeRequestCommand(Guid RequestId) : ICommand<PlanChangeRequestDto>;

public sealed class CancelPlanChangeRequestCommandValidator : AbstractValidator<CancelPlanChangeRequestCommand>
{
    public CancelPlanChangeRequestCommandValidator() => RuleFor(x => x.RequestId).NotEmpty();
}

public sealed class CancelPlanChangeRequestCommandHandler
    : IRequestHandler<CancelPlanChangeRequestCommand, PlanChangeRequestDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;

    public CancelPlanChangeRequestCommandHandler(ISubscriptionPersistence persistence, ITenantContext tenant)
    {
        _persistence = persistence;
        _tenant = tenant;
    }

    public async Task<PlanChangeRequestDto> Handle(CancelPlanChangeRequestCommand command, CancellationToken cancellationToken)
    {
        var companyId = GetCurrentSubscriptionQueryHandler.RequireCompany(_tenant);
        var request = await _persistence.GetPlanChangeRequestForUpdateAsync(
            command.RequestId, companyId, cancellationToken);

        if (request is null)
            throw new NotFoundException("Plan change request was not found for the authenticated company.");
        if (request.Status != PlanChangeRequestStatus.Pending)
            throw new ConflictException(
                "Only a pending plan change request can be cancelled.",
                "PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION");

        request.Cancel();
        return PlanChangeRequestProjection.Map(request);
    }
}

public sealed record GetPlatformPlanChangeRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    PlanChangeRequestStatus? Status = null,
    Guid? CompanyId = null,
    string? Search = null) : ITransactionalRequest<PlanChangeRequestPageDto>;

public sealed class GetPlatformPlanChangeRequestsQueryValidator : AbstractValidator<GetPlatformPlanChangeRequestsQuery>
{
    public GetPlatformPlanChangeRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.CompanyId).NotEqual(Guid.Empty).When(x => x.CompanyId.HasValue);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class GetPlatformPlanChangeRequestsQueryHandler
    : IRequestHandler<GetPlatformPlanChangeRequestsQuery, PlanChangeRequestPageDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;

    public GetPlatformPlanChangeRequestsQueryHandler(ISubscriptionPersistence persistence, ITenantContext tenant)
    {
        _persistence = persistence;
        _tenant = tenant;
    }

    public async Task<PlanChangeRequestPageDto> Handle(GetPlatformPlanChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _persistence.GetPlatformPlanChangeRequestsAsync(
            request.Page, request.PageSize, request.Status, request.CompanyId, request.Search, cancellationToken);
    }
}

public sealed record GetPlatformPlanChangeRequestByIdQuery(Guid RequestId)
    : ITransactionalRequest<PlanChangeRequestDto>;

public sealed class GetPlatformPlanChangeRequestByIdQueryValidator : AbstractValidator<GetPlatformPlanChangeRequestByIdQuery>
{
    public GetPlatformPlanChangeRequestByIdQueryValidator() => RuleFor(x => x.RequestId).NotEmpty();
}

public sealed class GetPlatformPlanChangeRequestByIdQueryHandler
    : IRequestHandler<GetPlatformPlanChangeRequestByIdQuery, PlanChangeRequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public GetPlatformPlanChangeRequestByIdQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PlanChangeRequestDto> Handle(GetPlatformPlanChangeRequestByIdQuery request, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        return await _db.PlanChangeRequests.AsNoTracking().Where(x => x.Id == request.RequestId)
            .Select(PlanChangeRequestProjection.Platform).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Plan change request was not found.");
    }
}

public sealed record ApprovePlanChangeRequestCommand(Guid RequestId, string? DecisionNote)
    : ICommand<PlanChangeRequestDto>;

public sealed class ApprovePlanChangeRequestCommandValidator : AbstractValidator<ApprovePlanChangeRequestCommand>
{
    public ApprovePlanChangeRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.DecisionNote).MaximumLength(1000);
    }
}

public sealed class ApprovePlanChangeRequestCommandHandler
    : IRequestHandler<ApprovePlanChangeRequestCommand, PlanChangeRequestDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;

    public ApprovePlanChangeRequestCommandHandler(
        IApplicationDbContext db, ISubscriptionPersistence persistence, ITenantContext tenant, ICurrentUserContext currentUser, IBusinessClock clock)
    {
        _db = db;
        _persistence = persistence;
        _tenant = tenant;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PlanChangeRequestDto> Handle(ApprovePlanChangeRequestCommand command, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        var reviewerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated platform administrator is required.");
        var request = await _persistence.GetPlanChangeRequestForUpdateAsync(command.RequestId, null, cancellationToken)
            ?? throw new NotFoundException("Plan change request was not found.");
        if (request.Status != PlanChangeRequestStatus.Pending)
            throw new ConflictException(
                "Only a pending plan change request can be approved.",
                "PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION");

        if (!await _db.Companies.AsNoTracking().AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
            throw new BusinessRuleException("The request company no longer exists.", "PLAN_CHANGE_COMPANY_MISSING");

        var subscription = await _persistence.GetSubscriptionForUpdateAsync(request.SubscriptionId, request.CompanyId, cancellationToken)
            ?? throw new BusinessRuleException("The request subscription no longer exists for the company.", "PLAN_CHANGE_SUBSCRIPTION_MISSING");
        if (!SubscriptionStates.Current.Contains(subscription.Status))
            throw new BusinessRuleException("The subscription is no longer current.", "PLAN_CHANGE_SUBSCRIPTION_NOT_CURRENT");
        if (subscription.PlanId != request.CurrentPlanId || subscription.BillingCycle != request.CurrentBillingCycle)
            throw new ConflictException(
                "The subscription changed after this request was submitted.",
                "CONCURRENCY_CONFLICT");

        var requestedPlan = await _persistence.GetPlanForUpdateAsync(request.RequestedPlanId, cancellationToken)
            ?? throw new BusinessRuleException("The requested plan no longer exists.", "PLAN_CHANGE_REQUESTED_PLAN_MISSING");
        if (!requestedPlan.IsActive)
            throw new BusinessRuleException("The requested plan is no longer active.", "PLAN_CHANGE_REQUESTED_PLAN_INACTIVE");
        if (!Enum.IsDefined(typeof(BillingCycleEnum), request.RequestedBillingCycle))
            throw new BusinessRuleException("The requested billing cycle is invalid.", "PLAN_CHANGE_BILLING_CYCLE_INVALID");

        var price = requestedPlan.PricingModel == SubscriptionPricingModel.Fixed
            ? request.RequestedBillingCycle == BillingCycleEnum.Yearly
                ? requestedPlan.YearlyPrice : requestedPlan.MonthlyPrice
            : 0m;
        var reviewedAt = _clock.UtcNow;
        subscription.ApplyApprovedPlanChange(
            requestedPlan.Id, request.RequestedBillingCycle, price, requestedPlan.Currency, reviewedAt);
        request.Approve(reviewerId, reviewedAt, command.DecisionNote);
        return PlanChangeRequestProjection.Map(request, currentPlan: null, requestedPlan: requestedPlan);
    }

}

public sealed record RejectPlanChangeRequestCommand(
    Guid RequestId,
    string RejectionReason,
    string? DecisionNote) : ICommand<PlanChangeRequestDto>;

public sealed class RejectPlanChangeRequestCommandValidator : AbstractValidator<RejectPlanChangeRequestCommand>
{
    public RejectPlanChangeRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Rejection reason is required.").MinimumLength(5).MaximumLength(500);
        RuleFor(x => x.DecisionNote).MaximumLength(1000);
    }
}

public sealed class RejectPlanChangeRequestCommandHandler
    : IRequestHandler<RejectPlanChangeRequestCommand, PlanChangeRequestDto>
{
    private readonly ISubscriptionPersistence _persistence;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;

    public RejectPlanChangeRequestCommandHandler(
        ISubscriptionPersistence persistence, ITenantContext tenant, ICurrentUserContext currentUser, IBusinessClock clock)
    {
        _persistence = persistence;
        _tenant = tenant;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PlanChangeRequestDto> Handle(RejectPlanChangeRequestCommand command, CancellationToken cancellationToken)
    {
        SubscriptionAuthorization.EnsurePlatform(_tenant);
        var reviewerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated platform administrator is required.");
        var request = await _persistence.GetPlanChangeRequestForUpdateAsync(command.RequestId, null, cancellationToken);

        if (request is null)
            throw new NotFoundException("Plan change request was not found.");
        if (request.Status != PlanChangeRequestStatus.Pending)
            throw new ConflictException(
                "Only a pending plan change request can be rejected.",
                "PLAN_CHANGE_INVALID_LIFECYCLE_TRANSITION");

        request.Reject(reviewerId, _clock.UtcNow, command.RejectionReason, command.DecisionNote);
        return PlanChangeRequestProjection.Map(request);
    }
}

internal static class PlanChangeRequestProjection
{
    public static readonly System.Linq.Expressions.Expression<Func<PlanChangeRequest, PlanChangeRequestDto>> Own = x => new PlanChangeRequestDto
    {
        Id = x.Id, CompanyId = x.CompanyId, SubscriptionId = x.SubscriptionId,
        CurrentPlanId = x.CurrentPlanId, CurrentPlanNameEn = x.CurrentPlan.NameEn, CurrentPlanNameAr = x.CurrentPlan.NameAr,
        RequestedPlanId = x.RequestedPlanId, RequestedPlanNameEn = x.RequestedPlan.NameEn, RequestedPlanNameAr = x.RequestedPlan.NameAr,
        CurrentBillingCycle = x.CurrentBillingCycle, RequestedBillingCycle = x.RequestedBillingCycle,
        RequestedBy = x.RequestedBy, RequestedAt = x.RequestedAt, Status = x.Status,
        ReviewerId = x.ReviewerId, ReviewedAt = x.ReviewedAt, DecisionNote = x.DecisionNote, RejectionReason = x.RejectionReason
    };

    public static readonly System.Linq.Expressions.Expression<Func<PlanChangeRequest, PlanChangeRequestDto>> Platform = x => new PlanChangeRequestDto
    {
        Id = x.Id, CompanyId = x.CompanyId, CompanyName = x.Company.DisplayName, SubscriptionId = x.SubscriptionId,
        CurrentPlanId = x.CurrentPlanId, CurrentPlanNameEn = x.CurrentPlan.NameEn, CurrentPlanNameAr = x.CurrentPlan.NameAr,
        RequestedPlanId = x.RequestedPlanId, RequestedPlanNameEn = x.RequestedPlan.NameEn, RequestedPlanNameAr = x.RequestedPlan.NameAr,
        CurrentBillingCycle = x.CurrentBillingCycle, RequestedBillingCycle = x.RequestedBillingCycle,
        RequestedBy = x.RequestedBy, RequesterName = x.Requester.FullName, RequestedAt = x.RequestedAt, Status = x.Status,
        ReviewerId = x.ReviewerId, ReviewerName = x.Reviewer == null ? null : x.Reviewer.FullName,
        ReviewedAt = x.ReviewedAt, DecisionNote = x.DecisionNote, RejectionReason = x.RejectionReason
    };

    public static PlanChangeRequestDto Map(
        PlanChangeRequest x,
        SubscriptionPlan? currentPlan = null,
        SubscriptionPlan? requestedPlan = null) => new()
    {
        Id = x.Id, CompanyId = x.CompanyId, SubscriptionId = x.SubscriptionId,
        CurrentPlanId = x.CurrentPlanId, CurrentPlanNameEn = currentPlan?.NameEn ?? string.Empty,
        CurrentPlanNameAr = currentPlan?.NameAr ?? string.Empty,
        RequestedPlanId = x.RequestedPlanId, RequestedPlanNameEn = requestedPlan?.NameEn ?? string.Empty,
        RequestedPlanNameAr = requestedPlan?.NameAr ?? string.Empty,
        CurrentBillingCycle = x.CurrentBillingCycle, RequestedBillingCycle = x.RequestedBillingCycle,
        RequestedBy = x.RequestedBy, RequestedAt = x.RequestedAt, Status = x.Status,
        ReviewerId = x.ReviewerId, ReviewedAt = x.ReviewedAt,
        DecisionNote = x.DecisionNote, RejectionReason = x.RejectionReason
    };
}

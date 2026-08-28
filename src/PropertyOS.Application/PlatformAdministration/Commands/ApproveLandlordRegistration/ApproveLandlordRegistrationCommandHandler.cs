using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.PlatformAdministration.Commands.ApproveLandlordRegistration;

public sealed class ApproveLandlordRegistrationCommandHandler
    : IRequestHandler<ApproveLandlordRegistrationCommand, LandlordRegistrationReviewResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;
    private readonly ITenantContext _tenantContext;

    public ApproveLandlordRegistrationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUser,
        IBusinessClock clock,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _tenantContext = tenantContext;
    }

    public async Task<LandlordRegistrationReviewResultDto> Handle(
        ApproveLandlordRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsPlatformAdmin)
            throw new UnauthorizedAccessException("Platform administrator scope is required.");

        var reviewerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated platform administrator is required.");

        var registration = await _dbContext.LandlordRegistrations
            .Include(x => x.User)
            .Include(x => x.Company)
            .Include(x => x.Membership)
            .FirstOrDefaultAsync(x => x.Id == request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException("Landlord registration was not found.");

        if (registration.Status != RegistrationApprovalStatus.Pending)
            throw new BusinessRuleException(
                "Only a pending landlord registration can be approved.",
                "LANDLORD_REGISTRATION_NOT_PENDING");

        var reviewedAt = _clock.UtcNow;
        registration.Approve(reviewerId, reviewedAt);

        registration.User.IsActive = true;
        registration.User.UpdatedAt = reviewedAt;
        registration.Company.ActivateAfterApproval(reviewedAt, reviewerId);
        registration.Membership.Status = MembershipStatus.Active;
        registration.Membership.JoinedAt ??= reviewedAt;
        registration.Membership.SuspendedAt = null;
        registration.Membership.UpdatedAt = reviewedAt;

        return new LandlordRegistrationReviewResultDto
        {
            RegistrationId = registration.Id,
            Status = registration.Status.ToString(),
            ReviewedAt = reviewedAt
        };
    }
}

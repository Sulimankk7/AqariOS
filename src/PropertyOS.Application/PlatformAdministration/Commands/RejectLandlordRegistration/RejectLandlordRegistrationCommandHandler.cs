using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.PlatformAdministration.DTOs;
using PropertyOS.Domain.Identity.Enums;

namespace PropertyOS.Application.PlatformAdministration.Commands.RejectLandlordRegistration;

public sealed class RejectLandlordRegistrationCommandHandler
    : IRequestHandler<RejectLandlordRegistrationCommand, LandlordRegistrationReviewResultDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly IBusinessClock _clock;
    private readonly ITenantContext _tenantContext;

    public RejectLandlordRegistrationCommandHandler(
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
        RejectLandlordRegistrationCommand request,
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
                "Only a pending landlord registration can be rejected.",
                "LANDLORD_REGISTRATION_NOT_PENDING");

        var reviewedAt = _clock.UtcNow;
        registration.Reject(request.Reason, reviewerId, reviewedAt);

        registration.User.IsActive = false;
        registration.User.UpdatedAt = reviewedAt;
        registration.Company.MarkPendingApproval(reviewedAt, reviewerId);
        registration.Membership.Status = MembershipStatus.Suspended;
        registration.Membership.SuspendedAt = reviewedAt;
        registration.Membership.UpdatedAt = reviewedAt;

        return new LandlordRegistrationReviewResultDto
        {
            RegistrationId = registration.Id,
            Status = registration.Status.ToString(),
            ReviewedAt = reviewedAt
        };
    }
}

using FluentValidation;

namespace PropertyOS.Application.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");

        RuleFor(x => x)
            .Must(x => x.LastSeenCreatedAt.HasValue == x.LastSeenId.HasValue)
            .WithMessage("LastSeenCreatedAt and LastSeenId must be provided together.");
    }
}

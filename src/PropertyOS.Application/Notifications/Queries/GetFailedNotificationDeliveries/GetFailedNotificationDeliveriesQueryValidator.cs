using FluentValidation;

namespace PropertyOS.Application.Notifications.Queries.GetFailedNotificationDeliveries;

public class GetFailedNotificationDeliveriesQueryValidator : AbstractValidator<GetFailedNotificationDeliveriesQuery>
{
    public GetFailedNotificationDeliveriesQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");

        RuleFor(x => x)
            .Must(x => x.LastSeenSentAt.HasValue == x.LastSeenId.HasValue)
            .WithMessage("LastSeenSentAt and LastSeenId must be provided together.");
    }
}

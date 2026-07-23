using FluentValidation;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;

public class UpdateNotificationDeliveryCommandValidator : AbstractValidator<UpdateNotificationDeliveryCommand>
{
    public UpdateNotificationDeliveryCommandValidator()
    {
        RuleFor(v => v.NotificationId).NotEmpty();
        RuleFor(v => v.DeliveryId).NotEmpty();
        RuleFor(v => v.NewStatus).IsInEnum();
        
        RuleFor(v => v.FailureReason)
            .NotEmpty()
            .When(v => v.NewStatus == DeliveryStatus.Failed)
            .WithMessage("FailureReason is required when status is Failed.");
    }
}

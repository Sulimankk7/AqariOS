using FluentValidation;

namespace PropertyOS.Application.DTOs.Subscriptions.Validators;

/// <summary>
/// Validator for CreateSubscriptionRequestDto.
/// </summary>
public class CreateSubscriptionRequestDtoValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    /// <summary>
    /// Initializes validation rules for subscription creation requests.
    /// </summary>
    public CreateSubscriptionRequestDtoValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("Plan ID is required and must be a valid GUID.");

        RuleFor(x => x.BillingCycle)
            .IsInEnum()
            .WithMessage("Billing cycle must be either Monthly (0) or Yearly (1).");
    }
}

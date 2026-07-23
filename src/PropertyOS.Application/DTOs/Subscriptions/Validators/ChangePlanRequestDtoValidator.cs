using FluentValidation;

namespace PropertyOS.Application.DTOs.Subscriptions.Validators;

/// <summary>
/// Validator for ChangePlanRequestDto.
/// </summary>
public class ChangePlanRequestDtoValidator : AbstractValidator<ChangePlanRequestDto>
{
    /// <summary>
    /// Initializes validation rules for plan change requests.
    /// </summary>
    public ChangePlanRequestDtoValidator()
    {
        RuleFor(x => x.NewPlanId)
            .NotEmpty()
            .WithMessage("New Plan ID is required and must be a valid GUID.");

        RuleFor(x => x.NewBillingCycle)
            .IsInEnum()
            .WithMessage("New Billing cycle must be either Monthly (0) or Yearly (1).");
    }
}

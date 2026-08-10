using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetEmergencyContactById;

public class GetEmergencyContactByIdQueryValidator : AbstractValidator<GetEmergencyContactByIdQuery>
{
    public GetEmergencyContactByIdQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.ContactId)
            .NotEmpty().WithMessage("ContactId is required.");
    }
}

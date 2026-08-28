using FluentValidation;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetLandlordRegistrationById;

public sealed class GetLandlordRegistrationByIdQueryValidator : AbstractValidator<GetLandlordRegistrationByIdQuery>
{
    public GetLandlordRegistrationByIdQueryValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
    }
}

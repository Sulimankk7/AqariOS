using FluentValidation;

namespace PropertyOS.Application.PlatformAdministration.Queries.GetPendingLandlordRegistrations;

public sealed class GetPendingLandlordRegistrationsQueryValidator : AbstractValidator<GetPendingLandlordRegistrationsQuery>
{
    public GetPendingLandlordRegistrationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;

public class GetLeaseHistoryForApartmentQueryValidator : AbstractValidator<GetLeaseHistoryForApartmentQuery>
{
    public GetLeaseHistoryForApartmentQueryValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty().WithMessage("Apartment ID is required.");
    }
}

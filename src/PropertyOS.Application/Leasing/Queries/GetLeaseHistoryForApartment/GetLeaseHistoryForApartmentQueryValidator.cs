using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForApartment;

public class GetLeaseHistoryForApartmentQueryValidator : AbstractValidator<GetLeaseHistoryForApartmentQuery>
{
    public GetLeaseHistoryForApartmentQueryValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty().WithMessage("Apartment ID is required.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be greater than or equal to 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be less than or equal to 200.");
    }
}

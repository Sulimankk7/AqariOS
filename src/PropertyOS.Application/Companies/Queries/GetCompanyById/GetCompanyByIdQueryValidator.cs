using FluentValidation;

namespace PropertyOS.Application.Companies.Queries.GetCompanyById;

public class GetCompanyByIdQueryValidator : AbstractValidator<GetCompanyByIdQuery>
{
    public GetCompanyByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Company ID is required.");
    }
}

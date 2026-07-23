using FluentValidation;

namespace PropertyOS.Application.Companies.Queries.GetCompanySettings;

public class GetCompanySettingsQueryValidator : AbstractValidator<GetCompanySettingsQuery>
{
    public GetCompanySettingsQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Company ID is required.");
    }
}

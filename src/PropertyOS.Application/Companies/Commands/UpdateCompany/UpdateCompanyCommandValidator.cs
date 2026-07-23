using FluentValidation;

namespace PropertyOS.Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Company ID is required.");
        RuleFor(x => x.LegalName).NotEmpty().WithMessage("Legal name is required.");
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name is required.");
        RuleFor(x => x.PrimaryPhone).NotEmpty().WithMessage("Primary phone is required.");
        RuleFor(x => x.PrimaryEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.PrimaryEmail)).WithMessage("Primary email is not a valid email address.");
    }
}

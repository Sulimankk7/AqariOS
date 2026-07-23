using FluentValidation;

namespace PropertyOS.Application.Companies.Commands.UpdateCompanySettings;

public class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Company ID is required.");
        
        RuleFor(x => x.FiscalYearStartMonth)
            .InclusiveBetween((short)1, (short)12)
            .WithMessage("Fiscal year start month must be between 1 and 12.");
            
        RuleFor(x => x.RentGracePeriodDays)
            .GreaterThanOrEqualTo((short)0)
            .WithMessage("Grace period days cannot be negative.");
    }
}

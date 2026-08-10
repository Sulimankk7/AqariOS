using FluentValidation;

namespace PropertyOS.Application.Leasing.Queries.GetVehicleById;

public class GetVehicleByIdQueryValidator : AbstractValidator<GetVehicleByIdQuery>
{
    public GetVehicleByIdQueryValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");
    }
}

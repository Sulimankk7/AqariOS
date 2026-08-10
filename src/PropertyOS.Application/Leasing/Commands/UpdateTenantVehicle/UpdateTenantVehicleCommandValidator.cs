using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;

public class UpdateTenantVehicleCommandValidator : AbstractValidator<UpdateTenantVehicleCommand>
{
    public UpdateTenantVehicleCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");

        RuleFor(v => v.PlateNumber)
            .NotEmpty().WithMessage("PlateNumber is required.")
            .MaximumLength(20).WithMessage("PlateNumber must not exceed 20 characters.");

        RuleFor(v => v.MakeModel)
            .NotEmpty().WithMessage("MakeModel is required.")
            .MaximumLength(100).WithMessage("MakeModel must not exceed 100 characters.");

        RuleFor(v => v.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(50).WithMessage("Color must not exceed 50 characters.");
    }
}

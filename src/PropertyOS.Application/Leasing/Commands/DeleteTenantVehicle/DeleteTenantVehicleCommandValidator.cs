using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantVehicle;

public class DeleteTenantVehicleCommandValidator : AbstractValidator<DeleteTenantVehicleCommand>
{
    public DeleteTenantVehicleCommandValidator()
    {
        RuleFor(v => v.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(v => v.VehicleId)
            .NotEmpty().WithMessage("VehicleId is required.");
    }
}

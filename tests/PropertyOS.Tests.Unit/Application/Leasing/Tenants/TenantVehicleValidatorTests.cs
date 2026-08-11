using System;
using PropertyOS.Application.Leasing.Commands.CreateTenantVehicle;
using PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantVehicleValidatorTests
{
    [Fact]
    public void CreateTenantVehicleCommandValidator_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var validator = new CreateTenantVehicleCommandValidator();
        var command = new CreateTenantVehicleCommand(Guid.Empty, "", "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantVehicleCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantVehicleCommand.PlateNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantVehicleCommand.MakeModel));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantVehicleCommand.Color));
    }

    [Fact]
    public void CreateTenantVehicleCommandValidator_ShouldPass_WhenValid()
    {
        // Arrange
        var validator = new CreateTenantVehicleCommandValidator();
        var command = new CreateTenantVehicleCommand(Guid.NewGuid(), "12-34567", "Toyota Camry", "White");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTenantVehicleCommandValidator_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var validator = new UpdateTenantVehicleCommandValidator();
        var command = new UpdateTenantVehicleCommand(Guid.Empty, Guid.Empty, "", "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantVehicleCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantVehicleCommand.VehicleId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantVehicleCommand.PlateNumber));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantVehicleCommand.MakeModel));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantVehicleCommand.Color));
    }
}

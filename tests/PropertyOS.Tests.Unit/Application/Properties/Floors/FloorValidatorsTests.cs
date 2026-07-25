using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Properties.Floors.Commands.CreateFloor;
using PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Floors;

public class FloorValidatorsTests
{
    private readonly CreateFloorCommandValidator _createValidator = new();
    private readonly UpdateFloorCommandValidator _updateValidator = new();

    [Fact]
    public void CreateFloorCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new CreateFloorCommand(
            BuildingId: Guid.Empty,
            FloorNumber: 300,
            FloorLabel: "",
            FloorType: (FloorType)99
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BuildingId);
        result.ShouldHaveValidationErrorFor(x => x.FloorLabel);
        result.ShouldHaveValidationErrorFor(x => x.FloorType);
        result.ShouldHaveValidationErrorFor(x => x.FloorNumber);
    }

    [Fact]
    public void CreateFloorCommandValidator_ShouldNotHaveErrors_WhenFieldsAreValid()
    {
        // Arrange
        var command = new CreateFloorCommand(
            BuildingId: Guid.NewGuid(),
            FloorNumber: 2,
            FloorLabel: "Second Floor",
            FloorType: FloorType.Regular
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateFloorCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new UpdateFloorCommand(
            Id: Guid.Empty,
            FloorLabel: new string('A', 51),
            FloorType: (FloorType)99
        );

        // Act & Assert
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.FloorLabel);
        result.ShouldHaveValidationErrorFor(x => x.FloorType);
    }
}

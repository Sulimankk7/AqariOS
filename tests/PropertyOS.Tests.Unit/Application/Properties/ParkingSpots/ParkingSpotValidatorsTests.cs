using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingSpots;

public class ParkingSpotValidatorsTests
{
    private readonly CreateParkingSpotCommandValidator _createValidator = new();
    private readonly UpdateParkingSpotCommandValidator _updateValidator = new();

    [Fact]
    public void CreateParkingSpotCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new CreateParkingSpotCommand(
            BuildingId: Guid.Empty,
            SpotCode: "",
            ParkingType: (ParkingType)99,
            LocationDescription: new string('X', 256)
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BuildingId);
        result.ShouldHaveValidationErrorFor(x => x.SpotCode);
        result.ShouldHaveValidationErrorFor(x => x.ParkingType);
        result.ShouldHaveValidationErrorFor(x => x.LocationDescription);
    }

    [Fact]
    public void CreateParkingSpotCommandValidator_ShouldNotHaveErrors_WhenFieldsAreValid()
    {
        // Arrange
        var command = new CreateParkingSpotCommand(
            BuildingId: Guid.NewGuid(),
            SpotCode: "P1-01",
            ParkingType: ParkingType.Covered,
            LocationDescription: "B1 Level"
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateParkingSpotCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new UpdateParkingSpotCommand(
            Id: Guid.Empty,
            SpotCode: "",
            ParkingType: (ParkingType)99,
            LocationDescription: new string('X', 256)
        );

        // Act & Assert
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.SpotCode);
        result.ShouldHaveValidationErrorFor(x => x.ParkingType);
        result.ShouldHaveValidationErrorFor(x => x.LocationDescription);
    }
}

using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;
using PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class ApartmentValidatorsTests
{
    private readonly CreateApartmentCommandValidator _createValidator = new();
    private readonly UpdateApartmentCommandValidator _updateValidator = new();

    [Fact]
    public void CreateApartmentCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new CreateApartmentCommand(
            FloorId: Guid.Empty,
            UnitNumber: "",
            AreaSqm: -10m,
            OwnershipStatus: OwnershipStatus.ThirdPartyOwned,
            ExternalOwnerName: "", // Required for ThirdPartyOwned
            Bedrooms: -1,
            Bathrooms: -1,
            BaseRentAmount: -50m,
            BaseRentCurrency: "INVALID"
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FloorId);
        result.ShouldHaveValidationErrorFor(x => x.UnitNumber);
        result.ShouldHaveValidationErrorFor(x => x.AreaSqm);
        result.ShouldHaveValidationErrorFor(x => x.ExternalOwnerName);
        result.ShouldHaveValidationErrorFor(x => x.Bedrooms);
        result.ShouldHaveValidationErrorFor(x => x.Bathrooms);
        result.ShouldHaveValidationErrorFor(x => x.BaseRentAmount);
        result.ShouldHaveValidationErrorFor(x => x.BaseRentCurrency);
    }

    [Fact]
    public void CreateApartmentCommandValidator_ShouldNotHaveErrors_WhenFieldsAreValid()
    {
        // Arrange
        var command = new CreateApartmentCommand(
            FloorId: Guid.NewGuid(),
            UnitNumber: "101",
            AreaSqm: 120.5m,
            OwnershipStatus: OwnershipStatus.CompanyOwned,
            Bedrooms: 2,
            Bathrooms: 2,
            BaseRentAmount: 500m,
            BaseRentCurrency: "JOD"
        );

        // Act & Assert
        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateApartmentCommandValidator_ShouldHaveErrors_WhenFieldsAreInvalid()
    {
        // Arrange
        var command = new UpdateApartmentCommand(
            Id: Guid.Empty,
            BaseRentAmount: -100m,
            BaseRentCurrency: "USDX"
        );

        // Act & Assert
        var result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.BaseRentAmount);
        result.ShouldHaveValidationErrorFor(x => x.BaseRentCurrency);
    }
}

using System;
using PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;
using PropertyOS.Application.Leasing.Commands.UpdateTenantEmergencyContact;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantEmergencyContactValidatorTests
{
    [Fact]
    public void CreateTenantEmergencyContactCommandValidator_ShouldFail_WhenRequiredFieldsAreEmptyOrInvalidPhone()
    {
        // Arrange
        var validator = new CreateTenantEmergencyContactCommandValidator();
        var command = new CreateTenantEmergencyContactCommand(Guid.Empty, "", "", "invalid-phone");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantEmergencyContactCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantEmergencyContactCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantEmergencyContactCommand.RelationshipType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantEmergencyContactCommand.Phone));
    }

    [Fact]
    public void CreateTenantEmergencyContactCommandValidator_ShouldPass_WhenValid()
    {
        // Arrange
        var validator = new CreateTenantEmergencyContactCommandValidator();
        var command = new CreateTenantEmergencyContactCommand(Guid.NewGuid(), "Khaled Ahmad", "Brother", "+962791234567");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTenantEmergencyContactCommandValidator_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var validator = new UpdateTenantEmergencyContactCommandValidator();
        var command = new UpdateTenantEmergencyContactCommand(Guid.Empty, Guid.Empty, "", "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantEmergencyContactCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantEmergencyContactCommand.ContactId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantEmergencyContactCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantEmergencyContactCommand.RelationshipType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantEmergencyContactCommand.Phone));
    }
}

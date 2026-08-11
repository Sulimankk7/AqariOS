using System;
using PropertyOS.Application.Leasing.Commands.CreateTenantFamilyMember;
using PropertyOS.Application.Leasing.Commands.UpdateTenantFamilyMember;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantFamilyMemberValidatorTests
{
    [Fact]
    public void CreateTenantFamilyMemberCommandValidator_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var validator = new CreateTenantFamilyMemberCommandValidator();
        var command = new CreateTenantFamilyMemberCommand(Guid.Empty, "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantFamilyMemberCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantFamilyMemberCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantFamilyMemberCommand.RelationshipType));
    }

    [Fact]
    public void CreateTenantFamilyMemberCommandValidator_ShouldPass_WhenValid()
    {
        // Arrange
        var validator = new CreateTenantFamilyMemberCommandValidator();
        var command = new CreateTenantFamilyMemberCommand(Guid.NewGuid(), "Ahmad", "Son", "Child");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTenantFamilyMemberCommandValidator_ShouldFail_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var validator = new UpdateTenantFamilyMemberCommandValidator();
        var command = new UpdateTenantFamilyMemberCommand(Guid.Empty, Guid.Empty, "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantFamilyMemberCommand.TenantId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantFamilyMemberCommand.FamilyMemberId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantFamilyMemberCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTenantFamilyMemberCommand.RelationshipType));
    }
}

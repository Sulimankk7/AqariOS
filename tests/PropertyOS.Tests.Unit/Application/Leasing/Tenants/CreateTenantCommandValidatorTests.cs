using System;
using PropertyOS.Application.Leasing.Commands.CreateTenant;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    [Fact]
    public void Validator_ValidCommand_PassesValidation()
    {
        var command = new CreateTenantCommand(
            Name: "Ahmad Odeh",
            NationalId: "9901234567",
            Phone: "+962791234567",
            Email: "ahmad.odeh@example.com",
            Occupation: "Engineer",
            Employer: "Acme"
        );

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_MissingEmail_FailsValidation(string? emptyEmail)
    {
        var command = new CreateTenantCommand(
            Name: "Ahmad Odeh",
            NationalId: "9901234567",
            Phone: "+962791234567",
            Email: emptyEmail!,
            Occupation: "Engineer",
            Employer: "Acme"
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantCommand.Email));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("username@.com")]
    public void Validator_InvalidEmail_FailsValidation(string invalidEmail)
    {
        var command = new CreateTenantCommand(
            Name: "Ahmad Odeh",
            NationalId: "9901234567",
            Phone: "+962791234567",
            Email: invalidEmail,
            Occupation: "Engineer",
            Employer: "Acme"
        );

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantCommand.Email));
    }
}

using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing.Commands.CreateLeaseContract;
using Xunit;
using System;

namespace PropertyOS.Tests.Unit.Application.Leasing;

public class CreateLeaseContractCommandValidatorTests
{
    private readonly CreateLeaseContractCommandValidator _validator;

    public CreateLeaseContractCommandValidatorTests()
    {
        _validator = new CreateLeaseContractCommandValidator();
    }

    [Fact]
    public void Should_Pass_For_Valid_Command()
    {
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_EndDate_Less_Than_StartDate()
    {
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow.AddDays(10), DateTime.UtcNow, 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Fail_When_MonthlyRent_Zero_Or_Negative()
    {
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 0, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MonthlyRentAmount);
    }

    [Fact]
    public void Should_Fail_When_SecurityDeposit_Negative()
    {
        var command = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 500, -100, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SecurityDepositAmount);
    }

    [Fact]
    public void Should_Fail_When_PaymentDueDay_Outside_Range()
    {
        var command1 = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 0);
        var result1 = _validator.TestValidate(command1);
        result1.ShouldHaveValidationErrorFor(x => x.PaymentDueDay);

        var command2 = new CreateLeaseContractCommand(Guid.NewGuid(), Guid.NewGuid(), "LC-123", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 29);
        var result2 = _validator.TestValidate(command2);
        result2.ShouldHaveValidationErrorFor(x => x.PaymentDueDay);
    }
}

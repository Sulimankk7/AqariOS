using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing.Commands.RenewLeaseContract;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Validators;

public class RenewLeaseContractCommandValidatorTests
{
    private readonly RenewLeaseContractCommandValidator _validator;

    public RenewLeaseContractCommandValidatorTests()
    {
        _validator = new RenewLeaseContractCommandValidator();
    }

    [Fact]
    public void Should_Pass_For_Valid_Command()
    {
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_PriorContractId_IsEmpty()
    {
        var command = new RenewLeaseContractCommand(Guid.Empty, "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PriorContractId);
    }

    [Fact]
    public void Should_Fail_When_ContractNumber_IsBlank()
    {
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "   ", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContractNumber);
    }

    [Fact]
    public void Should_Fail_When_EndDate_Equals_StartDate()
    {
        var date = new DateTime(2025, 1, 1);
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", date, date, 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Fail_When_EndDate_Less_Than_StartDate()
    {
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2024, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Fail_When_MonthlyRent_Zero_Or_Negative()
    {
        var command1 = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 0, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        _validator.TestValidate(command1).ShouldHaveValidationErrorFor(x => x.MonthlyRentAmount);

        var command2 = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), -100, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        _validator.TestValidate(command2).ShouldHaveValidationErrorFor(x => x.MonthlyRentAmount);
    }

    [Fact]
    public void Should_Fail_When_SecurityDeposit_Negative()
    {
        var command = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, -100, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 5);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SecurityDepositAmount);
    }

    [Fact]
    public void Should_Fail_When_PaymentDueDay_Outside_Range()
    {
        var command1 = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 0);
        _validator.TestValidate(command1).ShouldHaveValidationErrorFor(x => x.PaymentDueDay);

        var command2 = new RenewLeaseContractCommand(Guid.NewGuid(), "LC-123", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 500, 1000, PropertyOS.Domain.Leasing.Enums.PaymentFrequency.Monthly, 29);
        _validator.TestValidate(command2).ShouldHaveValidationErrorFor(x => x.PaymentDueDay);
    }
}

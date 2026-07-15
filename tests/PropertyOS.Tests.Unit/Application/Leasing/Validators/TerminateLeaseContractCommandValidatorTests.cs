using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Validators;

public class TerminateLeaseContractCommandValidatorTests
{
    private readonly TerminateLeaseContractCommandValidator _validator;

    public TerminateLeaseContractCommandValidatorTests()
    {
        _validator = new TerminateLeaseContractCommandValidator();
    }

    [Fact]
    public void Should_Pass_For_Valid_Command()
    {
        var command = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, 0);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ContractId_IsEmpty()
    {
        var command = new TerminateLeaseContractCommand(Guid.Empty, TerminationType.MutualAgreement, new DateTime(2025, 1, 1));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContractId);
    }

    [Fact]
    public void Should_Fail_When_Future_TerminationDate()
    {
        var command = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, DateTime.UtcNow.AddDays(10));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TerminationDate);
    }

    [Fact]
    public void Should_Fail_When_Negative_Monetary_Fields()
    {
        var command1 = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), -100, 0, 0);
        _validator.TestValidate(command1).ShouldHaveValidationErrorFor(x => x.OutstandingBalance);

        var command2 = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, -100, 0);
        _validator.TestValidate(command2).ShouldHaveValidationErrorFor(x => x.DepositReturnedAmount);

        var command3 = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, -100);
        _validator.TestValidate(command3).ShouldHaveValidationErrorFor(x => x.DepositDeductionAmount);
    }

    [Fact]
    public void Should_Fail_When_Positive_DepositDeductionAmount_Without_Reason()
    {
        var command1 = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, 100, null);
        _validator.TestValidate(command1).ShouldHaveValidationErrorFor(x => x.DepositDeductionReason);

        var command2 = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, 100, "   ");
        _validator.TestValidate(command2).ShouldHaveValidationErrorFor(x => x.DepositDeductionReason);
    }

    [Fact]
    public void Should_Pass_When_Positive_DepositDeductionAmount_With_Reason()
    {
        var command = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, 100, "Damage to walls");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Zero_DepositDeductionAmount_Without_Reason()
    {
        var command = new TerminateLeaseContractCommand(Guid.NewGuid(), TerminationType.MutualAgreement, new DateTime(2025, 1, 1), 0, 0, 0, null);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

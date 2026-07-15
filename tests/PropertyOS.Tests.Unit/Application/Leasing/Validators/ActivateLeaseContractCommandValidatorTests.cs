using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Validators;

public class ActivateLeaseContractCommandValidatorTests
{
    private readonly ActivateLeaseContractCommandValidator _validator;

    public ActivateLeaseContractCommandValidatorTests()
    {
        _validator = new ActivateLeaseContractCommandValidator();
    }

    [Fact]
    public void Should_Fail_When_ContractId_IsEmpty()
    {
        var command = new ActivateLeaseContractCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContractId);
    }

    [Fact]
    public void Should_Pass_For_Valid_ContractId()
    {
        var command = new ActivateLeaseContractCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

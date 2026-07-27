using System;
using FluentValidation.TestHelper;
using PropertyOS.Application.Leasing.Commands.AttachContractDocument;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Validators;

public class AttachContractDocumentCommandValidatorTests
{
    private readonly AttachContractDocumentCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: Guid.NewGuid(),
            FileId: Guid.NewGuid(),
            DocumentType: ContractDocumentType.SignedContract,
            Description: "Valid description"
        );

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyLeaseContractId_HasValidationError()
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: Guid.Empty,
            FileId: Guid.NewGuid(),
            DocumentType: ContractDocumentType.SignedContract
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.LeaseContractId);
    }

    [Fact]
    public void Validate_EmptyFileId_HasValidationError()
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: Guid.NewGuid(),
            FileId: Guid.Empty,
            DocumentType: ContractDocumentType.SignedContract
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileId);
    }

    [Fact]
    public void Validate_InvalidDocumentType_HasValidationError()
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: Guid.NewGuid(),
            FileId: Guid.NewGuid(),
            DocumentType: (ContractDocumentType)999
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DocumentType);
    }

    [Fact]
    public void Validate_ExceedinglyLongDescription_HasValidationError()
    {
        var command = new AttachContractDocumentCommand(
            LeaseContractId: Guid.NewGuid(),
            FileId: Guid.NewGuid(),
            DocumentType: ContractDocumentType.SignedContract,
            Description: new string('a', 256)
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

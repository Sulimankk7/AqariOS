using System;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Financials;

public class CompanyReceiptSequenceTests
{
    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var companyId = Guid.NewGuid();
        var sequence = CompanyReceiptSequence.Create(
            companyId,
            "REC-",
            5,
            ReceiptResetPolicy.Yearly,
            DateTimeOffset.UtcNow
        );

        Assert.Equal(companyId, sequence.CompanyId);
        Assert.Equal("REC-", sequence.Prefix);
        Assert.Equal(5, sequence.PaddingLength);
        Assert.Equal(ReceiptResetPolicy.Yearly, sequence.ResetPolicy);
        Assert.Equal(0, sequence.CurrentNumber);
        Assert.Null(sequence.LastResetAt);
    }

    [Fact]
    public void Create_InvalidPaddingLength_ThrowsArgumentOutOfRangeException()
    {
        var companyId = Guid.NewGuid();

        // Too small
        Assert.Throws<ArgumentOutOfRangeException>(() => CompanyReceiptSequence.Create(
            companyId,
            "REC-",
            0,
            ReceiptResetPolicy.Never,
            DateTimeOffset.UtcNow
        ));

        // Too large
        Assert.Throws<ArgumentOutOfRangeException>(() => CompanyReceiptSequence.Create(
            companyId,
            "REC-",
            11,
            ReceiptResetPolicy.Never,
            DateTimeOffset.UtcNow
        ));
    }

    [Fact]
    public void UpdateFormatting_WithValidValues_Succeeds()
    {
        var sequence = CompanyReceiptSequence.Create(
            Guid.NewGuid(),
            "REC-",
            5,
            ReceiptResetPolicy.Yearly,
            DateTimeOffset.UtcNow
        );

        sequence.UpdateFormatting("NEW-", 8, ReceiptResetPolicy.Monthly, DateTimeOffset.UtcNow);

        Assert.Equal("NEW-", sequence.Prefix);
        Assert.Equal(8, sequence.PaddingLength);
        Assert.Equal(ReceiptResetPolicy.Monthly, sequence.ResetPolicy);
    }

    [Fact]
    public void UpdateFormatting_InvalidPaddingLength_ThrowsArgumentOutOfRangeException()
    {
        var sequence = CompanyReceiptSequence.Create(
            Guid.NewGuid(),
            "REC-",
            5,
            ReceiptResetPolicy.Yearly,
            DateTimeOffset.UtcNow
        );

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sequence.UpdateFormatting("NEW-", 0, ReceiptResetPolicy.Never, DateTimeOffset.UtcNow)
        );
    }

    [Fact]
    public void FormatReceiptNumber_ValidInputs_Succeeds()
    {
        var sequence = CompanyReceiptSequence.Create(
            Guid.NewGuid(),
            "REC-",
            6,
            ReceiptResetPolicy.Never,
            DateTimeOffset.UtcNow
        );

        var formatted1 = sequence.FormatReceiptNumber(1);
        var formatted123 = sequence.FormatReceiptNumber(123);

        Assert.Equal("REC-000001", formatted1);
        Assert.Equal("REC-000123", formatted123);
    }

    [Fact]
    public void FormatReceiptNumber_ZeroOrNegativeSequence_ThrowsArgumentOutOfRangeException()
    {
        var sequence = CompanyReceiptSequence.Create(
            Guid.NewGuid(),
            "REC-",
            6,
            ReceiptResetPolicy.Never,
            DateTimeOffset.UtcNow
        );

        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.FormatReceiptNumber(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sequence.FormatReceiptNumber(-1));
    }
}

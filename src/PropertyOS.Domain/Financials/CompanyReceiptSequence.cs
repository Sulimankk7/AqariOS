using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Domain.Financials;

public class CompanyReceiptSequence
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Prefix { get; private set; } = string.Empty;
    public long CurrentNumber { get; private set; }
    public short PaddingLength { get; private set; }
    public ReceiptResetPolicy ResetPolicy { get; private set; }
    public DateTimeOffset? LastResetAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CompanyReceiptSequence() { }

    public static CompanyReceiptSequence Create(
        Guid companyId,
        string prefix,
        short paddingLength,
        ReceiptResetPolicy resetPolicy,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (paddingLength < 1 || paddingLength > 10)
            throw new ArgumentOutOfRangeException(nameof(paddingLength), "Padding length must be between 1 and 10.");

        return new CompanyReceiptSequence
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            Prefix = prefix?.Trim() ?? string.Empty,
            CurrentNumber = 0,
            PaddingLength = paddingLength,
            ResetPolicy = resetPolicy,
            LastResetAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateFormatting(
        string prefix,
        short paddingLength,
        ReceiptResetPolicy resetPolicy,
        DateTimeOffset now)
    {
        if (paddingLength < 1 || paddingLength > 10)
            throw new ArgumentOutOfRangeException(nameof(paddingLength), "Padding length must be between 1 and 10.");

        Prefix = prefix?.Trim() ?? string.Empty;
        PaddingLength = paddingLength;
        ResetPolicy = resetPolicy;
        UpdatedAt = now;
    }

    public string FormatReceiptNumber(long number)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(nameof(number), "Sequence number must be positive.");

        return $"{Prefix}{number.ToString().PadLeft(PaddingLength, '0')}";
    }
}

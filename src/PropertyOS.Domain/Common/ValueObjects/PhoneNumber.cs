using System;
using System.Linq;

namespace PropertyOS.Domain.Common.ValueObjects;

/// <summary>
/// A standardized E.164 phone number value object.
/// Validates and normalizes phone and WhatsApp numbers before persistence.
/// </summary>
public record PhoneNumber
{
    public string Value { get; init; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty.");

        var normalized = Normalize(value);
        if (!IsValid(normalized))
            throw new ArgumentException($"Invalid E.164 phone number format: {value}");

        Value = normalized;
    }

    private static string Normalize(string phone)
    {
        // Strip common formatting characters: spaces, dashes, parentheses, dots
        var clean = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Replace leading 00 with +
        if (clean.StartsWith("00"))
        {
            clean = "+" + clean.Substring(2);
        }

        return clean;
    }

    private static bool IsValid(string phone)
    {
        // E.164 format requirement:
        // Starts with '+' followed by 7 to 15 digits
        if (!phone.StartsWith("+") || phone.Length < 8 || phone.Length > 16)
            return false;

        return phone.Skip(1).All(char.IsDigit);
    }

    public override string ToString() => Value;
}

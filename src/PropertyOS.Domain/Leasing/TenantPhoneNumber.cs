using System;
using System.Collections.Generic;
using PropertyOS.Domain.Common.ValueObjects;

namespace PropertyOS.Domain.Leasing;

/// <summary>
/// Canonicalizes Tenant contact phones without changing User/OTP phone behavior.
/// Local numbers require an explicit ISO country code.
/// </summary>
public static class TenantPhoneNumber
{
    private static readonly IReadOnlyDictionary<string, string> DialCodes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["JO"] = "+962",
            ["AE"] = "+971",
            ["SA"] = "+966",
            ["US"] = "+1",
            ["GB"] = "+44",
            ["KW"] = "+965",
            ["QA"] = "+974",
            ["BH"] = "+973",
            ["OM"] = "+968",
            ["EG"] = "+20",
            ["IN"] = "+91"
        };

    public static string Normalize(string phone, string? countryCode = "JO")
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone is required.", nameof(phone));

        var compact = RemoveFormatting(phone);
        if (compact.StartsWith("00", StringComparison.Ordinal))
            compact = "+" + compact[2..];

        if (!compact.StartsWith('+'))
        {
            var effectiveCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "JO" : countryCode.Trim();
            if (!DialCodes.TryGetValue(effectiveCountryCode, out var dialCode))
            {
                throw new ArgumentException(
                    "A supported ISO country code is required for a local phone number.",
                    nameof(countryCode));
            }

            var nationalNumber = compact.TrimStart('0');
            if (nationalNumber.Length == 0)
                throw new ArgumentException("Phone is invalid.", nameof(phone));

            compact = dialCode + nationalNumber;
        }

        if (compact.Length < 8 || compact.Length > 16 || compact[0] != '+' || compact[1] == '0')
            throw new ArgumentException("Phone must be a valid E.164 number.", nameof(phone));

        for (var index = 1; index < compact.Length; index++)
        {
            if (!char.IsAsciiDigit(compact[index]))
                throw new ArgumentException("Phone must be a valid E.164 number.", nameof(phone));
        }

        return new PhoneNumber(compact).Value;
    }

    public static bool TryNormalize(string? phone, string? countryCode, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            normalized = Normalize(phone ?? string.Empty, countryCode);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string RemoveFormatting(string phone)
    {
        var result = new System.Text.StringBuilder(phone.Length);
        foreach (var character in phone.Trim())
        {
            if (char.IsAsciiDigit(character) || (character == '+' && result.Length == 0))
            {
                result.Append(character);
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '(' or ')' or '.' or '/')
                continue;

            throw new ArgumentException("Phone contains invalid characters.", nameof(phone));
        }

        return result.ToString();
    }
}

using System.Text.RegularExpressions;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills;

internal static partial class UtilityAccountNumberRules
{
    public static bool IsValid(UtilityType utilityType, string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return false;

        var value = accountNumber.Trim();
        return utilityType switch
        {
            UtilityType.Electricity => ElectricityAccountNumber().IsMatch(value),
            UtilityType.Water => value.Length <= 20 && DigitsOnly().IsMatch(value),
            _ => false
        };
    }

    [GeneratedRegex(@"^\d{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex ElectricityAccountNumber();

    [GeneratedRegex(@"^\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex DigitsOnly();
}

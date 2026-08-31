namespace PropertyOS.Application.Identity;

public static class OtpPhoneNumber
{
    public static bool TryNormalize(string? value, out string normalized, bool allowJordanianLocal = false)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var phone = value.Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace(".", string.Empty);

        if (phone.StartsWith("00", StringComparison.Ordinal))
            phone = "+" + phone[2..];

        // OTP requests may explicitly opt into the Jordanian local-number context.
        // Verification retains its existing international E.164-only behavior.
        if (allowJordanianLocal && phone.Length == 10 && phone.StartsWith("07", StringComparison.Ordinal))
            phone = "+962" + phone[1..];

        if (phone.Length < 8 || phone.Length > 16 || phone[0] != '+' || phone[1] == '0')
            return false;

        for (var index = 1; index < phone.Length; index++)
        {
            if (!char.IsAsciiDigit(phone[index]))
                return false;
        }

        normalized = phone;
        return true;
    }
}

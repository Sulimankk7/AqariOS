using System;
using System.Text;

namespace PropertyOS.Application.Common.Models;

public static class KeysetCursor
{
    public static string Encode(DateTimeOffset submittedAt, Guid id)
    {
        var raw = $"{submittedAt:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static (DateTimeOffset SubmittedAt, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|');
            if (parts.Length == 2 && 
                DateTimeOffset.TryParse(parts[0], out var submittedAt) && 
                Guid.TryParse(parts[1], out var id))
            {
                return (submittedAt, id);
            }
        }
        catch
        {
            // Invalid cursor falls through
        }

        return null;
    }
}

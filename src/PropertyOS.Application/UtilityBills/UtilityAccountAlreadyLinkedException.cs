using PropertyOS.Application.Common.Exceptions;

namespace PropertyOS.Application.UtilityBills;

/// <summary>
/// Safe conflict raised when a utility account link would violate an existing link.
/// </summary>
public sealed class UtilityAccountAlreadyLinkedException : ConflictException
{
    public UtilityAccountAlreadyLinkedException(string message)
        : base(message)
    {
    }
}

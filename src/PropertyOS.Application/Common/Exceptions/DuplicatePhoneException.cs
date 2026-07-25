namespace PropertyOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when attempting to register or update a user with a phone number that already exists.
/// </summary>
public class DuplicatePhoneException : ConflictException
{
    public DuplicatePhoneException(string phone)
        : base($"A user with phone number '{phone}' already exists.")
    {
    }
}

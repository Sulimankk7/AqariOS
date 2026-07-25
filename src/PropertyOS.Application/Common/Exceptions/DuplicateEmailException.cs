namespace PropertyOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when attempting to register or update a user with an email address that already exists.
/// </summary>
public class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException(string email)
        : base($"A user with email '{email}' already exists.")
    {
    }
}

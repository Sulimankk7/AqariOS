namespace PropertyOS.Application.Identity;

/// <summary>
/// Abstraction for password hashing and verification algorithms.
/// Supports Argon2id and PBKDF2/SHA256 verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using Argon2id or secure salted hash.
    /// </summary>
    /// <param name="password">Plain-text password string.</param>
    /// <returns>Hashed password string.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain-text password against a stored password hash.
    /// </summary>
    /// <param name="password">Plain-text password presented by user.</param>
    /// <param name="passwordHash">Stored password hash.</param>
    /// <returns>True if password matches; otherwise false.</returns>
    bool VerifyPassword(string password, string passwordHash);
}

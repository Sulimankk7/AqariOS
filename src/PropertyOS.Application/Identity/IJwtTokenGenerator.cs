using System;
using System.Collections.Generic;

namespace PropertyOS.Application.Identity;

/// <summary>
/// Abstraction for generating JWT access tokens and hashing refresh tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT access token embedding required claims (sub, company_id, roles, permissions).
    /// </summary>
    /// <param name="userId">Authenticated user unique identifier.</param>
    /// <param name="companyId">Active company unique identifier, if applicable.</param>
    /// <param name="roles">Granted role codes.</param>
    /// <param name="permissions">Granted permission keys.</param>
    /// <returns>Signed JWT string and validity duration in seconds.</returns>
    (string Token, int ExpiresIn) GenerateAccessToken(
        Guid userId,
        Guid? companyId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);

    /// <summary>
    /// Generates a cryptographically secure opaque refresh token string.
    /// </summary>
    /// <returns>Base64Url or hex raw token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Computes the SHA-256 hash of a presented raw refresh token.
    /// </summary>
    /// <param name="rawToken">Raw refresh token string.</param>
    /// <returns>Hex string representation of the token hash.</returns>
    string HashRefreshToken(string rawToken);
}

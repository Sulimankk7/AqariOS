using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PropertyOS.Application.Identity;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Infrastructure service for generating JWT access tokens and hashing refresh tokens.
/// Configurable via appsettings.json "Jwt" section.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    /// <summary>
    /// Initializes a new instance of JwtTokenGenerator reading options from IConfiguration.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JwtTokenGenerator(IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection("Jwt");
        _secret = section["Secret"] ?? "PropertyOS-Secret-Signing-Key-Minimum-32-Bytes-Length!";
        _issuer = section["Issuer"] ?? "PropertyOS";
        _audience = section["Audience"] ?? "PropertyOS-Clients";
        _expiryMinutes = int.TryParse(section["ExpiryMinutes"], out int exp) ? exp : 15;
    }

    /// <inheritdoc />
    public (string Token, int ExpiresIn) GenerateAccessToken(
        Guid userId,
        Guid? companyId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            claims.Add(new Claim("company_id", companyId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("roles", role));
            }
        }

        foreach (var perm in permissions)
        {
            if (!string.IsNullOrWhiteSpace(perm))
            {
                claims.Add(new Claim("permissions", perm));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, _expiryMinutes * 60);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ArgumentException("Refresh token cannot be empty.", nameof(rawToken));

        byte[] bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

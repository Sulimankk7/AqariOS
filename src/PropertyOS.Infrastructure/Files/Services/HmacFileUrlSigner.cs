using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Infrastructure.Files.Services;

/// <summary>
/// HMAC-SHA256 signer over "purpose\nstorageKey\nexpiresUnixSeconds" keyed by
/// FileStorage:UrlSigningSecret. Fail-closed: throws when the secret is missing or
/// shorter than 32 bytes — file URLs must never fall back to unsigned operation.
/// </summary>
public sealed class HmacFileUrlSigner : IFileUrlSigner
{
    private readonly FileStorageOptions _options;

    public HmacFileUrlSigner(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(string purpose, string storageKey, long expiresUnixSeconds)
    {
        var payload = $"{purpose}\n{storageKey}\n{expiresUnixSeconds}";
        using var hmac = new HMACSHA256(GetKey());
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public bool Verify(string purpose, string storageKey, long expiresUnixSeconds, string token, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (now.ToUnixTimeSeconds() > expiresUnixSeconds)
            return false;

        var expected = CreateToken(purpose, storageKey, expiresUnixSeconds);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(token));
    }

    private byte[] GetKey()
    {
        var secret = _options.UrlSigningSecret;
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "FileStorage:UrlSigningSecret is missing or shorter than 32 bytes. " +
                "Configure a unique secret before serving file upload/download URLs.");
        }

        return Encoding.UTF8.GetBytes(secret);
    }
}

using System;

namespace PropertyOS.Application.Files.Services;

/// <summary>
/// Creates and verifies the HMAC tokens that make file upload/download URLs genuine
/// capability URLs: the token binds purpose + storage key + absolute expiry, so a URL
/// cannot be replayed for a different key, a different operation, or after expiry.
/// Fail-closed: implementations must throw when no signing secret is configured.
/// </summary>
public interface IFileUrlSigner
{
    /// <param name="purpose">"upload" or "download" — bound into the signature.</param>
    string CreateToken(string purpose, string storageKey, long expiresUnixSeconds);

    bool Verify(string purpose, string storageKey, long expiresUnixSeconds, string token, DateTimeOffset now);
}

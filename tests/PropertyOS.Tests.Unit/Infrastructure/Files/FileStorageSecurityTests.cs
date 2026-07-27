using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Files.Options;
using PropertyOS.Infrastructure.Files.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Files;

/// <summary>
/// Security guarantees of the physical storage provider (path containment) and the
/// HMAC URL signer (tamper/expiry/fail-closed behavior).
/// </summary>
public class FileStorageSecurityTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly PhysicalFileStorageProvider _provider;
    private readonly HmacFileUrlSigner _signer;

    public FileStorageSecurityTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "propertyos-storage-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var options = Options.Create(new FileStorageOptions
        {
            StorageBasePath = _tempRoot,
            UrlSigningSecret = "unit-test-signing-secret-at-least-32-bytes!!"
        });
        _signer = new HmacFileUrlSigner(options);
        _provider = new PhysicalFileStorageProvider(options, _signer);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    // ── Path containment ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("..\\escape.txt")]
    [InlineData("company/../../escape.txt")]
    [InlineData("company/..\\..\\escape.txt")]
    public async Task TraversalKeys_AreRejected(string storageKey)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.ExistsAsync(storageKey));
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.GetReadStreamAsync(storageKey));
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.DeleteAsync(storageKey));
    }

    [Theory]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("\\\\server\\share\\file.txt")]
    public async Task RootedKeys_AreRejected(string storageKey)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.ExistsAsync(storageKey));
    }

    [Fact]
    public async Task BlankKey_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _provider.ExistsAsync("  "));
    }

    [Fact]
    public async Task NormalKey_RoundTrips_WithinRoot()
    {
        var key = $"{Guid.NewGuid():D}/documents/{Guid.NewGuid():D}-report.pdf";
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-

        await using (var ms = new MemoryStream(content))
        {
            await _provider.SaveAsync(key, ms, "application/pdf");
        }

        Assert.True(await _provider.ExistsAsync(key));
        Assert.Equal(content.Length, await _provider.GetSizeAsync(key));

        await using (var read = await _provider.GetReadStreamAsync(key))
        {
            var buffer = new byte[content.Length];
            Assert.Equal(content.Length, await read.ReadAsync(buffer));
            Assert.Equal(content, buffer);
        }

        await _provider.DeleteAsync(key);
        Assert.False(await _provider.ExistsAsync(key));
    }

    // ── URL signing ─────────────────────────────────────────────────────────────

    [Fact]
    public void Token_RoundTrips_ForMatchingParameters()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = _signer.CreateToken("download", "company/key.pdf", expires);

        Assert.True(_signer.Verify("download", "company/key.pdf", expires, token, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Token_IsRejected_ForTamperedKeyPurposeOrExpiry()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = _signer.CreateToken("download", "company/key.pdf", expires);

        Assert.False(_signer.Verify("download", "company/OTHER.pdf", expires, token, DateTimeOffset.UtcNow));
        Assert.False(_signer.Verify("upload", "company/key.pdf", expires, token, DateTimeOffset.UtcNow));
        Assert.False(_signer.Verify("download", "company/key.pdf", expires + 1, token, DateTimeOffset.UtcNow));
        Assert.False(_signer.Verify("download", "company/key.pdf", expires, token + "x", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Token_IsRejected_AfterExpiry()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
        var token = _signer.CreateToken("download", "company/key.pdf", expires);

        Assert.False(_signer.Verify("download", "company/key.pdf", expires, token, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Signer_FailsClosed_WithoutSecret()
    {
        var unconfigured = new HmacFileUrlSigner(Options.Create(new FileStorageOptions()));

        Assert.Throws<InvalidOperationException>(() => unconfigured.CreateToken("download", "k", 1));
    }

    [Fact]
    public async Task SignedUrls_CarryAbsoluteExpiryAndSignature()
    {
        var key = $"{Guid.NewGuid():D}/documents/{Guid.NewGuid():D}-a.pdf";
        var url = await _provider.GeneratePreSignedDownloadUrlAsync(key, "a.pdf", 5);

        Assert.Contains("/api/v1/files/download?key=", url);
        Assert.Contains("&expires=", url);
        Assert.Contains("&sig=", url);
    }
}

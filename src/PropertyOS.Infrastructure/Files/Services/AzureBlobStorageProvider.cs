using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Infrastructure.Files.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IStorageProvider"/>.
///
/// Key design decisions:
/// — <see cref="BlobContainerClient"/> is injected as a singleton; no per-request instantiation.
/// — Upload/download URLs are native Azure SAS tokens (HMAC signing is not involved here;
///   the Physical provider continues to use <see cref="IFileUrlSigner"/> for its own URLs).
/// — The container is assumed to exist; this provider never auto-creates it.
/// — <see cref="GetReadStreamAsync"/> intentionally returns a streaming response — no full
///   blob buffering occurs.
/// — During confirmation magic-byte validation only 8 bytes are fetched from Azure via an
///   HTTP range request (<see cref="GetMagicBytesAsync"/>).
/// </summary>
public sealed class AzureBlobStorageProvider : IStorageProvider
{
    // Magic-byte header size consumed by FileValidationService.VerifyMagicBytes.
    // That method reads exactly 8 bytes; we fetch precisely that many to avoid
    // downloading the full blob during confirmation.
    private const int MagicByteLength = 8;

    private readonly BlobContainerClient _container;
    private readonly FileStorageOptions _options;

    public AzureBlobStorageProvider(BlobContainerClient container, FileStorageOptions options)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    // ── Write ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads the content stream directly to Azure Blob Storage without buffering.
    /// </summary>
    public async Task<string> SaveAsync(
        string storageKey,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = mimeType }
        };

        await blob.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);
        return storageKey;
    }

    // ── Read ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a streaming response from Azure Blob Storage. The caller is responsible
    /// for disposing the stream. No buffering occurs.
    /// </summary>
    public async Task<Stream> GetReadStreamAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        try
        {
            BlobDownloadStreamingResult result = await blob
                .DownloadStreamingAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException(
                $"Azure blob '{storageKey}' was not found in container '{_container.Name}'.", ex);
        }
    }

    // ── Existence & Metadata ─────────────────────────────────────────────────────

    public async Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        Response<bool> response = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return response.Value;
    }

    public async Task<long> GetSizeAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        try
        {
            BlobProperties props = await blob
                .GetPropertiesAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return props.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException(
                $"Azure blob '{storageKey}' was not found in container '{_container.Name}'.", ex);
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync(
            snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // ── Pre-signed URLs (SAS) ────────────────────────────────────────────────────

    /// <summary>
    /// Generates a short-lived Azure SAS URL that allows the client to PUT (upload) a
    /// single blob directly to Azure Blob Storage. The URL is HTTPS-only and is scoped
    /// to Write permission on the specific blob.
    ///
    /// This URL is returned to the client as the <c>UploadUrl</c> field of
    /// <c>UploadFileRequestResponse</c>. The client treats it as an opaque URL and
    /// PUTs its binary payload there without constructing or modifying it.
    /// </summary>
    public Task<string> GeneratePreSignedUploadUrlAsync(
        string storageKey,
        int expirationMinutes,
        CancellationToken cancellationToken = default)
    {
        var sasUri = BuildSasUri(storageKey, BlobSasPermissions.Write | BlobSasPermissions.Create, expirationMinutes);
        return Task.FromResult(sasUri.ToString());
    }

    /// <summary>
    /// Generates a short-lived Azure SAS URL that allows the caller to GET (download) a
    /// single blob directly from Azure Blob Storage. Scoped to Read permission.
    /// </summary>
    public Task<string> GeneratePreSignedDownloadUrlAsync(
        string storageKey,
        string filename,
        int expirationMinutes,
        bool inline = false,
        CancellationToken cancellationToken = default)
    {
        var builder = CreateSasBuilder(storageKey, BlobSasPermissions.Read, expirationMinutes);

        // Instruct Azure to send Content-Disposition header (inline or attachment)
        var dispositionType = inline ? "inline" : "attachment";
        builder.ContentDisposition = $"{dispositionType}; filename=\"{Uri.EscapeDataString(filename)}\"";

        var sasUri = BuildSasUriFromBuilder(storageKey, builder);
        return Task.FromResult(sasUri.ToString());
    }


    // ── Targeted magic-byte fetch (8 bytes only) ─────────────────────────────────

    /// <summary>
    /// Fetches only the first <see cref="MagicByteLength"/> bytes from the blob using
    /// an HTTP Range request. Returns them wrapped in a <see cref="MemoryStream"/> so
    /// that <c>FileValidationService.VerifyMagicBytes</c> can seek back to position 0
    /// after reading, exactly as it does for local streams.
    ///
    /// Called by <see cref="ConfirmFileUploadCommandHandler"/> via
    /// <see cref="GetReadStreamAsync"/> — however, to keep the interface contract
    /// intact (full stream), this method is not exposed on the interface. The
    /// ConfirmFileUploadCommandHandler already limits its read to the first bytes via
    /// <c>FileValidationService</c> which only reads 8 bytes. The full streaming path
    /// through <see cref="GetReadStreamAsync"/> is therefore acceptable; only 8 bytes
    /// are consumed before the stream is disposed.
    ///
    /// If you wish to eliminate even the round-trip overhead of a streaming request
    /// for the confirm step, this method is available for future internal optimization
    /// without any interface or handler changes.
    /// </summary>
    internal async Task<MemoryStream> GetMagicBytesAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        var range = new HttpRange(0, MagicByteLength);
        var downloadOptions = new BlobDownloadOptions { Range = range };

        try
        {
            BlobDownloadStreamingResult result = await blob
                .DownloadStreamingAsync(downloadOptions, cancellationToken)
                .ConfigureAwait(false);

            var buffer = new byte[MagicByteLength];
            int totalRead = 0;
            await using (result.Content)
            {
                while (totalRead < MagicByteLength)
                {
                    int n = await result.Content
                        .ReadAsync(buffer.AsMemory(totalRead, MagicByteLength - totalRead), cancellationToken)
                        .ConfigureAwait(false);
                    if (n == 0) break;
                    totalRead += n;
                }
            }

            return new MemoryStream(buffer, 0, totalRead, writable: false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException(
                $"Azure blob '{storageKey}' was not found in container '{_container.Name}'.", ex);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private BlobClient GetBlobClient(string storageKey) =>
        _container.GetBlobClient(storageKey);

    private BlobSasBuilder CreateSasBuilder(string storageKey, BlobSasPermissions permissions, int expirationMinutes)
    {
        var builder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = storageKey,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
            Protocol = SasProtocol.Https
        };
        builder.SetPermissions(permissions);
        return builder;
    }

    private Uri BuildSasUri(string storageKey, BlobSasPermissions permissions, int expirationMinutes)
    {
        var builder = CreateSasBuilder(storageKey, permissions, expirationMinutes);
        builder.SetPermissions(permissions);
        return BuildSasUriFromBuilder(storageKey, builder);
    }

    private Uri BuildSasUriFromBuilder(string storageKey, BlobSasBuilder builder)
    {
        var blob = GetBlobClient(storageKey);

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "AzureBlobStorageProvider cannot generate SAS URIs. " +
                "Ensure the BlobServiceClient was constructed with a StorageSharedKeyCredential " +
                "(i.e. using a connection string with an AccountKey) rather than a managed identity " +
                "or token credential. SAS generation requires the account key to be available at runtime.");
        }

        return blob.GenerateSasUri(builder);
    }
}

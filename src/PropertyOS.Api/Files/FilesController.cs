using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PropertyOS.Api.Models.Files;
using PropertyOS.Application.Files.Commands.ConfirmFileUpload;
using PropertyOS.Application.Files.Commands.UploadFileRequest;
using PropertyOS.Application.Files.DTOs;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Api.Files;

/// <summary>
/// Two-phase file transfer API: metadata-validated upload request (authenticated) →
/// direct binary PUT to a signed capability URL → authenticated confirmation.
/// Downloads are served from signed capability URLs generated per document.
/// The binary endpoints are [AllowAnonymous] by design: the HMAC token (purpose + key +
/// absolute expiry, verified fixed-time) IS the credential, mirroring S3 pre-signed URLs.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Produces("application/json", "application/problem+json")]
public class FilesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IStorageProvider _storageProvider;
    private readonly IFileUrlSigner _urlSigner;
    private readonly FileStorageOptions _options;

    public FilesController(
        ISender mediator,
        IStorageProvider storageProvider,
        IFileUrlSigner urlSigner,
        IOptions<FileStorageOptions> options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _storageProvider = storageProvider;
        _urlSigner = urlSigner;
        _options = options.Value;
    }

    /// <summary>
    /// Phase 1: validates file metadata and returns a signed, short-lived upload URL.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/files/upload-request")]
    [Authorize]
    [ProducesResponseType(typeof(UploadFileRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestUpload(
        [FromBody] UploadFileRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new UploadFileRequestCommand(
            ModuleName: request.ModuleName,
            EntityId: request.EntityId,
            Filename: request.Filename,
            MimeType: request.MimeType,
            SizeBytes: request.SizeBytes), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Phase 3: confirms a completed upload — re-validates content (magic bytes), verifies
    /// tenant key containment, file-ID binding, and on-disk size, then persists the record.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/files/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(FileStorageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmUpload(
        [FromBody] ConfirmFileUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var dto = await _mediator.Send(new ConfirmFileUploadCommand(
            FileId: request.FileId,
            StorageKey: request.StorageKey,
            OriginalFilename: request.OriginalFilename,
            MimeType: request.MimeType,
            SizeBytes: request.SizeBytes), cancellationToken);

        return Ok(dto);
    }

    /// <summary>
    /// Phase 2: receives the binary at the signed upload URL. Token-authenticated.
    /// </summary>
    [HttpPut("api/v{version:apiVersion}/files/upload")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status411LengthRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(
        [FromQuery] string key,
        [FromQuery] long expires,
        [FromQuery] string sig,
        CancellationToken cancellationToken = default)
    {
        if (!_urlSigner.Verify("upload", key, expires, sig, DateTimeOffset.UtcNow))
            return Unauthorized();

        var declaredLength = Request.ContentLength;
        if (declaredLength is null)
            return StatusCode(StatusCodes.Status411LengthRequired);
        if (declaredLength.Value <= 0 || declaredLength.Value > _options.MaxSizeBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        // Hard cap the actual bytes read regardless of the declared length.
        await using var bounded = new BoundedReadStream(Request.Body, _options.MaxSizeBytes);
        try
        {
            await _storageProvider.SaveAsync(key, bounded, Request.ContentType ?? "application/octet-stream", cancellationToken);
        }
        catch (InvalidDataException)
        {
            await _storageProvider.DeleteAsync(key, CancellationToken.None);
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        return NoContent();
    }

    /// <summary>
    /// Serves the binary at a signed download URL. Token-authenticated.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/files/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        [FromQuery] string key,
        [FromQuery] long expires,
        [FromQuery] string sig,
        CancellationToken cancellationToken = default)
    {
        if (!_urlSigner.Verify("download", key, expires, sig, DateTimeOffset.UtcNow))
            return Unauthorized();

        Stream stream;
        try
        {
            stream = await _storageProvider.GetReadStreamAsync(key, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }

        // Serve as an opaque attachment: never let browsers sniff/execute stored content.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var filename = ExtractDownloadFilename(key);
        return File(stream, "application/octet-stream", filename);
    }

    /// <summary>
    /// Key tail is "{fileId}-{sanitizedFilename}"; strip the 36-char GUID prefix when present.
    /// </summary>
    private static string ExtractDownloadFilename(string storageKey)
    {
        var tail = storageKey.Contains('/') ? storageKey[(storageKey.LastIndexOf('/') + 1)..] : storageKey;
        if (tail.Length > 37 && Guid.TryParse(tail[..36], out _) && tail[36] == '-')
            return tail[37..];
        return tail;
    }

    /// <summary>
    /// Read-only wrapper that throws InvalidDataException once more than maxBytes are read —
    /// the backstop against clients under-declaring Content-Length.
    /// </summary>
    private sealed class BoundedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private long _read;

        public BoundedReadStream(Stream inner, long maxBytes)
        {
            _inner = inner;
            _maxBytes = maxBytes;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);
            Count(n);
            return n;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var n = await _inner.ReadAsync(buffer, cancellationToken);
            Count(n);
            return n;
        }

        private void Count(int n)
        {
            _read += n;
            if (_read > _maxBytes)
                throw new InvalidDataException("Upload exceeds the maximum allowed size.");
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

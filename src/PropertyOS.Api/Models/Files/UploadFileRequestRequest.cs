using System;

namespace PropertyOS.Api.Models.Files;

/// <summary>Phase-1 upload request: metadata only, no binary content.</summary>
public record UploadFileRequestRequest(
    string ModuleName,
    Guid EntityId,
    string Filename,
    string MimeType,
    long SizeBytes
);

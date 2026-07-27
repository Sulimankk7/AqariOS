using System;

namespace PropertyOS.Api.Models.Files;

/// <summary>Phase-3 confirmation of a completed signed upload.</summary>
public record ConfirmFileUploadRequest(
    Guid FileId,
    string StorageKey,
    string OriginalFilename,
    string MimeType,
    long SizeBytes
);

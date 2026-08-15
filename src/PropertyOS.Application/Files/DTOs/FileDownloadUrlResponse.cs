using System;

namespace PropertyOS.Application.Files.DTOs;

public record FileDownloadUrlResponse(
    Guid FileId,
    string DownloadUrl,
    int ExpirationMinutes,
    string MimeType,
    string OriginalFilename);

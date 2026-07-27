using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Files.Commands.UploadFileRequest;

public record UploadFileRequestCommand(
    string ModuleName,
    Guid EntityId,
    string Filename,
    string MimeType,
    long SizeBytes
) : ICommand<UploadFileRequestResponse>;

public record UploadFileRequestResponse(
    Guid FileId,
    string StorageKey,
    string UploadUrl,
    int ExpirationMinutes
);

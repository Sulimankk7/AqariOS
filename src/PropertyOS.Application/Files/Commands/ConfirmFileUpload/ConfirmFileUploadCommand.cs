using MediatR;
using System;
using PropertyOS.Application.Files.DTOs;

namespace PropertyOS.Application.Files.Commands.ConfirmFileUpload;

public record ConfirmFileUploadCommand(
    Guid FileId,
    string StorageKey,
    string OriginalFilename,
    string MimeType,
    long SizeBytes
) : IRequest<FileStorageDto>;

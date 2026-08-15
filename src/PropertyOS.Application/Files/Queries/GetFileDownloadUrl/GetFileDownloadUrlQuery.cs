using System;
using MediatR;
using PropertyOS.Application.Files.DTOs;

namespace PropertyOS.Application.Files.Queries.GetFileDownloadUrl;

public record GetFileDownloadUrlQuery(
    Guid FileId,
    bool Inline = true) : IRequest<FileDownloadUrlResponse>;

using System;
using MediatR;

namespace PropertyOS.Application.Documents.Queries.GetDocumentDownloadUrl;

public record GetDocumentDownloadUrlQuery(Guid DocumentId) : IRequest<DocumentDownloadUrlResponse>;

public record DocumentDownloadUrlResponse(
    Guid DocumentId,
    string DownloadUrl,
    int ExpirationMinutes
);
